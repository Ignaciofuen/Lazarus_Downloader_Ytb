using System.Diagnostics;
using System.IO;
using System.Text.Json;
using YtdlpDesktop.Models;

namespace YtdlpDesktop.Services;

/// <summary>
/// Envuelve yt-dlp.exe como proceso externo. Equivalente directo del
/// ytdlp.service.js de la versión Electron y del script de Python:
/// misma lógica de reintento, misma exclusión del cliente "android_vr".
/// </summary>
public class YtDlpService
{
    private readonly string _ytdlpPath;
    public string CarpetaDestino { get; set; }
    public string? NavegadorCookies { get; set; }
    public string? RutaArchivoCookies { get; set; }

    public string CarpetaMusica => Path.Combine(CarpetaDestino, "Musica");
    public string CarpetaVideos => Path.Combine(CarpetaDestino, "Videos");

    public YtDlpService(string ytdlpPath, string carpetaDestino, string? cookiesPath = null)
    {
        _ytdlpPath = ytdlpPath;
        RutaArchivoCookies = cookiesPath;
        CarpetaDestino = carpetaDestino;
    }

    /// <summary>
    /// Ejecuta yt-dlp.exe con los argumentos dados. Devuelve stdout si
    /// termina bien (código 0); lanza YtDlpException con stderr si falla.
    /// Equivalente a la función ejecutarYtDlp() de Node o el
    /// try/except alrededor de ydl.download() en Python.
    /// </summary>
    private async Task<string> EjecutarAsync(IEnumerable<string> args, IProgress<double>? progress = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ytdlpPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        Process procesoOriginal;
        try
        {
            procesoOriginal = Process.Start(psi) ?? throw new Exception();
        }
        catch (Exception)
        {
            throw new YtDlpException("No se pudo encontrar 'yt-dlp.exe'. Asegúrate de tenerlo instalado en tu PC o pegarlo en la misma carpeta que el programa.");
        }

        using var proceso = procesoOriginal;

        var outputBuilder = new System.Text.StringBuilder();

        var outputTask = Task.Run(async () =>
        {
            DateTime lastReportTime = DateTime.MinValue;
            double lastPercent = -1;

            while (!proceso.StandardOutput.EndOfStream)
            {
                var line = await proceso.StandardOutput.ReadLineAsync();
                if (line == null) continue;

                outputBuilder.AppendLine(line);

                if (progress != null && line.StartsWith("[download]"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"\[download\]\s+(?<percent>[\d\.]+)%");
                    if (match.Success && double.TryParse(match.Groups["percent"].Value, System.Globalization.CultureInfo.InvariantCulture, out double p))
                    {
                        var now = DateTime.Now;
                        // Evita saturar el hilo de la UI: actualiza máximo cada 200ms o si avanzó 1%
                        if ((now - lastReportTime).TotalMilliseconds > 200 || Math.Abs(p - lastPercent) >= 1.0)
                        {
                            lastReportTime = now;
                            lastPercent = p;
                            progress.Report(p);
                        }
                    }
                }
            }
        });

        string errores = await proceso.StandardError.ReadToEndAsync();
        await Task.WhenAll(outputTask, proceso.WaitForExitAsync());

        if (proceso.ExitCode != 0)
        {
            string msjError = string.IsNullOrWhiteSpace(errores) ? $"yt-dlp terminó con código {proceso.ExitCode}" : errores;

            if (msjError.Contains("Could not copy Chrome cookie database", StringComparison.OrdinalIgnoreCase))
            {
                msjError = "ATENCIÓN: Tu navegador está bloqueando las cookies.\nCierra tu navegador (Chrome/Edge/Brave) por completo e inténtalo de nuevo. O mejor aún, usa 'firefox' en el menú de abajo, ya que no tiene este problema.";
            }
            else if (msjError.Contains("Failed to decrypt with DPAPI", StringComparison.OrdinalIgnoreCase))
            {
                msjError = "ATENCIÓN: Las nuevas versiones de Chrome/Edge bloquean el acceso a las cookies.\nPara solucionarlo: Instala la extensión 'Get cookies.txt LOCALLY' en Chrome, exporta tus cookies, y usa el botón 'Cargar Archivo...' abajo.";
            }
            else if (msjError.Contains("JavaScript runtime", StringComparison.OrdinalIgnoreCase))
            {
                msjError = "ATENCIÓN: Falla de protección de YouTube. Falta el motor Deno o Node.js para resolver el desafío de YouTube.";
            }

            throw new YtDlpException(msjError);
        }

        return outputBuilder.ToString();
    }

