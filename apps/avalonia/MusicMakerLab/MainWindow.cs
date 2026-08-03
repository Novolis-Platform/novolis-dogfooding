using Avalonia.Controls;
using Novolis.Avalonia.Audio;

namespace MusicMakerLab;

internal sealed class MainWindow : Window
{
    readonly AudioEditWorkspace _workspace;

    public MainWindow()
    {
        Title = "Music Maker Lab";
        Width = 1280;
        Height = 780;
        Background = AudioEditPalette.Pane;

        _workspace = new AudioEditWorkspace(FullDemoBuilder.Build())
        {
            HeaderTitle = "Music Maker Lab — library · tracks · waveforms · fades · export",
        };
        Content = _workspace;
        Closed += (_, _) => _workspace.Dispose();
    }
}
