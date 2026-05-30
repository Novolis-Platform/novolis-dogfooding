using Avalonia.Controls;
using Avalonia.Layout;
using Microsoft.Extensions.DependencyInjection;
using Novolis.Avalonia.Studio;
using Novolis.Avalonia.Voice;

namespace NovolisVoiceStudio;

internal sealed class MainWindow : Window
{
    public MainWindow(VoicePreviewController preview)
    {
        Title = "Novolis Voice Studio";
        Width = 1280;
        Height = 860;

        var chrome = StudioChrome.Create();
        var feedback = chrome.CreateFeedback();
        var studio = new VoiceStudioPanel(feedback, preview);

        var statusBar = new DockPanel();
        DockPanel.SetDock(chrome.FlashLine, Dock.Bottom);
        DockPanel.SetDock(chrome.StatusLine, Dock.Bottom);
        statusBar.Children.Add(chrome.FlashLine);
        statusBar.Children.Add(chrome.StatusLine);

        var root = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        Grid.SetRow(studio, 0);
        root.Children.Add(studio);
        Grid.SetRow(statusBar, 1);
        root.Children.Add(statusBar);

        Content = root;
        Opened += (_, _) => feedback.Flash("Edit presets, preview speech, copy C# into Voice.Profiles / Atc.");
    }
}
