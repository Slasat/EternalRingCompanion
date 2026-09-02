using System;
using System.Windows;
using System.Windows.Threading;

namespace EternalRingCompanion;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        // Headless self-check: build the whole visual tree once and exit. Used to verify the
        // XAML parses and every StaticResource resolves without opening a window.
        if (Array.Exists(e.Args, a => a.Equals("--smoke", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var w = new MainWindow();
                w.Measure(new Size(1120, 760));
                var a = new AboutWindow();
                a.Measure(new Size(460, 300));
                Console.Error.WriteLine("smoke: OK");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("smoke: FAIL\n" + ex);
                Environment.Exit(1);
            }
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // If the main window is up, keep running after showing the error. If it never came up
        // (e.g. a startup failure), there is nothing to close by hand — exit so we don't leave
        // a windowless zombie process behind.
        bool hasWindow = MainWindow is { IsLoaded: true };

        MessageBox.Show(
            "Something went wrong:\n\n" + e.Exception.Message,
            "Eternal Ring Companion", MessageBoxButton.OK, MessageBoxImage.Warning);

        e.Handled = true;
        if (!hasWindow)
            Environment.Exit(1);
    }
}
