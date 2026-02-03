using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Net.Delivery.VideoDownloader;

/// <summary>
/// Gerenciador de downloads de vídeos com controle de concorrência.
/// 
/// Este módulo demonstra:
/// - Controle de concorrência com SemaphoreSlim
/// - Gerenciamento de fila com System.Threading.Channels
/// - Padrão Producer-Consumer
/// - Operações assíncronas
/// - Tratamento de erros e timeouts
/// 
/// IMPORTANTE: Use apenas com conteúdo público ou de sua propriedade.
/// Não implemente bypass de DRM, quebra de autenticação ou scraping agressivo.
/// </summary>
public class VideoDownloader : IDisposable
{
    // Constantes de configuração
    private const int MaxConcurrentDownloads = 5;
    private const int ConnectTimeoutSeconds = 30;
    private const int ReadTimeoutSeconds = 300; // 5 minutos
    private const int MaxRetries = 3;
    private const int RetryDelaySeconds = 5;
    private const int ChunkSize = 8192; // 8KB

    private readonly ILogger<VideoDownloader> _logger;
    private readonly string _outputDirectory;
    private readonly int _maxWorkers;
    private readonly SemaphoreSlim _semaphore;
    private readonly HttpClient _httpClient;
    private readonly Channel<VideoInfo?> _downloadChannel;
    
    // Estatísticas
    private int _totalVideos;
    private int _completedDownloads;
    private int _failedDownloads;
    private int _queuedVideos; // Contador para monitorar tamanho da fila

    public VideoDownloader(
        ILogger<VideoDownloader> logger,
        string outputDirectory = "downloads",
        int maxWorkers = MaxConcurrentDownloads)
    {
        _logger = logger;
        _outputDirectory = outputDirectory;
        _maxWorkers = Math.Min(maxWorkers, MaxConcurrentDownloads);
        
        // Semáforo controla o número máximo de downloads simultâneos
        _semaphore = new SemaphoreSlim(_maxWorkers, _maxWorkers);
        
        // HttpClient configurado com timeouts apropriados
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(ReadTimeoutSeconds)
        };
        
        // Channel para comunicação entre produtor (páginas) e consumidores (workers)
        // UnboundedChannel permite fila ilimitada (pode ser configurado com limite se necessário)
        _downloadChannel = Channel.CreateUnbounded<VideoInfo?>(
            new UnboundedChannelOptions
            {
                SingleReader = false, // Múltiplos workers leem
                SingleWriter = false  // Múltiplas páginas podem adicionar
            });
        
        // Criar diretório de saída se não existir
        Directory.CreateDirectory(_outputDirectory);
        