    private IEnumerable<string> ArgsCookies()
    {
        if (!string.IsNullOrWhiteSpace(RutaArchivoCookies) && File.Exists(RutaArchivoCookies))
            return new[] { "--cookies", RutaArchivoCookies };

        if (!string.IsNullOrWhiteSpace(NavegadorCookies))
            return new[] { "--cookies-from-browser", NavegadorCookies };

        return Array.Empty<string>();
    }

    // El cliente "android_vr" está roto (ago. 2026): sus formatos dan 403.
    // Lo excluimos siempre con el prefijo "-".
    private static readonly string[] ClienteSinCookies = { "default", "-android_vr" };
    private static readonly string[] ClienteConCookies = { "default", "web_embedded", "-android_vr" };

    private IEnumerable<string> ArgsExtractor(bool conCookies) =>
        new[] { "--extractor-args", $"youtube:player_client={string.Join(",", conCookies ? ClienteConCookies : ClienteSinCookies)}" };

    /// <summary>
    /// Intenta la operación con cookies primero (si existen), y si falla
    /// reintenta sin ellas. Mismo patrón que descargar() en Python.
    /// </summary>
    private async Task<string> ConReintentoAsync(Func<bool, IEnumerable<string>> construirArgs, IProgress<double>? progress = null)
    {
        if (!string.IsNullOrWhiteSpace(RutaArchivoCookies) || !string.IsNullOrWhiteSpace(NavegadorCookies))
        {
            try
            {
                return await EjecutarAsync(construirArgs(true), progress);
            }
            catch (YtDlpException)
            {
                // Reintenta sin cookies a continuación
            }
        }
        return await EjecutarAsync(construirArgs(false), progress);
    }

    /// <summary>
    /// Consulta la info del video (sin descargar) y devuelve el título
    /// junto con las resoluciones disponibles, una por altura (la de
    /// mayor bitrate).
    /// </summary>
    public async Task<(string Titulo, List<VideoFormatInfo> Formatos)> ObtenerFormatosAsync(string url)
    {
        string salida = await ConReintentoAsync(conCookies =>
            new[] { "-j", "--no-playlist" }
                .Concat(ArgsExtractor(conCookies))
                .Concat(ArgsCookies())
                .Append(url));

        using var doc = JsonDocument.Parse(salida);
        var root = doc.RootElement;
        string titulo = root.GetProperty("title").GetString() ?? "video";

        var mejoresPorAltura = new Dictionary<int, VideoFormatInfo>();
        if (root.TryGetProperty("formats", out var formatos))
        {
            foreach (var f in formatos.EnumerateArray())
            {
                if (!f.TryGetProperty("height", out var alturaEl) || alturaEl.ValueKind == JsonValueKind.Null)
                    continue;
                if (!f.TryGetProperty("vcodec", out var vcodecEl) ||
                    vcodecEl.GetString() is null or "none")
                    continue;

                int altura = alturaEl.GetInt32();
                double? tbr = f.TryGetProperty("tbr", out var tbrEl) && tbrEl.ValueKind == JsonValueKind.Number
                    ? tbrEl.GetDouble() : null;
                double? fps = f.TryGetProperty("fps", out var fpsEl) && fpsEl.ValueKind == JsonValueKind.Number
                    ? fpsEl.GetDouble() : null;
                string ext = f.TryGetProperty("ext", out var extEl) ? extEl.GetString() ?? "?" : "?";

                if (!mejoresPorAltura.TryGetValue(altura, out var actual) || (tbr ?? 0) > (actual.Tbr ?? 0))
                    mejoresPorAltura[altura] = new VideoFormatInfo(altura, fps, ext, tbr);
            }
        }

        var lista = mejoresPorAltura.Values.OrderByDescending(f => f.Height).ToList();
        return (titulo, lista);
    }

