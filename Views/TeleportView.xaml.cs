using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using EternalRingCompanion.Core;
using EternalRingCompanion.Data;

namespace EternalRingCompanion.Views;

public partial class TeleportView : UserControl
{
    private readonly GameSession _session = GameSession.Instance;

    public TeleportView()
    {
        InitializeComponent();
        BuildList();
        Loaded += (_, _) => { _session.StateChanged += OnState; OnState(); };
        Unloaded += (_, _) => _session.StateChanged -= OnState;
    }

    private void OnState() => Dispatcher.BeginInvoke(new Action(() =>
    {
        bool on = _session.IsAttached;
        List.IsEnabled = on;
        List.Opacity = on ? 1 : 0.55;
        Status.Text = on ? "" : "Not connected";
        Status.Foreground = (Brush)FindResource("TextMuted");
    }), DispatcherPriority.Background);

    private void BuildList()
    {
        var body = new StackPanel();
        var areas = GameData.WarpAreas;
        for (int i = 0; i < areas.Length; i++)
        {
            body.Children.Add(BuildAreaRow(areas[i], i + 1));
            if (i < areas.Length - 1)
                body.Children.Add(new Border { Height = 1, Background = (Brush)FindResource("BorderBrush"), Opacity = 0.55 });
        }

        List.Children.Add(new ContentControl
        {
            Style = (Style)FindResource("Frame"),
            Padding = new Thickness(24, 18, 24, 20),
            Content = body
        });
    }

    private FrameworkElement BuildAreaRow(GameData.WarpArea area, int ordinal)
    {
        var grid = new Grid { MinHeight = 46 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var cursor = new Border { Background = (Brush)FindResource("CursorBar"), Visibility = Visibility.Collapsed, Margin = new Thickness(-24, 0, -24, 0) };
        Grid.SetColumnSpan(cursor, 3);
        grid.Children.Add(cursor);
        grid.MouseEnter += (_, _) => cursor.Visibility = Visibility.Visible;
        grid.MouseLeave += (_, _) => cursor.Visibility = Visibility.Collapsed;

        var num = new TextBlock
        {
            Text = ordinal.ToString("00"),
            FontFamily = (FontFamily)FindResource("ValueFont"),
            FontSize = 11,
            Foreground = (Brush)FindResource("TextMuted"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(num, 0);
        grid.Children.Add(num);

        var mid = new StackPanel { Margin = new Thickness(2, 9, 12, 9), VerticalAlignment = VerticalAlignment.Center };
        mid.Children.Add(new TextBlock { Text = area.Name, FontSize = 15, Foreground = (Brush)FindResource("Text") });

        var selectedPart = area.Parts[0];
        if (area.Parts.Count > 1)
        {
            var pills = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 7, 0, 0) };
            foreach (var part in area.Parts)
            {
                var capturedPart = part;
                var pill = new ToggleButton
                {
                    Content = part.Label,
                    Style = (Style)FindResource("PillToggleSm"),
                    IsChecked = ReferenceEquals(part, area.Parts[0])
                };
                pill.Checked += (s, _) =>
                {
                    selectedPart = capturedPart;
                    foreach (var other in pills.Children.OfType<ToggleButton>())
                        if (!ReferenceEquals(other, s)) other.IsChecked = false;
                };
                pill.Unchecked += (s, _) =>
                {
                    if (pills.Children.OfType<ToggleButton>().All(t => t.IsChecked != true))
                        ((ToggleButton)s!).IsChecked = true;
                };
                pills.Children.Add(pill);
            }
            mid.Children.Add(pills);
        }
        Grid.SetColumn(mid, 1);
        grid.Children.Add(mid);

        var warpBtn = new Button
        {
            Content = "Warp",
            Style = (Style)FindResource("AccentButton"),
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(14, 6, 14, 6)
        };
        warpBtn.Click += (_, _) => DoWarp(area, selectedPart);
        Grid.SetColumn(warpBtn, 2);
        grid.Children.Add(warpBtn);

        return grid;
    }

    private void DoWarp(GameData.WarpArea area, GameData.WarpPart part)
    {
        if (!_session.IsAttached)
        {
            Flash("Not connected", isError: true);
            return;
        }

        bool ok = true;
        ok &= _session.Write(GameData.WarpPositionOffset + 0, BitConverter.GetBytes(part.X));
        ok &= _session.Write(GameData.WarpPositionOffset + 4, BitConverter.GetBytes(part.Y));
        ok &= _session.Write(GameData.WarpPositionOffset + 8, BitConverter.GetBytes(part.Z));
        ok &= _session.Write(GameData.WarpPositionOffset + 12, BitConverter.GetBytes(0f));
        ok &= _session.Write(GameData.WarpOrientationOffset + 0, BitConverter.GetBytes(0f));
        ok &= _session.Write(GameData.WarpOrientationOffset + 4, BitConverter.GetBytes(0f));
        ok &= _session.Write(GameData.WarpOrientationOffset + 8, BitConverter.GetBytes(0f));
        ok &= _session.Write(GameData.WarpHeadingOffset, BitConverter.GetBytes(part.Heading));
        ok &= _session.Write(GameData.WarpTargetIdOffset, BitConverter.GetBytes(part.LevelId));
        ok &= _session.Write(GameData.WarpLoadFlagOffset, BitConverter.GetBytes(1));
        ok &= _session.Write(GameData.WarpTriggerOffset, new byte[] { 1 });

        string where = area.Parts.Count > 1 ? $"{area.Name} · {part.Label}" : area.Name;
        Flash(ok ? $"Warping to {where}…" : "Warp failed — try again", isError: !ok);
    }

    private void Flash(string message, bool isError)
    {
        Status.Text = message;
        Status.Foreground = (Brush)FindResource(isError ? "Danger" : "Good");
    }
}
