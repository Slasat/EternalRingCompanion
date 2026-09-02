using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace EternalRingCompanion;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = v == null ? "" : $"Version {v.Major}.{v.Minor}.{v.Build}";
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}
