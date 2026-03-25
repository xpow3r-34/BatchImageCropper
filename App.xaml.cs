using System;
using System.Configuration;
using System.Data;
using System.Windows;

namespace BatchImageCropper;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Add global exception handlers
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
        base.OnStartup(e);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error(e.Exception, "Unhandled dispatcher exception");
        MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nCheck logs for details.", 
                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        Logger.Error(exception, "Unhandled domain exception");
        MessageBox.Show($"A critical error occurred: {exception?.Message}\n\nApplication will close. Check logs for details.", 
                      "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            Logger.Information("Application shutting down...");
            Logger.CloseAndFlush();
        }
        catch (Exception ex)
        {
            // Can't log here since logger might be shutting down
            System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
        }
        finally
        {
            base.OnExit(e);
        }
    }
}

