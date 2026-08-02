using PulseStrip.Core;
using PulseStrip.Core.Ai;
using Novolis.Simulation.Racing.Tracks;

namespace PulseStrip.Tests;

public class HoverRaceSimulationTests
{
    [Test]
    public async Task Track_Build_Produces_Centerline_And_Gates()
    {
        var track = new TrackBuilder().Build(BuiltInTracks.CompactOval);
        await Assert.That(track.CenterLineSamples.Count).IsGreaterThan(10);
        await Assert.That(track.Gates.Count).IsGreaterThan(0);
        await Assert.That(track.Geometry.HalfWidth).IsGreaterThan(0);
    }

    [Test]
    public async Task Hover_Tick_Advances_Progress_Without_Immediate_Crash()
    {
        var track = new TrackBuilder().Build(BuiltInTracks.MicroCircle);
        var player = new PlayerHoverController("T")
        {
            Current = new HoverControlDecision(0, 1, 0, 0, false),
        };
        var sim = new HoverRaceSimulation(track, [player], targetLaps: 1);
        for (var i = 0; i < 120; i++)
            sim.Tick();

        await Assert.That(sim.State.Tick).IsEqualTo(120);
        await Assert.That(sim.State.Craft[0].Crashed).IsFalse();
        await Assert.That(sim.State.Craft[0].TrackProgress).IsGreaterThan(0);
    }

    [Test]
    public async Task Weapon_Fire_Consumes_Ammo_And_Spawns_Projectile()
    {
        var track = new TrackBuilder().Build(BuiltInTracks.MicroCircle);
        var player = new PlayerHoverController("T")
        {
            Current = new HoverControlDecision(0, 0.2, 0, 0, true),
        };
        var sim = new HoverRaceSimulation(track, [player], targetLaps: 1);
        sim.State.Craft[0].WeaponAmmo = 2;
        sim.Tick();
        await Assert.That(sim.State.Craft[0].WeaponAmmo).IsEqualTo(1);
        await Assert.That(sim.State.Projectiles.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Neural_Controller_Clamps_Outputs()
    {
        var net = Novolis.MachineLearning.Neural.DenseNetwork.Create(
            "clamp-test",
            HoverRaceSimulation.SensorInputSize,
            [8],
            HoverRaceSimulation.ControlOutputSize,
            random: new Random(1));
        var controller = new NeuralHoverController(net);
        var track = new TrackBuilder().Build(BuiltInTracks.MicroCircle);
        var sim = new HoverRaceSimulation(track, [controller], targetLaps: 1);
        for (var i = 0; i < 30; i++)
            sim.Tick();
        await Assert.That(sim.State.Craft[0].Crashed).IsFalse();
    }

    [Test]
    public async Task Evolutionary_Trainer_Produces_Champion()
    {
        var settings = new EvolutionaryHoverTrainerSettings(
            Track: BuiltInTracks.MicroCircle,
            PopulationSize: 6,
            Generations: 2,
            MaxTicksPerEpisode: 200,
            RandomSeed: 7);
        var result = new EvolutionaryHoverTrainer().Train(settings);
        await Assert.That(result.Champion).IsNotNull();
        await Assert.That(result.BestFitnessPerGeneration.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Smoke_Runner_Exits_Zero()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pulsestrip-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var code = PulseStripSmoke.Run(dir);
            await Assert.That(code).IsEqualTo(0);
            await Assert.That(Directory.GetFiles(Path.Combine(dir, "brains"), "*.json").Length).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }
}
