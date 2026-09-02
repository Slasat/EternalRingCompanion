using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using EternalRingCompanion.Core;
using EternalRingCompanion.Views;

namespace EternalRingCompanion;

public partial class MainWindow : Window
{
    private readonly GameSession _session = GameSession.Instance;

    private readonly PlayerView _playerView = new();
    private readonly InventoryView _inventoryView = new();
    private readonly TeleportView _teleportView = new();

    public MainWindow()
    {
        InitializeComponent();

        PageHost.Content = _playerView;

        _session.StateChanged += OnSessionStateChanged;
        Loaded += OnLoaded;
        Closed += (_, _) => _session.StateChanged -= OnSessionStateChanged;
        StateChanged += (_, _) => AdjustForMaximize();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        AdjustForMaximize();
        _session.TryAutoAttach();
        RefreshStatus();
    }

    // ---- window chrome ----------------------------------------------------

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximize(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnAbout(object sender, RoutedEventArgs e)
        => new AboutWindow { Owner = this }.ShowDialog();

    private void AdjustForMaximize()
    {
        // WindowChrome clips a few px when maximized; compensate with padding.
        var root = (Border)Content;
        bool max = WindowState == WindowState.Maximized;
        root.Padding = max ? new Thickness(7) : new Thickness(0);

        var stroke = (Brush)FindResource("TextMuted");
        if (max)
        {
            var g = new Grid { Width = 13, Height = 13 };
            g.Children.Add(new Rectangle
            {
                Width = 8, Height = 8, Stroke = stroke, StrokeThickness = 1,
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top
            });
            g.Children.Add(new Rectangle
            {
                Width = 8, Height = 8, Stroke = stroke, StrokeThickness = 1,
                Fill = (Brush)FindResource("WindowBg"),
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom
            });
            MaxBtn.Content = g;
            MaxBtn.ToolTip = "Restore";
        }
        else
        {
            MaxBtn.Content = new Rectangle { Width = 10, Height = 10, Stroke = stroke, StrokeThickness = 1, Fill = Brushes.Transparent };
            MaxBtn.ToolTip = "Maximize";
        }
    }

    // ---- navigation -----------------------------------------------------

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (PageHost == null) return;
        if (sender == NavPlayer) PageHost.Content = _playerView;
        else if (sender == NavInventory) PageHost.Content = _inventoryView;
        else if (sender == NavTeleport) PageHost.Content = _teleportView;
    }

    // ---- connection ----------------------------------------------------

    private void OnConnectClick(object sender, RoutedEventArgs e)
    {
        if (_session.IsAttached)
        {
            _session.Detach();
            RefreshStatus();
            return;
        }

        var candidates = GameSession.ListCandidateProcesses();
        var pcsx2 = candidates.Where(p => p.Name.Contains("pcsx2", StringComparison.OrdinalIgnoreCase)).ToList();

        GameSession.ProcessInfo? pick = null;
        if (pcsx2.Count == 1)
            pick = pcsx2[0];
        else
        {
            var chooseFrom = pcsx2.Count > 0 ? pcsx2 : candidates;
            var dlg = new ProcessPickerWindow(chooseFrom) { Owner = this };
            if (dlg.ShowDialog() == true) pick = dlg.Selected;
        }

        if (pick == null) return;

        if (!_session.Attach(pick.Value.Pid))
        {
            MessageBox.Show(this,
                "Could not open that process. Make sure this app is running as administrator and that PCSX2 is still open.",
                "Eternal Ring Companion", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else if (_session.ResolveEememBase() == null)
        {
            MessageBox.Show(this,
                "Connected to the process, but Eternal Ring's memory isn't loaded yet. Load into your save first, then reconnect.",
                "Eternal Ring Companion", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        RefreshStatus();
    }

    private void OnSessionStateChanged() => Dispatcher.BeginInvoke(new Action(RefreshStatus), DispatcherPriority.Background);

    private void RefreshStatus()
    {
        bool on = _session.IsAttached;
        StatusDot.Fill = (SolidColorBrush)(on ? FindResource("Good") : FindResource("Danger"));
        StatusText.Text = on ? "Linked" : "No link";
        StatusSub.Text = on
            ? $"{_session.AttachedProcessName} · pid {_session.AttachedPid}"
            : "start Eternal Ring in PCSX2, then link";
        ConnectBtn.Content = on ? "Unlink" : "Link";
    }
}
