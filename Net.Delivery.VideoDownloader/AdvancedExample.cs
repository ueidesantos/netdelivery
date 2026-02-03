using Microsoft.Extensions.Logging;
using Net.Delivery.VideoDownloader;

/*
 * Exemplo Avançado: Video Downloader com Customização
 * 
 * Este exemplo mostra como customizar o downloader para necessidades específicas:
 * - Parsing HTML personalizado
 * - Headers de autenticação
 * - Configuração de workers
 * 
 * Para executar este exemplo, comente o código em Program.cs e descomente o Main abaixo.
 */

namespace Net.Delivery.VideoDownloader.Examples;

public class AdvancedExample
{
    // Para usar este exemplo, remova o comentário do método Main abaixo
    // e comente o código em Program.cs
    /*
    public static async Task Main(string[] args)
    {
        // Configurar logging com níveis customizados
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug) // Debug para ver mais detalhes
                .AddConsole();
        });

        var logger = loggerFactory.CreateLogger<VideoDownloader>();

        // Exemplo 1: Usar com servidor local de teste
        await RunLocalTestExample(logger);

        // Exemplo 2: Configurações diferentes
        // await RunWithDifferentSettings(logger);
    }
    */

    public static async Task RunLocalTestExample(ILogger<VideoDownloader> logger)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         Exemplo: Servidor Local de Teste                  ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Certifique-se de que o servidor local está rodando:");
        Console.WriteLine("  cd test_videos && python3 -m http.server 8000");
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para continuar ou Ctrl+C para cancelar...");
        Console.ReadLine();

        const string baseUrl = "http://localhost:8000/page{page}.html";
        const int pageCount = 3;
        const int maxWorkers = 3; // Usar apenas 3 workers para teste local

        using var downloader = new VideoDownloader(
            logger,
            outputDirectory: "test_downloads",
            maxWorkers: maxWorkers);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n❌ Cancelamento solicitado...");
            cts.Cancel();
        };

        try
        {
            await downloader.RunAsync(baseUrl, pageCount, cts.Token);
            Console.WriteLine("\n✅ Teste concluído com sucesso!");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("⚠️ Operação cancelada pelo usuário.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Erro durante o teste");
        }
    }

    public static async Task RunWithDifferentSettings(ILogger<VideoDownloader> logger)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         Exemplo: Configurações Customizadas               ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

        // Exemplo com apenas 2 workers para conexões mais lentas
        const string baseUrl = "https://example.com/videos?page={page}";
        const int pageCount = 5;
        const int maxWorkers = 2; // Apenas 2 downloads simultâneos

        using var downloader = new VideoDownloader(
            logger,
            outputDirectory: "custom_downloads",
            maxWorkers: maxWorkers);

        using var cts = new CancellationTokenSource();
        
        // Timeout de 10 minutos para a operação completa
        cts.CancelAfter(TimeSpan.FromMinutes(10));

        await downloader.RunAsync(baseUrl, pageCount, cts.Token);
    }
}

/*
 * Exemplo de Customização: ExtractVideoLinks
 * 
 * Para usar parsing HTML robusto, instale HtmlAgilityPack:
 *   dotnet add package HtmlAgilityPack
 * 
 * Então customize o método ExtractVideoLinks no VideoDownloader.cs:
 * 
 * using HtmlAgilityPack;
 * 
 * private List<VideoInfo> ExtractVideoLinks(string pageUrl, string pageContent)
 * {
 *     var videos = new List<VideoInfo>();
 *     var doc = new HtmlDocument();
 *     doc.LoadHtml(pageContent);
 *     
 *     // Exemplo: Extrair de elementos <div class="video-item">
 *     var videoNodes = doc.DocumentNode.SelectNodes("//div[@class='video-item']//a");
 *     
 *     if (videoNodes != null)
 *     {
 *         foreach (var node in videoNodes)
 *         {
 *             var href = node.GetAttributeValue("href", "");
 *             var title = node.GetAttributeValue("title", "video");
 *             
 *             if (!string.IsNullOrEmpty(href) && href.EndsWith(".mp4"))
 *             {
 *                 var videoUrl = new Uri(new Uri(pageUrl), href).ToString();
 *                 var fileName = !string.IsNullOrEmpty(title) 
 *                     ? $"{title}.mp4" 
 *                     : Path.GetFileName(new Uri(videoUrl).LocalPath);
 *                 
 *                 videos.Add(new VideoInfo
 *                 {
 *                     Url = videoUrl,
 *                     FileName = SanitizeFileName(fileName),
 *                     SourcePage = pageUrl
 *                 });
 *             }
 *         }
 *     }
 *     
 *     return videos;
 * }
 * 
 * private string SanitizeFileName(string fileName)
 * {
 *     var invalidChars = Path.GetInvalidFileNameChars();
 *     return string.Join("_", fileName.Split(invalidChars, 
 *         StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
 * }
 */

/*
 * Exemplo de Adição de Autenticação:
 * 
 * No construtor do VideoDownloader, configure headers:
 * 
 * _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_TOKEN");
 * _httpClient.DefaultRequestHeaders.Add("User-Agent", "VideoDownloader/1.0");
 * 
 * Ou para autenticação básica:
 * 
 * var credentials = Convert.ToBase64String(
 *     Encoding.ASCII.GetBytes($"{username}:{password}"));
 * _httpClient.DefaultRequestHeaders.Authorization = 
 *     new AuthenticationHeaderValue("Basic", credentials);
 */
