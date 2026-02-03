# Net.Delivery.VideoDownloader

## 📚 Módulo Educacional/Técnico em .NET 10

Este projeto demonstra padrões avançados de concorrência em .NET para download de conteúdo de fontes publicamente acessíveis.

### ⚠️ Aviso Legal e Conformidade

**IMPORTANTE**: Este módulo é projetado **exclusivamente para fins educacionais** e deve ser usado APENAS com:

- ✅ Conteúdo que você criou ou possui
- ✅ Conteúdo público com permissão explícita para download
- ✅ Ambientes de teste e desenvolvimento
- ✅ APIs internas e serviços autorizados

**NÃO use este módulo para:**

- ❌ Bypass de DRM ou sistemas de proteção de cópia
- ❌ Quebra de autenticação ou autorização
- ❌ Violação de termos de serviço ou paywalls
- ❌ Web scraping agressivo
- ❌ Engenharia reversa de players proprietários
- ❌ Download de material protegido por direitos autorais sem permissão

### 🎯 Foco Técnico

Este módulo demonstra os seguintes conceitos de programação concorrente em .NET:

1. **Controle de Concorrência com SemaphoreSlim**
   - Limita downloads simultâneos (máximo 5)
   - Previne sobrecarga de recursos

2. **Gerenciamento de Fila com System.Threading.Channels**
   - Comunicação eficiente entre produtor e consumidores
   - Padrão Producer-Consumer pattern

3. **Programação Assíncrona (async/await)**
   - Operações I/O não bloqueantes
   - Melhor utilização de recursos do sistema

4. **Worker Pool Pattern**
   - Múltiplos workers processam tarefas concorrentemente
   - Distribuição automática de carga

5. **Tratamento de Erros e Resiliência**
   - Retry logic com backoff
   - Timeouts configuráveis
   - Logging estruturado

6. **Monitoramento em Tempo Real**
   - Tamanho da fila de downloads
   - Contadores de conclusão
   - Estatísticas de sessão

### 🚀 Pré-requisitos

- .NET 10.0 SDK ou superior
- Sistema Operacional: Windows, Linux, ou macOS

### 📦 Instalação

1. Clone o repositório:
```bash
git clone https://github.com/ueidesantos/netdelivery.git
cd netdelivery/Net.Delivery.VideoDownloader
```

2. Restaurar dependências:
```bash
dotnet restore
```

3. Compilar o projeto:
```bash
dotnet build
```

### 📖 Uso

#### Execução Básica

```bash
dotnet run
```

#### Personalização

Edite o arquivo `Program.cs` para configurar:

```csharp
// URL base (use {page} como placeholder para número da página)
const string baseUrl = "https://seu-servidor.com/videos?page={page}";

// Número de páginas para processar
const int pageCount = 3;

// Máximo de downloads simultâneos (até 5)
const int maxWorkers = 5;
```

#### Customizando a Extração de Links

O método `ExtractVideoLinks` em `VideoDownloader.cs` é um placeholder. Para uso em produção, implemente parsing HTML robusto:

```csharp
// Instale: dotnet add package HtmlAgilityPack
using HtmlAgilityPack;

private List<VideoInfo> ExtractVideoLinks(string pageUrl, string pageContent)
{
    var videos = new List<VideoInfo>();
    var doc = new HtmlDocument();
    doc.LoadHtml(pageContent);
    
    // Exemplo: extrair de elementos específicos
    var videoNodes = doc.DocumentNode.SelectNodes("//a[@class='video-link']");
    
    if (videoNodes != null)
    {
        foreach (var node in videoNodes)
        {
            var href = node.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(href))
            {
                var videoUrl = new Uri(new Uri(pageUrl), href).ToString();
                videos.Add(new VideoInfo
                {
                    Url = videoUrl,
                    FileName = Path.GetFileName(new Uri(videoUrl).LocalPath),
                    SourcePage = pageUrl
                });
            }
        }
    }
    
    return videos;
}
```

### 🏗️ Arquitetura

#### Componentes Principais

1. **VideoDownloader**: Classe principal que orquestra todo o processo
   - Gerencia ciclo de vida do HttpClient
   - Controla concorrência com SemaphoreSlim
   - Coordena workers e processamento de páginas

2. **System.Threading.Channels**: Fila de comunicação
   - Channel<VideoInfo> para passar vídeos entre produtor e consumidores
   - Unbounded para permitir fila ilimitada (configurável)

3. **Workers**: Consumidores que processam downloads
   - Múltiplas tarefas executando concorrentemente
   - Controladas por semáforo para limitar concorrência real

#### Fluxo de Dados

```
Processamento de Páginas     Canal de Downloads          Workers (Concorrente)
     (Sequencial)                                      
          │                          │                        │
          ├─> Página 1 ──────────────┤                        │
          │   Extrai vídeos          ├──> Video 1 ──> Worker 0 ──> Download
          │                          │                        │
          ├─> Página 2 ──────────────┤                        │
          │   Extrai vídeos          ├──> Video 2 ──> Worker 1 ──> Download
          │                          │                        │
          ├─> Página 3 ──────────────┤                        │
          │   Extrai vídeos          ├──> Video 3 ──> Worker 2 ──> Download
          │                          │                        │
          └────────────────────────────┤──> Video 4 ──> Worker 3 ──> Download
                                      │                        │
                                      └──> Video 5 ──> Worker 4 ──> Download
                                      
              (Máximo 5 downloads simultâneos via SemaphoreSlim)
```

### 📊 Monitoramento

A aplicação fornece logs em tempo real:

