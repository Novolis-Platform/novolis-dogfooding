namespace PulseStrip.Game;

using System.Drawing;
using System.Numerics;
using Novolis.Audio;
using Novolis.Game.MenuFlows;
using Novolis.Raylib.Game;
using Novolis.Raylib.Interact;
using Novolis.Raylib.Rendering;
using Novolis.Simulation.Racing.Tracks;
using PulseStrip.Audio;
using PulseStrip.Core;
using PulseStrip.Core.Ai;

internal sealed class PulseStripGame : IDisposable
{
    private static readonly Color VoidBlack = Color.FromArgb(255, 4, 8, 18);
    private static readonly Color NeonCyan = Color.FromArgb(255, 40, 220, 230);
    private static readonly Color NeonMagenta = Color.FromArgb(255, 255, 60, 170);
    private static readonly Color NeonAmber = Color.FromArgb(255, 255, 180, 60);
    private static readonly Color TrackBlue = Color.FromArgb(255, 20, 50, 120);
    private static readonly Color WallPink = Color.FromArgb(220, 255, 40, 120);
    private static readonly Color PlayerHull = Color.FromArgb(255, 230, 240, 255);
    private static readonly Color AiHull = Color.FromArgb(255, 80, 200, 140);

    private readonly string _contentDir;
    private readonly IAudioEngine _audio;
    private readonly PulseStripSfx _sfx;
    private readonly GameScreenStack _screens = new();

    private int _circuitIndex;
    private HoverRaceSimulation? _sim;
    private PlayerHoverController? _player;
    private readonly List<VfxSpark> _sparks = [];
    private float _resultsTimer;
    private string _status = "";

    private readonly record struct VfxSpark(Vector3 Pos, Vector3 Vel, float Life, Color Color);

    public PulseStripGame(string contentDir, IAudioEngine audio, bool smoke)
    {
        _ = smoke;
        _contentDir = contentDir;
        _audio = audio;
        _sfx = new PulseStripSfx(audio, contentDir);
    }

    public void Initialize(RayGameContext ctx)
    {
        _sfx.EnsureGenerated();
        _screens.PushAsync(new TitleScreen()).GetAwaiter().GetResult();
        _sfx.Play("blip");
    }

    public void Update(RayGameContext ctx)
    {
        var screen = _screens.Current?.ScreenId ?? "title";
        switch (screen)
        {
            case "title":
                UpdateTitle(ctx);
                break;
            case "circuit":
                UpdateCircuit(ctx);
                break;
            case "race":
                UpdateRace(ctx);
                break;
            case "results":
                UpdateResults(ctx);
                break;
        }
    }

    private void UpdateTitle(RayGameContext ctx)
    {
        ctx.Clear(VoidBlack);
        ctx.HudText("PULSESTRIP", 72, 120, 56, NeonCyan);
        ctx.HudText("Anti-grav circuit racing", 76, 190, 22, NeonMagenta);
        ctx.HudText("ENTER — select circuit", 76, 280, 20, Color.White);
        ctx.HudText("WASD — drive   SHIFT — boost   SPACE — fire", 76, 320, 18, Color.LightGray);
        ctx.HudText("ESC — quit", 76, ctx.Height - 48, 16, Color.Gray);

        if (ctx.IsKeyPressed(KeyboardKey.Enter) || ctx.IsKeyPressed(KeyboardKey.Space))
        {
            _sfx.Play("blip");
            _screens.PushAsync(new CircuitScreen()).GetAwaiter().GetResult();
        }

        if (ctx.IsKeyPressed(KeyboardKey.Escape))
            Environment.Exit(0);
    }

