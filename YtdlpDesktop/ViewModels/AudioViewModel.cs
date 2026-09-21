using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtdlpDesktop.Services;

namespace YtdlpDesktop.ViewModels;

/// <summary>
/// [ObservableProperty] genera automáticamente la propiedad pública +
/// el evento PropertyChanged que WPF necesita para refrescar la UI.
/// Es el equivalente a "const [url, setUrl] = useState('')" en React,
/// pero resuelto por el compilador en vez de un Hook.
/// </summary>
public partial class AudioViewModel : ObservableObject
{
    private readonly YtDlpService _servicio;

    [ObservableProperty]
    private string _url = "";

    [ObservableProperty]
    private string _formatoSeleccionado = "mp3";

    [ObservableProperty]
    private bool _estaOcupado;

    [ObservableProperty]
    private string _mensajeEstado = "";

    [ObservableProperty]
    private bool _esError;

    [ObservableProperty]
    private double _progreso;

    [ObservableProperty]
    private bool _descargaCompletada;

    public string[] FormatosDisponibles { get; } = { "mp3", "flac", "wav" };

    public AudioViewModel(YtDlpService servicio)
    {
        _servicio = servicio;
    }

    [RelayCommand]
    private void AbrirCarpeta()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _servicio.CarpetaMusica,
                UseShellExecute = true
            });
        }
        catch { }
    }

    [RelayCommand]
    private async Task DescargarAsync()
    {
        if (string.IsNullOrWhiteSpace(Url)) return;

        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uriResult) || 
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            MensajeEstado = "Por favor, ingresa una URL válida (http/https).";
            EsError = true;
            return;
        }

        EstaOcupado = true;
        DescargaCompletada = false;
        Progreso = 0;
        MensajeEstado = "Descargando...";
        EsError = false;

        try
        {
            var progressHandler = new Progress<double>(p =>
            {
                Progreso = p;
                MensajeEstado = $"Descargando... {p:0.0}%";
            });

            await _servicio.DescargarAudioAsync(Url, FormatoSeleccionado, progressHandler);
            Progreso = 100;
            MensajeEstado = "¡Audio descargado con éxito!";
            DescargaCompletada = true;
            EsError = false;
        }
        catch (YtDlpException ex)
        {
            MensajeEstado = ex.Message;
            EsError = true;
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
