#!/bin/bash

# Script de teste local para o Video Downloader
# Cria um ambiente de teste com servidor HTTP local

echo "============================================================"
echo "  Video Downloader - Ambiente de Teste Local"
echo "============================================================"
echo ""

# Criar diretório de teste
TEST_DIR="test_videos"
mkdir -p "$TEST_DIR"

echo "Criando arquivos de vídeo de teste..."

# Criar alguns arquivos de teste simulando vídeos
for i in {1..5}; do
    # Criar arquivos de 1MB cada
    dd if=/dev/zero of="$TEST_DIR/video$i.mp4" bs=1M count=1 2>/dev/null
    echo "✅ Criado: video$i.mp4 (1MB)"
done

# Criar uma página HTML simples com links para os vídeos
cat > "$TEST_DIR/index.html" <<'EOF'
<!DOCTYPE html>
<html>
<head>
    <title>Test Videos - Página 1</title>
</head>
<body>
    <h1>Vídeos de Teste - Página 1</h1>
    <ul>
        <li><a href="video1.mp4">Video 1</a></li>
        <li><a href="video2.mp4">Video 2</a></li>
    </ul>
</body>
</html>
EOF

# Criar página 2
cat > "$TEST_DIR/page2.html" <<'EOF'
<!DOCTYPE html>
<html>
<head>
    <title>Test Videos - Página 2</title>
</head>
<body>
    <h1>Vídeos de Teste - Página 2</h1>
    <ul>
        <li><a href="video3.mp4">Video 3</a></li>
        <li><a href="video4.mp4">Video 4</a></li>
    </ul>
</body>
</html>
EOF

# Criar página 3
cat > "$TEST_DIR/page3.html" <<'EOF'
<!DOCTYPE html>
<html>
<head>
    <title>Test Videos - Página 3</title>
</head>
<body>
    <h1>Vídeos de Teste - Página 3</h1>
    <ul>
        <li><a href="video5.mp4">Video 5</a></li>
    </ul>
</body>
</html>
EOF

echo ""
echo "✅ Ambiente de teste criado!"
echo ""
echo "Para testar o Video Downloader:"
echo ""
echo "1. Inicie o servidor HTTP local em outro terminal:"
echo "   cd $TEST_DIR && python3 -m http.server 8000"
echo ""
echo "2. Edite Program.cs e configure:"
echo "   const string baseUrl = \"http://localhost:8000/page{page}.html\";"
echo "   const int pageCount = 3;"
echo ""
echo "3. Execute o downloader:"
echo "   cd Net.Delivery.VideoDownloader"
echo "   dotnet run"
echo ""
echo "4. Os vídeos serão baixados para: Net.Delivery.VideoDownloader/downloads/"
echo ""
echo "============================================================"