        _logger.LogInformation("VideoDownloader inicializado");
        _logger.LogInformation("Máximo de downloads simultâneos: {MaxWorkers}", _maxWorkers);
        _logger.LogInformation("Diretório de saída: {OutputDirectory}", _outputDirectory);
    }

    /// <summary>
    /// Busca o conteúdo de uma página
    /// </summary>
    private async Task<string?> FetchPageAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(ConnectTimeoutSeconds));
            
            var response = await _httpClient.GetAsync(url, cts.Token);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync(cts.Token);
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Timeout ao buscar página: {Url}", url);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao buscar página: {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Extrai links de vídeos de uma página.
    /// 
    /// PLACEHOLDER: Esta é uma implementação simplificada.
    /// Em produção, use HtmlAgilityPack ou AngleSharp para parsing HTML robusto.
    /// </summary>
    private List<VideoInfo> ExtractVideoLinks(string pageUrl, string pageContent)
    {
        var videos = new List<VideoInfo>();
        
        // Exemplo simplificado: busca por links diretos de vídeos
        // Em produção, use parsing HTML apropriado
        var videoExtensions = new[] { ".mp4", ".webm", ".avi", ".mkv" };
        
        foreach (var extension in videoExtensions)
        {
            var pattern = $@"href=""([^""]*{extension}[^""]*)""";
            var matches = System.Text.RegularExpressions.Regex.Matches(pageContent, pattern);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var videoUrl = match.Groups[1].Value;
                    
                    // Converter URL relativa para absoluta
                    if (!Uri.IsWellFormedUriString(videoUrl, UriKind.Absolute))
                    {
                        var baseUri = new Uri(pageUrl);
                        videoUrl = new Uri(baseUri, videoUrl).ToString();
                    }
                    
                    var fileName = Path.GetFileName(new Uri(videoUrl).LocalPath);
                    
                    videos.Add(new VideoInfo
                    {
                        Url = videoUrl,
                        FileName = fileName,
                        SourcePage = pageUrl
                    });
                }
            }
        }
        
        return videos;
    }

    /// <summary>
    /// Processa uma página: busca e extrai links de vídeos
    /// </summary>
    private async Task<int> ProcessPageAsync(string pageUrl, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processando página: {PageUrl}", pageUrl);
        
        var content = await FetchPageAsync(pageUrl, cancellationToken);
        if (content == null)
        {
            _logger.LogWarning("Falha ao buscar página: {PageUrl}", pageUrl);
            return 0;
        }
        
        var videos = ExtractVideoLinks(pageUrl, content);
        
        // Adicionar vídeos ao canal (fila)
        foreach (var video in videos)
        {
            await _downloadChannel.Writer.WriteAsync(video, cancellationToken);
            
            Interlocked.Increment(ref _totalVideos);
            Interlocked.Increment(ref _queuedVideos);
            
            // Monitoramento: Exibir tamanho da fila
            _logger.LogInformation("📥 Vídeo adicionado à fila: {FileName}", video.FileName);
            _logger.LogInformation("📥 Tamanho atual da fila: {QueueSize}", _queuedVideos);
        }
        
        return videos.Count;
    }

    /// <summary>
    /// Faz o download de um vídeo com retry logic
    /// </summary>
    private async Task<bool> DownloadVideoAsync(VideoInfo video, CancellationToken cancellationToken)
    {
        var outputPath = Path.Combine(_outputDirectory, video.FileName);
        
        // Semáforo controla a concorrência - apenas N downloads simultâneos
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            _logger.LogInformation("Iniciando download: {FileName}", video.FileName);
            
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var response = await _httpClient.GetAsync(
                        video.Url,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);
                    
                    response.EnsureSuccessStatusCode();
                    
                    var totalSize = response.Content.Headers.ContentLength ?? 0;
                    long downloadedSize = 0;
                    long lastLoggedSize = 0; // Para rastrear quando logar progresso
                    const long logIntervalBytes = 1024 * 1024; // Logar a cada 1MB
                    
                    await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await using var fileStream = new FileStream(
                        outputPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        ChunkSize,
                        useAsync: true);
                    
                    var buffer = new byte[ChunkSize];
                    int bytesRead;
                    
                    while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        downloadedSize += bytesRead;
                        
                        // Log de progresso para arquivos grandes (a cada 1MB de progresso)
                        if (totalSize > 0 && (downloadedSize - lastLoggedSize) >= logIntervalBytes)
                        {
                            var progress = (downloadedSize / (double)totalSize) * 100;
                            _logger.LogDebug("Progresso {FileName}: {Progress:F1}%", video.FileName, progress);
                            lastLoggedSize = downloadedSize;
                        }
                    }
                    
                    _logger.LogInformation("✅ Download concluído: {FileName} ({Size} bytes)", 
                        video.FileName, downloadedSize);
                    
                    Interlocked.Increment(ref _completedDownloads);
                    Interlocked.Decrement(ref _queuedVideos);
                    
                    // Monitoramento: Exibir total de downloads concluídos
                    _logger.LogInformation("✅ Total de downloads concluídos: {Completed}", _completedDownloads);
                    
                    return true;
                }
                catch (TaskCanceledException) when (attempt < MaxRetries)
                {
                    _logger.LogWarning("Timeout no download {FileName} (tentativa {Attempt}/{MaxRetries})",
                        video.FileName, attempt, MaxRetries);
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), cancellationToken);
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    _logger.LogError(ex, "Erro no download {FileName} (tentativa {Attempt}/{MaxRetries})",
                        video.FileName, attempt, MaxRetries);
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro inesperado no download: {FileName}", video.FileName);
                    break;
                }
            }
            
            // Todas as tentativas falharam
            _logger.LogError("❌ Falha ao baixar: {FileName}", video.FileName);
            Interlocked.Increment(ref _failedDownloads);
            Interlocked.Decrement(ref _queuedVideos);
            return false;
        }
        finally
        {
            // Liberar o semáforo para permitir outro download
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Worker que processa downloads do canal
    /// </summary>
    private async Task WorkerAsync(int workerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker {WorkerId} iniciado", workerId);
        
        try
        {
            // Ler do canal até que seja fechado
            await foreach (var video in _downloadChannel.Reader.ReadAllAsync(cancellationToken))
            {
                // null é usado como sinal para parar o worker
                if (video == null)
                {
                    break;
                }
                
                await DownloadVideoAsync(video, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker {WorkerId} cancelado", workerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no Worker {WorkerId}", workerId);
        }
        
        _logger.LogInformation("Worker {WorkerId} finalizado", workerId);
    }

    /// <summary>
    /// Processa múltiplas páginas sequencialmente
    /// </summary>
    private async Task ProcessPagesAsync(string baseUrl, int pageCount, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando processamento de {PageCount} páginas", pageCount);
        
        for (int pageNum = 1; pageNum <= pageCount; pageNum++)
        {
            // Gerar URL da página (customizar baseado no formato de paginação)
            var pageUrl = baseUrl.Contains("{page}")
                ? baseUrl.Replace("{page}", pageNum.ToString())
                : $"{baseUrl}?page={pageNum}";
            
            var videosFound = await ProcessPageAsync(pageUrl, cancellationToken);
            _logger.LogInformation("Página {PageNum}/{PageCount}: {VideosFound} vídeos encontrados",
                pageNum, pageCount, videosFound);
            
            // Pequeno delay entre páginas para evitar sobrecarga no servidor
            if (pageNum < pageCount)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        
        // Sinalizar que não haverá mais vídeos
        _downloadChannel.Writer.Complete();
    }

    /// <summary>
    /// Executa o downloader
    /// </summary>
    public async Task RunAsync(string baseUrl, int pageCount, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("============================================================");
        _logger.LogInformation("Video Downloader Iniciado");
        _logger.LogInformation("Máximo de downloads simultâneos: {MaxWorkers}", _maxWorkers);
        _logger.LogInformation("Diretório de saída: {OutputDirectory}", _outputDirectory);
        _logger.LogInformation("============================================================");
        
        var startTime = DateTime.UtcNow;
        
        // Iniciar workers
        var workers = Enumerable.Range(0, _maxWorkers)
            .Select(i => WorkerAsync(i, cancellationToken))
            .ToArray();
        
        // Processar páginas (produtor)
        await ProcessPagesAsync(baseUrl, pageCount, cancellationToken);
        
        // Aguardar todos os workers completarem
        _logger.LogInformation("Aguardando conclusão de todos os downloads...");
        await Task.WhenAll(workers);
        
        var elapsed = DateTime.UtcNow - startTime;
        
        // Sumário
        _logger.LogInformation("============================================================");
        _logger.LogInformation("Sessão de Download Concluída");
        _logger.LogInformation("Total de vídeos processados: {Total}", _totalVideos);
        _logger.LogInformation("Downloads concluídos com sucesso: {Completed}", _completedDownloads);
        _logger.LogInformation("Downloads falhados: {Failed}", _failedDownloads);
        _logger.LogInformation("Tempo decorrido: {Elapsed:hh\\:mm\\:ss}", elapsed);
        _logger.LogInformation("============================================================");
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
        _httpClient?.Dispose();
    }
}