```
📥 Vídeo adicionado à fila: video1.mp4
📥 Tamanho atual da fila: 1
📥 Vídeo adicionado à fila: video2.mp4
📥 Tamanho atual da fila: 2
...
Worker 0 iniciado
Worker 1 iniciado
...
Iniciando download: video1.mp4
✅ Download concluído: video1.mp4 (15234567 bytes)
✅ Total de downloads concluídos: 1
...
```

### 🛠️ Configuração Avançada

Parâmetros configuráveis em `VideoDownloader.cs`:

```csharp
private const int MaxConcurrentDownloads = 5;      // Máximo absoluto
private const int ConnectTimeoutSeconds = 30;       // Timeout de conexão
private const int ReadTimeoutSeconds = 300;         // Timeout de leitura (5 min)
private const int MaxRetries = 3;                   // Tentativas de retry
private const int RetryDelaySeconds = 5;            // Delay entre retries
private const int ChunkSize = 8192;                 // Tamanho do chunk (8KB)
```

### 🧪 Testando Localmente

Para testar sem acesso a internet, crie um servidor HTTP local:

**Windows (PowerShell):**
```powershell
# Criar arquivos de teste
mkdir test_videos
fsutil file createnew test_videos\video1.mp4 10485760  # 10MB
fsutil file createnew test_videos\video2.mp4 10485760

# Executar servidor local (requer Python)
python -m http.server 8000 --directory test_videos
```

**Linux/macOS:**
```bash
# Criar arquivos de teste
mkdir -p test_videos
dd if=/dev/zero of=test_videos/video1.mp4 bs=1M count=10
dd if=/dev/zero of=test_videos/video2.mp4 bs=1M count=10

# Executar servidor local
python3 -m http.server 8000 --directory test_videos
```

Então configure `baseUrl = "http://localhost:8000"` no Program.cs.

### 📝 Estrutura do Projeto

```
Net.Delivery.VideoDownloader/
├── Net.Delivery.VideoDownloader.csproj  # Configuração do projeto
├── Program.cs                            # Ponto de entrada da aplicação
├── VideoDownloader.cs                    # Lógica principal de download
├── VideoInfo.cs                          # Modelo de dados
└── README.md                             # Esta documentação
```

### 🔒 Considerações de Segurança

1. **Nunca hardcode credenciais** - Use variáveis de ambiente ou Azure Key Vault
2. **Valide e sanitize URLs** - Previna injeção e acesso não autorizado
3. **Respeite robots.txt** - Sempre verifique as regras de scraping
4. **Use HTTPS** - Sempre que possível para conexões seguras
5. **Implemente rate limiting** - Incluso delays entre requisições de páginas
6. **Auditoria e logs** - Mantenha logs para compliance e debugging

### 📚 Recursos de Aprendizado

Para entender os padrões usados neste módulo:

- [Async programming in .NET](https://docs.microsoft.com/dotnet/csharp/async)
- [System.Threading.Channels](https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/)
- [SemaphoreSlim Class](https://docs.microsoft.com/dotnet/api/system.threading.semaphoreslim)
- [HttpClient Best Practices](https://docs.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines)
- [Producer-Consumer Pattern](https://docs.microsoft.com/dotnet/standard/collections/thread-safe/blockingcollection-overview)

### 🤝 Contribuindo

Contribuições que melhorem:
- Padrões de concorrência
- Tratamento de erros
- Documentação
- Testes unitários e de integração

são bem-vindas.

### 📄 Licença e Disclaimer

Este código é fornecido para fins educacionais. Os usuários são responsáveis por garantir que seu uso esteja em conformidade com todas as leis aplicáveis, termos de serviço e diretrizes éticas.

### 💡 Exemplos de Uso Legítimo

1. **Download de Conteúdo Próprio**
   ```csharp
   // Baixar vídeos do seu próprio servidor
   const string baseUrl = "https://meu-servidor.com/meus-videos?page={page}";
   ```

2. **Ambiente de Teste**
   ```csharp
   // Usar em ambiente de desenvolvimento
   const string baseUrl = "http://localhost:8000/test-videos";
   ```

3. **API Interna Corporativa**
   ```csharp
   // Acessar API interna da empresa (com autorização)
   const string baseUrl = "https://internal-api.empresa.com/videos?page={page}";
   ```

### ❓ FAQ

**P: Posso aumentar o número de downloads simultâneos acima de 5?**
R: Tecnicamente sim, mas não é recomendado. Mais downloads simultâneos podem:
- Sobrecarregar o servidor de origem
- Consumir muita banda e memória
- Ser interpretado como ataque (DDoS)

**P: Como adiciono autenticação?**
R: Configure o HttpClient com headers apropriados:
```csharp
_httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", "seu-token");
```

**P: Funciona com streaming de vídeo (HLS, DASH)?**
R: Não diretamente. Esta implementação é para downloads diretos HTTP. Para streaming, você precisaria de bibliotecas específicas como ffmpeg.

**P: Como fazer logging em arquivo?**
R: Adicione provider de arquivo ao LoggerFactory:
```csharp
using Microsoft.Extensions.Logging.Console;

builder.AddConsole();
builder.AddFile("logs/app-{Date}.log");  // Requer Serilog.Extensions.Logging.File
```

### 🔄 Roadmap

Melhorias futuras planejadas:
- [ ] Suporte a autenticação OAuth2
- [ ] Persistência de estado (retomar downloads)
- [ ] Interface de linha de comando (CLI) robusta
- [ ] Suporte a proxy e rate limiting configurável
- [ ] Testes unitários e de integração
- [ ] Docker containerização
