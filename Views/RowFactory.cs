using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EternalRingCompanion.Views;

/// <summary>
/// Builds the menu-style edit rows and the bronze-framed section panels used across the pages:
/// "Name .......... value  [ SET ]  ◇", with a soft cursor bar on hover.
/// </summary>
internal static class RowFactory
{
    public sealed class ValueRow
    {
        public required FrameworkElement Root { get; init; }
        public required TextBox Box { get; init; }
        public Button? SetButton { get; init; }
        public ToggleButton? Freeze { get; init; }
    }

    public static ValueRow Create(FrameworkElement host, string label, bool withFreeze, string? hint = null)
    {
        Brush B(string key) => (Brush)host.FindResource(key);

        var rowGrid = new Grid { MinHeight = 36 };
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var cursor = new Border
        {
            Background = B("CursorBar"),
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(-8, 0, -8, 0)
        };
        Grid.SetColumnSpan(cursor, 4);
        rowGrid.Children.Add(cursor);
        rowGrid.MouseEnter += (_, _) => cursor.Visibility = Visibility.Visible;
        rowGrid.MouseLeave += (_, _) => cursor.Visibility = Visibility.Collapsed;

        var nameCell = new DockPanel { LastChildFill = true, VerticalAlignment = VerticalAlignment.Center };
        var name = new TextBlock
        {
            Text = label,
            FontSize = 13.5,
            Foreground = B("Text"),
            VerticalAlignment = VerticalAlignment.Center
        };
        DockPanel.SetDock(name, Dock.Left);
        nameCell.Children.Add(name);
        nameCell.Children.Add(new Rectangle
        {
            Fill = B("LeaderBrush"),
            Height = 7,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(10, 0, 12, 4)
        });
        Grid.SetColumn(nameCell, 0);
        rowGrid.Children.Add(nameCell);

        var box = new TextBox
        {
            Width = 92,
            TextAlignment = TextAlignment.Right,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(box, 1);
        rowGrid.Children.Add(box);

        var setBtn = new Button
        {
            Content = "Set",
            Style = (Style)host.FindResource("GhostButton"),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(setBtn, 2);
        rowGrid.Children.Add(setBtn);

        ToggleButton? freeze = null;
        if (withFreeze)
        {
            freeze = new ToggleButton
            {
                Style = (Style)host.FindResource("Switch"),
                Margin = new Thickness(10, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(freeze, 3);
            rowGrid.Children.Add(freeze);
        }

        FrameworkElement root = rowGrid;
        if (!string.IsNullOrEmpty(hint))
        {
            var stack = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };
            stack.Children.Add(rowGrid);
            stack.Children.Add(new TextBlock
            {
                Text = hint,
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Foreground = B("TextMuted"),
                Margin = new Thickness(0, 1, 0, 3)
            });
            root = stack;
        }

        return new ValueRow { Root = root, Box = box, SetButton = setBtn, Freeze = freeze };
    }

    /// <summary>A bronze-framed black section panel with a small blue-grey header and rule.</summary>
    public static FrameworkElement Card(FrameworkElement host, string? title, UIElement content)
    {
        var stack = new StackPanel();
        if (!string.IsNullOrEmpty(title))
        {
            stack.Children.Add(new TextBlock
            {
                Text = title,
                Style = (Style)host.FindResource("SectionLabel")
            });
            stack.Children.Add(new Border { Style = (Style)host.FindResource("Rule") });
        }
        var body = content as FrameworkElement;
        if (body != null) body.Margin = new Thickness(0, 12, 0, 0);
        stack.Children.Add(content);

        return new ContentControl
        {
            Style = (Style)host.FindResource("Frame"),
            Padding = new Thickness(20, 16, 20, 16),
            Margin = new Thickness(0, 0, 0, 14),
            Content = stack
        };
    }
}
