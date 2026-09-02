using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using EternalRingCompanion.Core;
using EternalRingCompanion.Data;

namespace EternalRingCompanion.Views;

public partial class InventoryView : UserControl
{
    private readonly GameSession _session = GameSession.Instance;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    private GameData.InvCategory _category = GameData.Inventory[0];
    private readonly List<(GameData.InvItem Item, TextBox Box)> _rows = new();

    public InventoryView()
    {
        InitializeComponent();

        for (int i = 0; i < GameData.Inventory.Length; i++)
        {
            var cat = GameData.Inventory[i];
            var pill = new ToggleButton
            {
                Content = cat.Name,
                Style = (Style)FindResource("PillToggle"),
                IsChecked = i == 0
            };
            pill.Checked += (_, _) =>
            {
                foreach (var child in CategoryBar.Items)
                    if (child is ToggleButton tb && !ReferenceEquals(tb, pill)) tb.IsChecked = false;
                _category = cat;
                BuildRows();
            };
            pill.Unchecked += (s, _) => { if (CategoryBar.Items.Cast<ToggleButton>().All(t => t.IsChecked != true)) ((ToggleButton)s!).IsChecked = true; };
            CategoryBar.Items.Add(pill);
        }

        BuildRows();

        Loaded += (_, _) => { _session.StateChanged += OnState; _timer.Start(); OnState(); RefreshValues(); };
        Unloaded += (_, _) => { _session.StateChanged -= OnState; _timer.Stop(); };
        _timer.Tick += (_, _) => RefreshValues();
    }

    private bool IsEquipmentCategory => _category.Name is "Weapons" or "Key Items";
    private int FillValue => IsEquipmentCategory ? 1 : 99;

    private void OnState() => Dispatcher.BeginInvoke(new Action(() =>
    {
        bool on = _session.IsAttached;
        Rows.IsEnabled = on; FillBtn.IsEnabled = on; ClearBtn.IsEnabled = on;
        Rows.Opacity = on ? 1 : 0.55;
        Status.Text = on ? "" : "Connect to the emulator to read and change your inventory.";
        if (on) RefreshValues();
    }), DispatcherPriority.Background);

    private void BuildRows()
    {
        Rows.Children.Clear();
        _rows.Clear();

        foreach (var item in _category.Items)
        {
            var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var label = new TextBlock
            {
                Text = item.Name,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13
            };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            var box = new TextBox { VerticalContentAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center };
            box.KeyDown += (_, e) => { if (e.Key == Key.Enter) WriteRow(item, box); };
            Grid.SetColumn(box, 1);
            grid.Children.Add(box);

            var setBtn = new Button { Content = "Set", Style = (Style)FindResource("GhostButton"), Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(12, 7, 12, 7), VerticalAlignment = VerticalAlignment.Center };
            setBtn.Click += (_, _) => WriteRow(item, box);
            Grid.SetColumn(setBtn, 2);
            grid.Children.Add(setBtn);

            var maxBtn = new Button { Content = IsEquipmentCategory ? "Own" : "Max", Style = (Style)FindResource("GhostButton"), Margin = new Thickness(6, 0, 0, 0), Padding = new Thickness(12, 7, 12, 7), VerticalAlignment = VerticalAlignment.Center };
            maxBtn.Click += (_, _) => { box.Text = FillValue.ToString(); WriteRow(item, box); };
            Grid.SetColumn(maxBtn, 3);
            grid.Children.Add(maxBtn);

            _rows.Add((item, box));
            Rows.Children.Add(grid);
        }

        if (_session.IsAttached) RefreshValues();
    }

    private void WriteRow(GameData.InvItem item, TextBox box)
    {
        if (!_session.IsAttached) return;
        if (!int.TryParse(box.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return;
        v = Math.Clamp(v, 0, 255);
        _session.Write(item.EememOffset, new[] { (byte)v });
        box.Text = v.ToString();
    }

    private void RefreshValues()
    {
        if (!_session.IsAttached) return;
        foreach (var (item, box) in _rows)
        {
            if (box.IsKeyboardFocused) continue;
            var raw = _session.Read(item.EememOffset, 1);
            box.Text = raw != null ? raw[0].ToString() : "";
        }
    }

    private void OnFill(object sender, RoutedEventArgs e) => SetAll(FillValue);
    private void OnClear(object sender, RoutedEventArgs e) => SetAll(0);

    private void SetAll(int value)
    {
        if (!_session.IsAttached) return;
        foreach (var (item, box) in _rows)
        {
            _session.Write(item.EememOffset, new[] { (byte)Math.Clamp(value, 0, 255) });
            box.Text = value.ToString();
        }
    }
}
