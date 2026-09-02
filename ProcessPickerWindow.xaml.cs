using System.Collections.Generic;
using System.Windows;
using EternalRingCompanion.Core;

namespace EternalRingCompanion;

public partial class ProcessPickerWindow : Window
{
    public GameSession.ProcessInfo? Selected { get; private set; }

    public ProcessPickerWindow(IReadOnlyList<GameSession.ProcessInfo> processes)
    {
        InitializeComponent();
        List.ItemsSource = processes;
        if (processes.Count > 0) List.SelectedIndex = 0;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (List.SelectedItem is GameSession.ProcessInfo p)
        {
            Selected = p;
            DialogResult = true;
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }
}
