using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MauiWinAnd.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.InitializeComponent();
		
		// Set up global exception handlers
		SetupGlobalExceptionHandling();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	private void SetupGlobalExceptionHandling()
	{
		// Handle unhandled exceptions
		AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
		
		// Handle unobserved task exceptions
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		
		// Handle WinUI unhandled exceptions
		this.UnhandledException += OnWinUIUnhandledException;
		
		// Handle WinUI unhandled exceptions in debug mode
#if DEBUG
		this.UnhandledException += OnDebugUnhandledException;
#endif
	}

	private void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
	{
		var exception = e.ExceptionObject as Exception;
		LogException("AppDomain.UnhandledException", exception);
		
		// Log to debug output
		Debug.WriteLine($"=== UNHANDLED EXCEPTION (AppDomain) ===");
		Debug.WriteLine($"Exception Type: {exception?.GetType().Name ?? "Unknown"}");
		Debug.WriteLine($"Message: {exception?.Message ?? "No message"}");
		Debug.WriteLine($"Stack Trace: {exception?.StackTrace ?? "No stack trace"}");
		if (exception?.InnerException != null)
		{
			Debug.WriteLine($"Inner Exception: {exception.InnerException.GetType().Name} - {exception.InnerException.Message}");
		}
		Debug.WriteLine($"Is Terminating: {e.IsTerminating}");
		Debug.WriteLine("=========================================");
	}

	private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
	{
		LogException("TaskScheduler.UnobservedTaskException", e.Exception);
		
		// Log to debug output
		Debug.WriteLine($"=== UNOBSERVED TASK EXCEPTION ===");
		Debug.WriteLine($"Exception Type: {e.Exception.GetType().Name}");
		Debug.WriteLine($"Message: {e.Exception.Message}");
		Debug.WriteLine($"Stack Trace: {e.Exception.StackTrace}");
		if (e.Exception.InnerException != null)
		{
			Debug.WriteLine($"Inner Exception: {e.Exception.InnerException.GetType().Name} - {e.Exception.InnerException.Message}");
		}
		Debug.WriteLine("=================================");
		
		// Mark as observed to prevent app crash
		e.SetObserved();
	}

	private void OnWinUIUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
	{
		LogException("WinUI.UnhandledException", e.Exception);
		
		// Log to debug output
		Debug.WriteLine($"=== WINUI UNHANDLED EXCEPTION ===");
		Debug.WriteLine($"Exception Type: {e.Exception.GetType().Name}");
		Debug.WriteLine($"Message: {e.Exception.Message}");
		Debug.WriteLine($"Stack Trace: {e.Exception.StackTrace}");
		if (e.Exception.InnerException != null)
		{
			Debug.WriteLine($"Inner Exception: {e.Exception.InnerException.GetType().Name} - {e.Exception.InnerException.Message}");
		}
		Debug.WriteLine($"Handled: {e.Handled}");
		Debug.WriteLine("===================================");
		
		// Set as handled to prevent app crash
		e.Handled = true;
	}

#if DEBUG
	private void OnDebugUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
	{
		// In debug mode, break into debugger if attached
		if (Debugger.IsAttached)
		{
			Debugger.Break();
		}
	}
#endif

	private void LogException(string source, Exception? exception)
	{
		if (exception == null) return;
		
		try
		{
			// Log to file (optional - you can implement file logging here)
			var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {source}: {exception.GetType().Name} - {exception.Message}";
			Debug.WriteLine(logMessage);
			
			// You could also write to a log file here
			// File.AppendAllText("app_exceptions.log", logMessage + Environment.NewLine);
		}
		catch
		{
			// If logging fails, at least write to debug output
			Debug.WriteLine($"Failed to log exception from {source}: {exception.Message}");
		}
	}
}

