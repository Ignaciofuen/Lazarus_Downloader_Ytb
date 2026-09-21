using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YtdlpDesktop.Services;
using YtdlpDesktop.ViewModels;
using YtdlpDesktop.Views;

namespace YtdlpDesktop;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        string carpetaDestino = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Descargas yt-dlp");

        string? cookiesPath = null;
        string posibleCookies = Path.Combine(AppContext.BaseDirectory, "cookies.txt");
        if (File.Exists(posibleCookies))
            cookiesPath = posibleCookies;

        // Registro de servicios
        services.AddSingleton(new YtDlpService(
            ytdlpPath: "yt-dlp.exe",
            carpetaDestino: carpetaDestino,
            cookiesPath: cookiesPath));

        // Registro de ViewModels
        services.AddTransient<AudioViewModel>();
        services.AddTransient<VideoViewModel>();
        services.AddTransient<MainViewModel>();

        // Registro de la ventana principal
        services.AddTransient<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
