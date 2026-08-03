using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Audio.Midi;
using Novolis.Avalonia.Audio;

namespace MusicMakerLab;

internal sealed class MainWindow : Window
{
    readonly AudioEditWorkspace _arrangement;
    readonly MidiPianoWorkspace _piano;

    public MainWindow()
    {
        Title = "Music Maker Lab";
        Width = 1280;
        Height = 820;
        Background = AudioEditPalette.Pane;

        var project = FullDemoBuilder.Build();
        _arrangement = new AudioEditWorkspace(project)
        {
            HeaderTitle = "Arrangement — free library + By The Sword preview",
        };
        _piano = new MidiPianoWorkspace(musicProject: project)
        {
            HeaderTitle = "Orchestral Score — demos + free MIDI",
        };

        WireFreeMedia(_piano);
        _piano.RefreshFreeMidiList();

        var tabs = new TabControl
        {
            Background = AudioEditPalette.Pane,
        };
        tabs.Items.Add(new TabItem
        {
            Header = "Arrangement",
            Content = _arrangement,
            Foreground = Brushes.White,
        });
        tabs.Items.Add(new TabItem
        {
            Header = "Orchestral Score",
            Content = _piano,
            Foreground = Brushes.White,
        });
        tabs.SelectionChanged += (_, _) =>
        {
            if (tabs.SelectedIndex == 0)
                _arrangement.Refresh();
            else
                _piano.Focus();
        };

        Content = tabs;
        Closed += (_, _) =>
        {
            _arrangement.Dispose();
            _piano.Dispose();
        };
    }

    void WireFreeMedia(MidiPianoWorkspace piano)
    {
        piano.SyncFreeLibrary = () =>
        {
            FreeMediaLibrary.EnsureCached(log: msg => Console.WriteLine("  " + msg));
            var n = FreeMediaLibrary.ImportAudioIntoProject(_arrangement.Project);
            Console.WriteLine($"Imported {n} free SFX.");
            Avalonia.Threading.Dispatcher.UIThread.Post(() => _arrangement.Refresh());
        };

        piano.ListFreeMidiTitles = () =>
            FreeMediaLibrary.CachedMidiFiles().Select(x => x.Entry.Title).ToList();

        piano.ResolveFreeMidi = title =>
        {
            var hit = FreeMediaCatalog.MidiEntries.FirstOrDefault(e =>
                string.Equals(e.Title, title, StringComparison.OrdinalIgnoreCase));
            return hit is null ? null : FreeMediaLibrary.LoadMidiScore(hit);
        };

        piano.CreateFreeAudioSketch = () =>
        {
            // Prefer a mid-length Mixkit clip for a more interesting sketch.
            foreach (var id in new[] { "mixkit-2563", "mixkit-3003", "mixkit-2004", "mixkit-2573" })
            {
                var entry = FreeMediaCatalog.All.FirstOrDefault(e => e.Id == id);
                if (entry is null)
                    continue;
                var score = FreeMediaLibrary.SketchFromFreeAudio(entry);
                if (score is not null)
                    return score;
            }

            foreach (var entry in FreeMediaCatalog.AudioEntries)
            {
                var score = FreeMediaLibrary.SketchFromFreeAudio(entry);
                if (score is not null)
                    return score;
            }

            return null;
        };
    }
}
