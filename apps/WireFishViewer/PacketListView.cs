using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using WireFishViewer.Capture;

namespace WireFishViewer;

/// <summary>
/// Wireshark-style packet list using ListBox (FluentTheme) instead of DataGrid
/// so rows are visible without the separate DataGrid theme pack.
/// </summary>
internal sealed class PacketListView : DockPanel
{
    private readonly ListBox _list;

    public PacketListView()
    {
        var header = BuildHeader();
        DockPanel.SetDock(header, Dock.Top);
        Children.Add(header);

        _list = new ListBox
        {
            SelectionMode = SelectionMode.Single,
            FontFamily = new FontFamily("Consolas,Courier New,monospace"),
            FontSize = 12,
            ItemTemplate = new FuncDataTemplate<PacketRow>((row, _) => BuildRow(row), supportsRecycling: true),
        };
        Children.Add(_list);
    }

    public IEnumerable? ItemsSource
    {
        get => _list.ItemsSource;
        set => _list.ItemsSource = value;
    }

    public object? SelectedItem
    {
        get => _list.SelectedItem;
        set => _list.SelectedItem = value;
    }

    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged
    {
        add => _list.SelectionChanged += value;
        remove => _list.SelectionChanged -= value;
    }

    private static Border BuildHeader()
    {
        var grid = CreateColumnGrid();
        AddHeaderCell(grid, 0, "#");
        AddHeaderCell(grid, 1, "Time");
        AddHeaderCell(grid, 2, "Source");
        AddHeaderCell(grid, 3, "Destination");
        AddHeaderCell(grid, 4, "Protocol");
        AddHeaderCell(grid, 5, "Length");
        AddHeaderCell(grid, 6, "Info");
        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(4, 4),
            Child = grid,
        };
    }

    private static Control BuildRow(PacketRow row)
    {
        var grid = CreateColumnGrid();
        AddCell(grid, 0, row.Number.ToString());
        AddCell(grid, 1, row.Time);
        AddCell(grid, 2, row.Source);
        AddCell(grid, 3, row.Destination);
        AddCell(grid, 4, row.Protocol);
        AddCell(grid, 5, row.Length.ToString());
        AddCell(grid, 6, row.Info);
        return grid;
    }

    private static Grid CreateColumnGrid() =>
        new()
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(48)),
                new ColumnDefinition(new GridLength(120)),
                new ColumnDefinition(new GridLength(140)),
                new ColumnDefinition(new GridLength(140)),
                new ColumnDefinition(new GridLength(72)),
                new ColumnDefinition(new GridLength(64)),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
            ],
        };

    private static void AddHeaderCell(Grid grid, int column, string text)
    {
        var block = new TextBlock
        {
            Text = text,
            FontWeight = FontWeight.SemiBold,
            FontFamily = new FontFamily("Consolas,Courier New,monospace"),
            FontSize = 12,
            Margin = new Thickness(4, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        Grid.SetColumn(block, column);
        grid.Children.Add(block);
    }

    private static void AddCell(Grid grid, int column, string text)
    {
        var block = new TextBlock
        {
            Text = text,
            Margin = new Thickness(4, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(block, column);
        grid.Children.Add(block);
    }
}