    private void UpdateCircuit(RayGameContext ctx)
    {
        ctx.Clear(VoidBlack);
        ctx.HudText("SELECT CIRCUIT", 72, 80, 32, NeonAmber);
        for (var i = 0; i < PulseStripCircuits.All.Count; i++)
        {
            var selected = i == _circuitIndex;
            var color = selected ? NeonCyan : Color.White;
            var mark = selected ? ">" : " ";
            ctx.HudText($"{mark} {PulseStripCircuits.DisplayName(i)}", 90, 160 + i * 40, 24, color);
        }

        ctx.HudText("UP/DOWN  ENTER race  ESC back", 72, ctx.Height - 48, 16, Color.LightGray);

        if (ctx.IsKeyPressed(KeyboardKey.Up) || ctx.IsKeyPressed(KeyboardKey.W))
            _circuitIndex = Math.Max(0, _circuitIndex - 1);
        if (ctx.IsKeyPressed(KeyboardKey.Down) || ctx.IsKeyPressed(KeyboardKey.S))
            _circuitIndex = Math.Min(PulseStripCircuits.All.Count - 1, _circuitIndex + 1);
        if (ctx.IsKeyPressed(KeyboardKey.Escape))
        {
            _screens.PopAsync().GetAwaiter().GetResult();
            return;
        }

        if (ctx.IsKeyPressed(KeyboardKey.Enter) || ctx.IsKeyPressed(KeyboardKey.Space))
        {
            _sfx.Play("blip");
            StartRace();
            _screens.PushAsync(new RaceScreen()).GetAwaiter().GetResult();
        }
    }

    private void StartRace()
    {
        var def = PulseStripCircuits.ByIndex(_circuitIndex);
        var track = new TrackBuilder().Build(def);
        var brainsDir = Path.Combine(_contentDir, "brains");
        IReadOnlyList<Novolis.MachineLearning.Neural.INeuralNetwork> brains;
        try
        {
            brains = BrainStore.LoadOrTrain(brainsDir, count: 3, trainTrack: BuiltInTracks.MicroCircle);
        }
        catch
        {
            brains = [];
        }

        _player = new PlayerHoverController("You");
        var controllers = new List<IHoverController> { _player };
        for (var i = 0; i < 3; i++)
        {
            if (i < brains.Count)
                controllers.Add(new NeuralHoverController(brains[i]));
            else
                controllers.Add(new FullThrottleHoverController($"Throttle-{i + 1}"));
        }

        _sim = new HoverRaceSimulation(track, controllers, targetLaps: 3);
        _sim.WeaponFired += _ => _sfx.Play("fire");
        _sim.WeaponHit += (_, _) =>
        {
            _sfx.Play("hit");
            SpawnSparks(_sim!.State.Craft[0].Position, NeonMagenta, 8);
        };
        _sim.PickupCollected += (_, _) => _sfx.Play("pickup");
        _sim.LapCompleted += c =>
        {
            if (c.Id == 0)
                _sfx.Play("lap");
        };
        _sparks.Clear();
        _status = PulseStripCircuits.DisplayName(_circuitIndex);
    }

    private void UpdateRace(RayGameContext ctx)
    {
        if (_sim is null || _player is null)
            return;

        _player.Current = ReadPlayerInput(ctx);
        _sim.Tick();

        var player = _sim.State.Craft[0];
        if (player.Boosting)
        {
            SpawnSparks(player.Position - player.Forward * 1.2f, NeonAmber, 2);
            if (_sim.State.Tick % 20 == 0)
                _sfx.Play("boost");
        }

        if (player.Speed > 12)
            SpawnSparks(player.Position - player.Forward * 0.8f, NeonCyan, 1);

        TickSparks(ctx.DeltaSeconds);
        DrawRace(ctx);

        if (_sim.State.RaceFinished || player.Finished || player.Crashed)
        {
            _resultsTimer = 0;
            _screens.PushAsync(new ResultsScreen()).GetAwaiter().GetResult();
        }

        if (ctx.IsKeyPressed(KeyboardKey.Escape))
        {
            while (_screens.Current is not null && _screens.Current.ScreenId != "circuit")
                _screens.PopAsync().GetAwaiter().GetResult();
            _sim = null;
        }
    }

