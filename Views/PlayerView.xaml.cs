using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EternalRingCompanion.Core;
using EternalRingCompanion.Data;

namespace EternalRingCompanion.Views;

public partial class PlayerView : UserControl
{
    private readonly GameSession _session = GameSession.Instance;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    private readonly List<(GameData.StatField Field, RowFactory.ValueRow Row)> _statRows = new();
    private TextBox _nameBox = null!;

    public PlayerView()
    {
        InitializeComponent();
        BuildUi();
        Loaded += (_, _) => { _session.StateChanged += OnState; _timer.Start(); OnState(); RefreshAll(); };
        Unloaded += (_, _) => { _session.StateChanged -= OnState; _timer.Stop(); };
        _timer.Tick += (_, _) => RefreshAll();
    }

    private void OnState() => Dispatcher.BeginInvoke(new Action(() =>
    {
        bool on = _session.IsAttached;
        SetConnected(on);
        if (on) RefreshAll();
    }), DispatcherPriority.Background);

    private void SetConnected(bool on)
    {
        RefillBtn.IsEnabled = on;
        Root.IsEnabled = on;
        Root.Opacity = on ? 1.0 : 0.55;
    }

    // ---- build --------------------------------------------------------

    private void BuildUi()
    {
        var vitals = new StackPanel();
        foreach (var f in GameData.PlayerStats)
        {
            var row = RowFactory.Create(this, f.Name, withFreeze: true, hint: f.Hint);
            row.SetButton!.Click += (_, _) => WriteStat(f, row);
            row.Box.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) WriteStat(f, row); };
            row.Freeze!.Checked += (_, _) => WriteStat(f, row, fromFreeze: true);
            row.Freeze.Unchecked += (_, _) => _session.ClearFrozen("stat:" + f.Name);
            _statRows.Add((f, row));
            vitals.Children.Add(row.Root);
        }
        Root.Children.Add(RowFactory.Card(this, "Level & Vitals", vitals));

        var namePanel = new StackPanel();
        var nameRow = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _nameBox = new TextBox { MaxLength = GameData.NameBufferSize - 1 };
        var readNameBtn = new Button { Content = "Read", Style = (Style)FindResource("GhostButton"), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        var renameBtn = new Button { Content = "Rename", Style = (Style)FindResource("AccentButton"), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        readNameBtn.Click += (_, _) => ReadName();
        renameBtn.Click += (_, _) => WriteName();
        Grid.SetColumn(_nameBox, 0); Grid.SetColumn(readNameBtn, 1); Grid.SetColumn(renameBtn, 2);
        nameRow.Children.Add(_nameBox); nameRow.Children.Add(readNameBtn); nameRow.Children.Add(renameBtn);
        namePanel.Children.Add(nameRow);
        namePanel.Children.Add(new TextBlock { Text = "Up to 15 characters.", Style = (Style)FindResource("Muted"), Margin = new Thickness(0, 6, 0, 0) });
        Root.Children.Add(RowFactory.Card(this, "Character Name", namePanel));
    }

    // ---- stats ------------------------------------------------------

    private static byte[] Encode(FieldType type, long v) => type switch
    {
        FieldType.Byte => new[] { (byte)Math.Clamp(v, byte.MinValue, byte.MaxValue) },
        FieldType.Int16 => BitConverter.GetBytes((short)Math.Clamp(v, short.MinValue, short.MaxValue)),
        FieldType.Int32 => BitConverter.GetBytes((int)Math.Clamp(v, int.MinValue, int.MaxValue)),
        FieldType.Int64 => BitConverter.GetBytes(v),
        FieldType.Float => BitConverter.GetBytes((float)v),
        FieldType.Double => BitConverter.GetBytes((double)v),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private void WriteStat(GameData.StatField f, RowFactory.ValueRow row, bool fromFreeze = false)
    {
        if (!_session.IsAttached) return;
        if (!long.TryParse(row.Box.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
        {
            if (fromFreeze) row.Freeze!.IsChecked = false;
            return;
        }
        var bytes = Encode(f.Type, v);
        _session.Write(f.EememOffset, bytes);
        if (row.Freeze?.IsChecked == true)
            _session.SetFrozen("stat:" + f.Name, f.EememOffset, bytes);
    }

    private void OnRefill(object sender, RoutedEventArgs e)
    {
        if (!_session.IsAttached) return;
        var maxHp = _session.ReadValue(GameData.PlayerStats[1].EememOffset, FieldType.Int16);
        var maxMp = _session.ReadValue(GameData.PlayerStats[3].EememOffset, FieldType.Int16);
        if (maxHp is { } h) _session.Write(GameData.PlayerStats[0].EememOffset, BitConverter.GetBytes((short)h));
        if (maxMp is { } m) _session.Write(GameData.PlayerStats[2].EememOffset, BitConverter.GetBytes((short)m));
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (!_session.IsAttached) return;

        foreach (var (f, row) in _statRows)
        {
            if (row.Box.IsKeyboardFocused || row.Freeze?.IsChecked == true) continue;
            var v = _session.ReadValue(f.EememOffset, f.Type);
            row.Box.Text = v?.ToString(CultureInfo.InvariantCulture) ?? "";
        }

        if (!_nameBox.IsKeyboardFocused) ReadName();
    }

    // ---- name -----------------------------------------------------

    private void ReadName()
    {
        var raw = _session.Read(GameData.NameOffset, GameData.NameBufferSize);
        if (raw == null) return;
        int end = Array.IndexOf(raw, (byte)0);
        if (end < 0) end = raw.Length;
        _nameBox.Text = System.Text.Encoding.ASCII.GetString(raw, 0, end);
    }

    private void WriteName()
    {
        if (!_session.IsAttached) return;
        var text = _nameBox.Text;
        if (text.Length > GameData.NameBufferSize - 1) text = text[..(GameData.NameBufferSize - 1)];
        var buf = new byte[GameData.NameBufferSize];
        System.Text.Encoding.ASCII.GetBytes(text, 0, text.Length, buf, 0);
        _session.Write(GameData.NameOffset, buf);
    }
}
