using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.DependencyInjection;

namespace WireFishViewer;

public class App : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        // DataGrid ships unstyled; without this the packet list is blank.
        Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")));
        RequestedThemeVariant = ThemeVariant.Light;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = Program.ApplicationHost.Services.GetRequiredService<MainWindow>();

        base.OnFrameworkInitializationCompleted();
    }
}
