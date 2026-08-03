using Novolis.Audio.Edit;

namespace MusicMakerLab;

/// <summary>Seeds a small Music Maker–style demo: library tones + two tracks with fades.</summary>
internal static class FullDemoBuilder
{
    public static MusicProject Build()
    {
        var project = new MusicProject("Music Maker Lab");
        var lead = AudioEditOps.AddTrack(project, "Lead");
        var pad = AudioEditOps.AddTrack(project, "Pad");

        var a = AudioEditOps.AddTone(project, "A3 bed", 220, TimeSpan.FromSeconds(4), amplitude: 0.2);
        var c = AudioEditOps.AddTone(project, "C4 pluck", 261.63, TimeSpan.FromSeconds(1.5), amplitude: 0.28);
        var e = AudioEditOps.AddTone(project, "E4 pluck", 329.63, TimeSpan.FromSeconds(1.5), amplitude: 0.28);
        AudioEditOps.AddTone(project, "G4 spare", 392, TimeSpan.FromSeconds(1.2), amplitude: 0.25);

        var bed = AudioEditOps.PlaceClip(project, pad, a, TimeSpan.Zero);
        AudioEditOps.SetClipEnvelope(bed, gain: 0.7f, fadeIn: TimeSpan.FromMilliseconds(200), fadeOut: TimeSpan.FromSeconds(0.8));

        var p1 = AudioEditOps.PlaceClip(project, lead, c, TimeSpan.FromSeconds(0.5));
        AudioEditOps.SetClipEnvelope(p1, fadeIn: TimeSpan.FromMilliseconds(30), fadeOut: TimeSpan.FromMilliseconds(120));
        var p2 = AudioEditOps.PlaceClip(project, lead, e, TimeSpan.FromSeconds(2.2));
        AudioEditOps.SetClipEnvelope(p2, fadeIn: TimeSpan.FromMilliseconds(30), fadeOut: TimeSpan.FromMilliseconds(200));

        return project;
    }
}
