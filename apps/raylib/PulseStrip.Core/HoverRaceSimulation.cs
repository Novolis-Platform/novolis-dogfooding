namespace PulseStrip.Core;

using System.Numerics;
using Novolis.Simulation.Racing.Cars;
using Novolis.Simulation.Racing.Progress;
using Novolis.Simulation.Racing.Rewards;
using Novolis.Simulation.Racing.Sensors;
using Novolis.Simulation.Racing.Tracks;

/// <summary>
/// Anti-grav race loop on a <see cref="RaceTrack"/> spline raster:
/// boost, weapons, pickups, altitude band, wall scrape damage.
/// </summary>
public sealed class HoverRaceSimulation
{
    public const float HoverHeight = 1.35f;
    public const int SensorInputSize = 10;
    public const int ControlOutputSize = 5;

    private const double DeltaTime = 1.0 / 60.0;
    private const double MaxSpeed = 16.0;
    private const double BoostMaxSpeed = 26.0;
    private const double Acceleration = 11.0;
    private const double MaxTurnRate = 2.6;
    private const double BoostDrainPerSecond = 0.35;
    private const double BoostRegenPerSecond = 0.12;
    private const float ProjectileSpeed = 42f;
    private const float PickupRadius = 2.2f;
    private const float ProjectileHitRadius = 1.8f;

    private readonly ICarSensorModel _sensors = new DefaultCarSensorModel();
    private readonly ILapScorer _laps = new LapScorer();
    private readonly ITrackProgressResolver _progress = new TrackProgressResolver();
    private readonly IRewardModel? _rewardModel;
    private readonly double[]? _rewardAccum;

    public RaceTrack Track { get; }
    public IReadOnlyList<IHoverController> Controllers { get; }
    public HoverRaceWorldState State { get; private set; }

    /// <summary>Raised when a craft fires a weapon (for SFX/VFX hooks).</summary>
    public event Action<HoverCraftState>? WeaponFired;

    /// <summary>Raised when a projectile hits a craft.</summary>
    public event Action<HoverCraftState, HoverCraftState>? WeaponHit;

    /// <summary>Raised when a craft collects a pickup.</summary>
    public event Action<HoverCraftState, TrackPickup>? PickupCollected;

    /// <summary>Raised when a craft completes a lap.</summary>
    public event Action<HoverCraftState>? LapCompleted;

    public HoverRaceSimulation(
        RaceTrack track,
        IReadOnlyList<IHoverController> controllers,
        int targetLaps = 3,
        IRewardModel? trainingRewardModel = null,
        double[]? trainingRewardAccumulator = null)
    {
        ArgumentNullException.ThrowIfNull(track);
        ArgumentNullException.ThrowIfNull(controllers);
        if (controllers.Count == 0)
            throw new ArgumentException("At least one controller is required.", nameof(controllers));

        Track = track;
        Controllers = controllers;
        _rewardModel = trainingRewardModel;
        _rewardAccum = trainingRewardAccumulator;

        if (trainingRewardModel is not null)
        {
            if (trainingRewardAccumulator is null)
                throw new ArgumentNullException(nameof(trainingRewardAccumulator));
            if (trainingRewardAccumulator.Length != controllers.Count)
                throw new ArgumentException("Reward accumulator length must match controllers.", nameof(trainingRewardAccumulator));
        }

        State = CreateInitialState(targetLaps);
    }

    public void Reset(int? targetLaps = null)
    {
        State = CreateInitialState(targetLaps ?? State.TargetLaps);
    }

