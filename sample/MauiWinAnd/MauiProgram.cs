using BleLib.Models;
using BleLib.Services;
using Microsoft.Extensions.Logging;

namespace MauiWinAnd;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		
		// Register BluetoothService using factory pattern
		builder.Services.AddSingleton<IBluetoothService>(provider => 
			BluetoothServiceFactory.CreateBluetoothService());

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
