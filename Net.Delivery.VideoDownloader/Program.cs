using Microsoft.Extensions.Logging;
using Net.Delivery.VideoDownloader;

/*
 * Video Downloader - Módulo Educacional para Estudo de Concorrência
 * 
 * IMPORTANTE: Este programa é para fins educacionais e técnicos.
 * Use apenas com:
 * - Conteúdo que você possui
 * - Conteúdo público com permissão explícita para download
 * - Ambientes de teste
 * - APIs internas autorizadas
 * 
 * NÃO use para:
 * - Bypass de DRM ou proteções de cópia
 * - Quebra de autenticação
 * - Violação de termos de serviço
 * - Scraping agressivo
 * - Download de conteúdo protegido por direitos autorais sem permissão
 */

// Configurar logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Information)
        .AddConsole();
});

var logger = loggerFactory.CreateLogger<VideoDownloader>();

// IMPORTANTE: Substitua pela URL real que você tem permissão para acessar
// A URL deve apontar para conteúdo público ou de sua propriedade
const string baseUrl = "https://example.com/videos?page={page}";
const int pageCount = 3; // Número de páginas para processar
const int maxWorkers = 5; // Máximo de downloads simultâneos

Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║    Video Downloader - Estudo de Concorrência Controlada   ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("AVISO: Use apenas com conteúdo público ou autorizado!");
Console.WriteLine();

try
{
    using var downloader = new VideoDownloader(
        logger,
        outputDirectory: "downloads",
        maxWorkers: maxWorkers);

    using var cts = new CancellationTokenSource();
    
    // Capturar Ctrl+C para cancelamento gracioso
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        Console.WriteLine("\nCancelamento solicitado...");
        cts.Cancel();
    };

    await downloader.RunAsync(baseUrl, pageCount, cts.Token);
    
    Console.WriteLine();
    Console.WriteLine("Pressione qualquer tecla para sair...");
    Console.ReadKey();
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operação cancelada pelo usuário.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Erro fatal na aplicação");
    return 1;
}

return 0;
