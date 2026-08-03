using Avalonia.Controls;
using Avalonia.Media;
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
            HeaderTitle = "Arrangement — library · tracks · waveforms · fades · export",
        };
        _piano = new MidiPianoWorkspace(musicProject: project)
        {
            HeaderTitle = "Piano Score",
        };

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
            Header = "Piano Score",
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
}
