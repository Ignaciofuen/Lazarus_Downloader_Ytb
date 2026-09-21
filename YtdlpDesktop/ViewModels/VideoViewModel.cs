using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtdlpDesktop.Models;
using YtdlpDesktop.Services;

namespace YtdlpDesktop.ViewModels;

public partial class VideoViewModel : ObservableObject
{
    private readonly YtDlpService _servicio;
    private string _urlConsultada = "";

    [ObservableProperty]
    private string _url = "";

    [ObservableProperty]
    private string _tituloVideo = "";

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

    public ObservableCollection<VideoFormatInfo> Formatos { get; } = new();

    public VideoViewModel(YtDlpService servicio)
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
                FileName = _servicio.CarpetaVideos,
                UseShellExecute = true
            });
        }
        catch { }
    }

    [RelayCommand]
    private async Task ConsultarAsync()
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
        MensajeEstado = "Consultando formatos...";
        EsError = false;
        TituloVideo = string.Empty;
        Formatos.Clear();

        try
        {
            var (titulo, lista) = await _servicio.ObtenerFormatosAsync(Url);
            TituloVideo = titulo;
            _urlConsultada = Url;
            foreach (var f in lista)
                Formatos.Add(f);

            MensajeEstado = "";
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

    // Recibe como parámetro el VideoFormatInfo que el usuario clickeó en la lista
    [RelayCommand]
    private async Task DescargarAsync(VideoFormatInfo formato)
    {
        if (formato == null) return;

        EstaOcupado = true;
        DescargaCompletada = false;
        Progreso = 0;
        MensajeEstado = "Descargando video...";
        EsError = false;

        try
        {
            var progressHandler = new Progress<double>(p =>
            {
                Progreso = p;
                MensajeEstado = $"Descargando video... {p:0.0}%";
            });

            await _servicio.DescargarVideoAsync(_urlConsultada, formato.Height, progressHandler);
            Progreso = 100;
            MensajeEstado = "¡Video descargado con éxito!";
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
