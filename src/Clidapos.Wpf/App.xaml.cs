using System;
using System.Windows;
using System.Windows.Threading;

namespace Clidapos.Wpf
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"CRASH: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "Unhandled Error");
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"FATAL CRASH: {ex?.Message}\n\n{ex?.StackTrace}",
                "Fatal Error");
        }
    }
}