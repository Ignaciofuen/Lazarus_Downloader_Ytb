using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using YtdlpDesktop.Services;

namespace YtdlpDesktop.ViewModels;

/// <summary>
/// Equivalente al componente App.jsx de la versión React: no tiene lógica
/// propia, solo expone las dos "páginas" para que la ventana principal
/// las muestre en pestañas.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly YtDlpService _servicio;

    public AudioViewModel Audio { get; }
    public VideoViewModel Video { get; }

    [ObservableProperty]
    private string _carpetaDestino;

    [ObservableProperty]
    private string _navegadorCookiesSeleccionado;

    public List<string> NavegadoresDisponibles { get; } = new() { "Ninguno", "chrome", "edge", "firefox", "brave", "opera", "vivaldi" };

    private readonly string _settingsPath = Path.Combine(AppContext.BaseDirectory, "browser.txt");

    public MainViewModel(AudioViewModel audio, VideoViewModel video, YtDlpService servicio)
    {
        Audio = audio;
        Video = video;
        _servicio = servicio;
        _carpetaDestino = servicio.CarpetaDestino;

        if (File.Exists(_settingsPath))
        {
            _navegadorCookiesSeleccionado = File.ReadAllText(_settingsPath).Trim();
            if (!NavegadoresDisponibles.Contains(_navegadorCookiesSeleccionado))
                _navegadorCookiesSeleccionado = "edge";
        }
        else
        {
            _navegadorCookiesSeleccionado = "edge"; // Default
        }

        _servicio.NavegadorCookies = _navegadorCookiesSeleccionado == "Ninguno" ? null : _navegadorCookiesSeleccionado;
    }

    partial void OnNavegadorCookiesSeleccionadoChanged(string value)
    {
        _servicio.NavegadorCookies = value == "Ninguno" ? null : value;
        try { File.WriteAllText(_settingsPath, value); } catch { }
    }

    [ObservableProperty]
    private bool _estaActualizando;

    [ObservableProperty]
    private string _mensajeActualizacion = "";

    [RelayCommand]
    private async Task ActualizarYtDlpAsync()
    {
        EstaActualizando = true;
        MensajeActualizacion = "Actualizando motor...";

        try
        {
            var progress = new Progress<string>(line =>
            {
                MensajeActualizacion = line;
            });

            await _servicio.ActualizarAsync(progress);
            MensajeActualizacion = "Motor yt-dlp actualizado con éxito.";
        }
        catch (Exception ex)
        {
            MensajeActualizacion = ex.Message;
        }
        finally
        {
            EstaActualizando = false;
        }
    }

    [ObservableProperty]
    private string? _archivoCookies;

    [RelayCommand]
    private void CargarArchivoCookies()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecciona el archivo cookies.txt",
            Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            ArchivoCookies = dialog.FileName;
            _servicio.RutaArchivoCookies = ArchivoCookies;
        }
    }

    [RelayCommand]
    private void QuitarArchivoCookies()
    {
        ArchivoCookies = null;
        _servicio.RutaArchivoCookies = null;
    }

    [RelayCommand]
    private void CambiarCarpeta()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Selecciona la carpeta de destino para las descargas"
        };

        if (dialog.ShowDialog() == true)
        {
            CarpetaDestino = dialog.FolderName;
            _servicio.CarpetaDestino = CarpetaDestino;
        }
    }
}