    /// <summary>
    /// Descarga y extrae audio (mp3/flac/wav). "wav" no soporta carátula
    /// embebida (limitación real del contenedor WAV), así que en ese caso
    /// se guarda la imagen aparte, igual que en el script de Python.
    /// </summary>
    public async Task DescargarAudioAsync(string url, string formato, IProgress<double>? progress = null)
    {
        Directory.CreateDirectory(CarpetaMusica);
        string plantilla = Path.Combine(
            CarpetaMusica,
            "%(album,playlist_title|Canciones Sueltas)s",
            "%(playlist_index&{:02d} - |)s%(title)s.%(ext)s");

        await ConReintentoAsync(conCookies =>
        {
            var args = new List<string>
            {
                "--no-playlist",
                "--newline",
                "--no-colors",
                "--retries", "5",
                "--fragment-retries", "5",
                "--format", "bestaudio/best",
                "-x", "--audio-format", formato,
                "--audio-quality", "0",
                "--embed-metadata",
            };
            args.AddRange(ArgsExtractor(conCookies));
            args.AddRange(ArgsCookies());

            if (formato == "wav")
                args.AddRange(new[] { "--write-thumbnail", "--convert-thumbnails", "jpg" });
            else
                args.AddRange(new[] { "--embed-thumbnail", "--no-write-thumbnail" });

            args.AddRange(new[] { "-o", plantilla, url });
            return args;
        }, progress);
    }

    /// <summary>
    /// Descarga un video pidiendo la resolución por ALTURA (no por
    /// format_id exacto), porque el id puede no ser estable entre
    /// consultas si YouTube cambia de cliente entre llamadas.
    /// </summary>
    public async Task DescargarVideoAsync(string url, int altura, IProgress<double>? progress = null)
    {
        Directory.CreateDirectory(CarpetaVideos);
        string plantilla = Path.Combine(CarpetaVideos, "%(title)s.%(ext)s");
        string selector = $"bestvideo[height<={altura}]+bestaudio/best[height<={altura}]/best";

        await ConReintentoAsync(conCookies =>
        {
            var args = new List<string>
            {
                "--no-playlist",
                "--newline",
                "--no-colors",
                "--retries", "5",
                "--fragment-retries", "5",
                "--format", selector,
            };
            args.AddRange(ArgsExtractor(conCookies));
            args.AddRange(ArgsCookies());
            args.AddRange(new[] { "-o", plantilla, url });
            return args;
        }, progress);
    }

    public async Task ActualizarAsync(IProgress<string> progress)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ytdlpPath,
            Arguments = "-U",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        Process procesoOriginal;
        try
        {
            procesoOriginal = Process.Start(psi) ?? throw new Exception();
        }
        catch (Exception)
        {
            throw new YtDlpException("No se encontró yt-dlp.exe para actualizar.");
        }

        using var proceso = procesoOriginal;

        var outputTask = Task.Run(async () =>
        {
            while (!proceso.StandardOutput.EndOfStream)
            {
                var line = await proceso.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                    progress.Report(line);
            }
        });

        await Task.WhenAll(outputTask, proceso.WaitForExitAsync());

        if (proceso.ExitCode != 0)
        {
            string errores = await proceso.StandardError.ReadToEndAsync();
            throw new YtDlpException($"Error al actualizar: {errores}");
        }
    }
}

public class YtDlpException(string message) : Exception(message);