    private void UpdateResults(RayGameContext ctx)
    {
        ctx.Clear(VoidBlack);
        var craft = _sim?.State.Craft;
        ctx.HudText("RESULTS", 72, 80, 40, NeonAmber);
        if (craft is not null)
        {
            var ranked = craft.OrderBy(c => c.Place).ToList();
            for (var i = 0; i < ranked.Count; i++)
            {
                var c = ranked[i];
                var tag = c.Crashed ? "DNF" : $"P{c.Place}";
                var color = c.Id == 0 ? NeonCyan : Color.White;
                ctx.HudText($"{tag}  {c.Name}  laps={c.CompletedLaps}  hp={c.Health:0}", 90, 160 + i * 36, 22, color);
            }
        }

        _resultsTimer += ctx.DeltaSeconds;
        ctx.HudText(_resultsTimer > 1.2f ? "ENTER — circuits   ESC — title" : "…", 72, ctx.Height - 48, 18, Color.LightGray);

        if (_resultsTimer > 1.2f && (ctx.IsKeyPressed(KeyboardKey.Enter) || ctx.IsKeyPressed(KeyboardKey.Space)))
        {
            while (_screens.Current is not null && _screens.Current.ScreenId != "circuit")
                _screens.PopAsync().GetAwaiter().GetResult();
            _sim = null;
            _sfx.Play("blip");
        }

        if (ctx.IsKeyPressed(KeyboardKey.Escape))
        {
            while (_screens.Current is not null && _screens.Current.ScreenId != "title")
                _screens.PopAsync().GetAwaiter().GetResult();
            _sim = null;
        }
    }

    private void DrawRace(RayGameContext ctx)
    {
        var sim = _sim!;
        var player = sim.State.Craft[0];
        ctx.Clear(VoidBlack);

        var chase = player.Position - player.Forward * 8f + Vector3.UnitY * 4.5f;
        var target = player.Position + player.Forward * 6f;
        var camera = Camera.Perspective(chase, target, Vector3.UnitY, 62f);
        ctx.BeginWorld(camera);

        DrawTrack(ctx, sim.Track);
        DrawPickups(ctx, sim);
        DrawCraft(ctx, sim);
        DrawProjectiles(ctx, sim);
        DrawSparks(ctx);

        ctx.EndWorld();
        DrawHud(ctx, sim, player);
    }

    private static void DrawTrack(RayGameContext ctx, RaceTrack track)
    {
        var samples = track.CenterLineSamples;
        var half = (float)track.Geometry.HalfWidth;
        for (var i = 0; i < samples.Count; i++)
        {
            var a = samples[i];
            var b = samples[(i + 1) % samples.Count];
            var mid = (a + b) * 0.5f;
            mid.Y = 0.05f;
            var tangent = Vector3.Normalize(b - a);
            if (tangent.LengthSquared() < 1e-6f)
                continue;
            var len = Vector3.Distance(a, b);
            ctx.DrawShipBox(mid, new Vector3(half * 2f, 0.12f, Math.Max(0.4f, len)), TrackBlue);

            var normal = new Vector3(tangent.Z, 0f, -tangent.X);
            var left = mid + normal * half;
            var right = mid - normal * half;
            left.Y = 1.1f;
            right.Y = 1.1f;
            ctx.DrawShipBox(left, new Vector3(0.25f, 2.2f, Math.Max(0.4f, len)), WallPink);
            ctx.DrawShipBox(right, new Vector3(0.25f, 2.2f, Math.Max(0.4f, len)), WallPink);

            if (i % 8 == 0)
                ctx.DrawBolt(a with { Y = 0.2f }, b with { Y = 0.2f }, NeonCyan);
        }

        foreach (var gate in track.Gates)
        {
            var ga = gate.A with { Y = 2.5f };
            var gb = gate.B with { Y = 2.5f };
            ctx.DrawBolt(ga, gb, NeonMagenta);
        }
    }

    private static void DrawPickups(RayGameContext ctx, HoverRaceSimulation sim)
    {
        foreach (var pad in sim.State.Pickups)
        {
            if (!pad.Available)
                continue;
            var color = pad.Kind == PickupKind.Weapon ? NeonAmber : NeonCyan;
            ctx.DrawGlowSphere(pad.Position, 0.55f, color);
            ctx.DrawGlowSphereWires(pad.Position, 0.75f, color);
        }
    }

