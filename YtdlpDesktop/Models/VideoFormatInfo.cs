namespace YtdlpDesktop.Models;

/// <summary>
/// Representa una resolución de video disponible para un contenido dado.
/// Equivalente al objeto que armábamos en Python dentro de
/// listar_formatos_video().
/// </summary>
public record VideoFormatInfo(int Height, double? Fps, string Ext, double? Tbr)
{
    public string Etiqueta =>
        Fps is not null && Fps != 30
            ? $"{Height}p {Fps:0}fps"
            : $"{Height}p";

    public string Detalle =>
        Tbr is not null ? $".{Ext} · ~{Tbr:0} kbps" : $".{Ext}";
}
