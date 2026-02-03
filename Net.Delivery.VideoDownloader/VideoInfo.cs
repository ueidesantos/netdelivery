namespace Net.Delivery.VideoDownloader;

/// <summary>
/// Representa informações de um vídeo a ser baixado
/// </summary>
public class VideoInfo
{
    /// <summary>
    /// URL do vídeo
    /// </summary>
    public required string Url { get; init; }
    
    /// <summary>
    /// Nome do arquivo
    /// </summary>
    public required string FileName { get; init; }
    
    /// <summary>
    /// Página de origem
    /// </summary>
    public required string SourcePage { get; init; }
}