    private static void DrawCraft(RayGameContext ctx, HoverRaceSimulation sim)
    {
        foreach (var c in sim.State.Craft)
        {
            if (c.Crashed)
                continue;
            var color = c.Id == 0 ? PlayerHull : AiHull;
            var size = new Vector3(1.1f, 0.45f, 2.0f);
            ctx.DrawShipBox(c.Position, size, color);
            ctx.DrawShipWires(c.Position, size * 1.05f, c.Boosting ? NeonAmber : NeonCyan);
            if (c.ShieldActive)
                ctx.DrawGlowSphereWires(c.Position, 1.4f, NeonCyan);
            if (c.Boosting)
                ctx.DrawGlowSphere(c.Position - c.Forward * 1.4f, 0.35f, NeonAmber);
        }
    }

    private static void DrawProjectiles(RayGameContext ctx, HoverRaceSimulation sim)
    {
        foreach (var p in sim.State.Projectiles)
        {
            if (!p.Active)
                continue;
            ctx.DrawLaserBolt(p.Position, p.Position + Vector3.Normalize(p.Velocity) * 1.2f, NeonMagenta);
        }
    }

    private void DrawSparks(RayGameContext ctx)
    {
        foreach (var s in _sparks)
            ctx.DrawGlowSphere(s.Pos, 0.12f, s.Color);
    }

    private void DrawHud(RayGameContext ctx, HoverRaceSimulation sim, HoverCraftState player)
    {
        ctx.HudText("PULSESTRIP", 24, 20, 22, NeonCyan);
        ctx.HudText($"{_status}  LAP {Math.Min(player.CompletedLaps + 1, sim.State.TargetLaps)}/{sim.State.TargetLaps}", 24, 50, 18, Color.White);
        ctx.HudText($"P{player.Place}  SPD {player.Speed:0.0}  BOOST {player.BoostFuel * 100:0}%", 24, 78, 18, NeonAmber);
        ctx.HudText($"HP {player.Health:0}  AMMO {player.WeaponAmmo}{(player.ShieldActive ? "  SHIELD" : "")}", 24, 106, 18, player.Health < 35 ? Color.OrangeRed : Color.LightGray);

        var y = 140;
        foreach (var c in sim.State.Craft.OrderBy(x => x.Place))
        {
            var mark = c.Id == 0 ? ">" : " ";
            ctx.HudText($"{mark} P{c.Place} {c.Name}", ctx.Width - 280, y, 16, c.Id == 0 ? NeonCyan : Color.Gray);
            y += 22;
        }
    }

    private static HoverControlDecision ReadPlayerInput(RayGameContext ctx)
    {
        var steer = 0.0;
        if (ctx.IsKeyDown(KeyboardKey.A))
            steer -= 1;
        if (ctx.IsKeyDown(KeyboardKey.D))
            steer += 1;

        var throttle = 0.0;
        var brake = 0.0;
        if (ctx.IsKeyDown(KeyboardKey.W) || ctx.IsKeyDown(KeyboardKey.Up))
            throttle = 1;
        if (ctx.IsKeyDown(KeyboardKey.S) || ctx.IsKeyDown(KeyboardKey.Down))
            brake = 1;
        if (throttle < 0.1 && brake < 0.1)
            throttle = 0.55; // mild auto-cruise

        var boost = ctx.IsKeyDown(KeyboardKey.LeftShift) ? 1.0 : 0.0;
        var fire = ctx.IsKeyPressed(KeyboardKey.Space);
        return new HoverControlDecision(steer, throttle, brake, boost, fire);
    }

    private void SpawnSparks(Vector3 origin, Color color, int count)
    {
        var rng = Random.Shared;
        for (var i = 0; i < count; i++)
        {
            var vel = new Vector3(
                (float)(rng.NextDouble() * 2 - 1),
                (float)(rng.NextDouble() * 2),
                (float)(rng.NextDouble() * 2 - 1));
            _sparks.Add(new VfxSpark(origin, vel * 4f, 0.35f + (float)rng.NextDouble() * 0.25f, color));
        }
    }

    private void TickSparks(float dt)
    {
        for (var i = _sparks.Count - 1; i >= 0; i--)
        {
            var s = _sparks[i];
            s = s with { Pos = s.Pos + s.Vel * dt, Life = s.Life - dt, Vel = s.Vel + Vector3.UnitY * -2f * dt };
            if (s.Life <= 0)
                _sparks.RemoveAt(i);
            else
                _sparks[i] = s;
        }
    }

    public void Dispose()
    {
        _sfx.Dispose();
        _audio.Dispose();
    }
}