    public void Tick()
    {
        State.Tick++;
        var craft = State.Craft;

        for (var i = 0; i < craft.Count; i++)
        {
            var c = craft[i];
            if (c.Crashed || c.Finished)
                continue;

            var before = c.CloneForComparison();
            var carProxy = ToCarState(c);
            var progressBefore = _progress.Resolve(Track, Flat(c.Position), c.Forward);
            var sensors = _sensors.Read(Track, carProxy);
            var standing = GetStanding(c);
            var obs = new HoverObservation(i, c, sensors, progressBefore, standing);
            var decision = Controllers[i].Decide(in obs);

            IntegrateMotion(c, decision);
            ResolveCollisions(c);
            UpdateShield(c);
            CollectPickups(c);

            if (decision.Fire && c.WeaponAmmo > 0)
                FireWeapon(c);

            if (c.Crashed)
            {
                c.Speed = 0;
                AccumulateReward(i, before, c, progressBefore, _progress.Resolve(Track, Flat(c.Position), c.Forward));
                continue;
            }

            var lapCar = ToCarState(c);
            lapCar.PreviousPosition = Flat(before.Position);
            lapCar.Position = Flat(c.Position);
            lapCar.CurrentGateIndex = c.CurrentGateIndex;
            lapCar.CompletedLaps = c.CompletedLaps;
            var lapsBefore = lapCar.CompletedLaps;
            _laps.Update(Track, lapCar);
            c.CurrentGateIndex = lapCar.CurrentGateIndex;
            c.CompletedLaps = lapCar.CompletedLaps;
            if (c.CompletedLaps > lapsBefore)
                LapCompleted?.Invoke(c);

            var progressAfter = _progress.Resolve(Track, Flat(c.Position), c.Forward);
            c.TrackProgress = progressAfter.LoopT + c.CompletedLaps;
            c.TicksAlive++;

            if (c.CompletedLaps >= State.TargetLaps)
                c.Finished = true;

            AccumulateReward(i, before, c, progressBefore, progressAfter);
        }

        UpdateProjectiles();
        UpdatePickupRespawns();
        UpdatePlaces();

        if (craft.All(x => x.Finished || x.Crashed) || craft.Count(x => x.Finished) > 0 && craft.All(x => x.Finished || x.Crashed || x.Id != craft[0].Id))
        {
            // Race ends when player finishes or everyone is done.
            if (craft[0].Finished || craft.All(x => x.Finished || x.Crashed))
                State.RaceFinished = true;
        }
    }

    private void IntegrateMotion(HoverCraftState c, HoverControlDecision decision)
    {
        c.PreviousPosition = c.Position;

        var boosting = decision.Boost > 0.5 && c.BoostFuel > 0.05;
        c.Boosting = boosting;
        if (boosting)
            c.BoostFuel = Math.Max(0, c.BoostFuel - BoostDrainPerSecond * DeltaTime);
        else
            c.BoostFuel = Math.Min(1.0, c.BoostFuel + BoostRegenPerSecond * DeltaTime);

        var maxSpeed = boosting ? BoostMaxSpeed : MaxSpeed;
        c.Speed += (decision.Throttle - decision.Brake) * Acceleration * DeltaTime;
        if (boosting)
            c.Speed += Acceleration * 0.6 * DeltaTime;
        c.Speed = Math.Clamp(c.Speed, 0, maxSpeed);

        var turn = decision.Steering * MaxTurnRate * DeltaTime * (1.0 - 0.35 * (c.Speed / BoostMaxSpeed));
        var cos = Math.Cos(turn);
        var sin = Math.Sin(turn);
        var fx = (float)(c.Forward.X * cos - c.Forward.Z * sin);
        var fz = (float)(c.Forward.X * sin + c.Forward.Z * cos);
        c.Forward = Vector3.Normalize(new Vector3(fx, 0f, fz));
        c.Bank = Math.Clamp(c.Bank + (float)(decision.Steering * 4.0 * DeltaTime) - c.Bank * 3f * (float)DeltaTime, -0.7f, 0.7f);

        var planar = Flat(c.Position) + c.Forward * (float)(c.Speed * DeltaTime);
        var altitude = HoverHeight + (boosting ? 0.25f : 0f) + MathF.Sin(State.Tick * 0.08f + c.Id) * 0.05f;
        c.Position = new Vector3(planar.X, altitude, planar.Z);
    }

    private void ResolveCollisions(HoverCraftState c)
    {
        var col = (int)c.Position.X;
        var row = (int)c.Position.Z;
        if (col < 0 || col >= Track.Width || row < 0 || row >= Track.Height)
        {
            Crash(c);
            return;
        }

        var cell = Track.Cells[col, row];
        if (cell is TrackCell.Wall or TrackCell.Empty)
        {
            c.Health -= c.ShieldActive ? 4.0 : 12.0;
            c.Speed *= 0.35;
            // Nudge toward centerline.
            var prog = _progress.Resolve(Track, Flat(c.Position), c.Forward);
            var tangent = Track.ProgressMap.Tangents[
                Math.Clamp((int)(prog.LoopT * Track.ProgressMap.Samples.Count), 0, Track.ProgressMap.Samples.Count - 1)];
            var normal = new Vector3(tangent.Z, 0f, -tangent.X);
            var push = (float)(-Math.Sign(prog.SignedCenterOffset) * 0.6);
            c.Position += normal * push;
            if (c.Health <= 0)
                Crash(c);
        }
    }

    private static void Crash(HoverCraftState c)
    {
        c.Crashed = true;
        c.Speed = 0;
        c.Health = 0;
        c.Boosting = false;
    }

    private static void UpdateShield(HoverCraftState c)
    {
        if (!c.ShieldActive)
            return;
        c.ShieldTimer -= (float)DeltaTime;
        if (c.ShieldTimer <= 0)
        {
            c.ShieldActive = false;
            c.ShieldTimer = 0;
        }
    }

    private void CollectPickups(HoverCraftState c)
    {
        foreach (var pad in State.Pickups)
        {
            if (!pad.Available)
                continue;
            if (Vector3.Distance(Flat(c.Position), Flat(pad.Position)) > PickupRadius)
                continue;

            pad.Available = false;
            pad.RespawnTimer = 8f;
            switch (pad.Kind)
            {
                case PickupKind.Weapon:
                    c.WeaponAmmo = Math.Min(5, c.WeaponAmmo + 2);
                    break;
                case PickupKind.Shield:
                    c.ShieldActive = true;
                    c.ShieldTimer = 4.5f;
                    break;
            }

            PickupCollected?.Invoke(c, pad);
        }
    }

    private void FireWeapon(HoverCraftState c)
    {
        c.WeaponAmmo--;
        State.Projectiles.Add(new HoverProjectile
        {
            OwnerId = c.Id,
            Position = c.Position + c.Forward * 1.5f,
            Velocity = c.Forward * ProjectileSpeed,
        });
        WeaponFired?.Invoke(c);
    }

    private void UpdateProjectiles()
    {
        for (var i = State.Projectiles.Count - 1; i >= 0; i--)
        {
            var p = State.Projectiles[i];
            if (!p.Active)
            {
                State.Projectiles.RemoveAt(i);
                continue;
            }

            p.Position += p.Velocity * (float)DeltaTime;
            p.Life -= (float)DeltaTime;
            if (p.Life <= 0)
            {
                State.Projectiles.RemoveAt(i);
                continue;
            }

            foreach (var c in State.Craft)
            {
                if (c.Id == p.OwnerId || c.Crashed || c.Finished)
                    continue;
                if (Vector3.Distance(c.Position, p.Position) > ProjectileHitRadius)
                    continue;

                p.Active = false;
                var damage = c.ShieldActive ? 8.0 : 28.0;
                c.Health -= damage;
                if (c.ShieldActive)
                {
                    c.ShieldActive = false;
                    c.ShieldTimer = 0;
                }

                var owner = State.Craft.FirstOrDefault(x => x.Id == p.OwnerId);
                if (owner is not null)
                    WeaponHit?.Invoke(owner, c);

                if (c.Health <= 0)
                    Crash(c);

                State.Projectiles.RemoveAt(i);
                break;
            }
        }
    }

    private void UpdatePickupRespawns()
    {
        foreach (var pad in State.Pickups)
        {
            if (pad.Available)
                continue;
            pad.RespawnTimer -= (float)DeltaTime;
            if (pad.RespawnTimer <= 0)
            {
                pad.Available = true;
                pad.RespawnTimer = 0;
            }
        }
    }

    private void UpdatePlaces()
    {
        var ranked = State.Craft
            .Select((c, i) => (c, i))
            .OrderByDescending(x => x.c.TrackProgress)
            .ThenBy(x => x.c.Crashed)
            .ToList();
        for (var place = 0; place < ranked.Count; place++)
            ranked[place].c.Place = place + 1;
    }

    private RaceStanding GetStanding(HoverCraftState c) =>
        new(c.Place, c.Id, c.Name, c.CompletedLaps, c.TrackProgress);

    private void AccumulateReward(
        int index,
        HoverCraftState before,
        HoverCraftState after,
        TrackProgressSample progressBefore,
        TrackProgressSample progressAfter)
    {
        if (_rewardModel is null || _rewardAccum is null)
            return;

        var prevCar = ToCarState(before);
        var currCar = ToCarState(after);
        var breakdown = _rewardModel.Evaluate(Track, prevCar, currCar, progressBefore, progressAfter);
        _rewardAccum[index] += breakdown.Total;
    }

    private HoverRaceWorldState CreateInitialState(int targetLaps)
    {
        var start = Track.StartPose;
        var craft = new List<HoverCraftState>(Controllers.Count);
        for (var i = 0; i < Controllers.Count; i++)
        {
            var lateral = (i - (Controllers.Count - 1) * 0.5f) * 1.6f;
            var tangent = start.Forward;
            var normal = new Vector3(tangent.Z, 0f, -tangent.X);
            var pos = start.Position + normal * lateral - tangent * (i * 2.2f);
            craft.Add(new HoverCraftState
            {
                Id = i,
                Name = Controllers[i].Name,
                Position = new Vector3(pos.X, HoverHeight, pos.Z),
                PreviousPosition = new Vector3(pos.X, HoverHeight, pos.Z),
                Forward = Vector3.Normalize(new Vector3(tangent.X, 0f, tangent.Z)),
                WeaponAmmo = i == 0 ? 1 : 0,
            });
        }

        var pickups = BuildPickups();
        return new HoverRaceWorldState
        {
            Craft = craft,
            Pickups = pickups,
            TargetLaps = targetLaps,
        };
    }

    private List<TrackPickup> BuildPickups()
    {
        var list = new List<TrackPickup>();
        var samples = Track.CenterLineSamples;
        if (samples.Count == 0)
            return list;

        var step = Math.Max(1, samples.Count / 6);
        for (var i = step / 2; i < samples.Count; i += step)
        {
            var p = samples[i];
            list.Add(new TrackPickup
            {
                Id = list.Count,
                Kind = list.Count % 2 == 0 ? PickupKind.Weapon : PickupKind.Shield,
                Position = new Vector3(p.X, HoverHeight, p.Z),
            });
        }

        return list;
    }

    private static Vector3 Flat(Vector3 p) => new(p.X, 0f, p.Z);

    private static CarState ToCarState(HoverCraftState c) =>
        new()
        {
            Id = c.Id,
            Name = c.Name,
            Position = Flat(c.Position),
            PreviousPosition = Flat(c.PreviousPosition),
            Forward = new Vector3(c.Forward.X, 0f, c.Forward.Z),
            Speed = c.Speed,
            SteeringAngle = 0,
            Crashed = c.Crashed,
            WrongWay = false,
            CompletedLaps = c.CompletedLaps,
            CurrentGateIndex = c.CurrentGateIndex,
            TrackProgress = c.TrackProgress,
            Fitness = 0,
            TicksAlive = c.TicksAlive,
            WrongWayTicks = 0,
        };
}
