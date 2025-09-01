using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using ProcessaTxtAsync; // importa a classe FileProcessor

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("== Contagem Assíncrona de Linhas e Palavras ==");
Console.Write("Informe o diretório com arquivos .txt: ");
var dir = Console.ReadLine()?.Trim('"', ' ') ?? string.Empty;

if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
{
    Console.WriteLine("Diretório inválido ou inexistente.");
    return;
}

// 1) Descobrir os .txt e listar na tela
var files = Directory.GetFiles(dir, "*.txt", SearchOption.TopDirectoryOnly);

if (files.Length == 0)
{
    Console.WriteLine("Nenhum .txt encontrado neste diretório.");
    return;
}

Console.WriteLine($"\nArquivos encontrados ({files.Length}):");
foreach (var f in files) Console.WriteLine($"- {Path.GetFileName(f)}");

Console.Write("\nPressione ENTER para iniciar o processamento...");
Console.ReadLine();

// 2) Processar em paralelo (async/await)
var results = new ConcurrentBag<(string FileName, int Lines, int Words)>();
var errors = new ConcurrentBag<(string FileName, string Error)>();

// Limite de concorrência
var maxDegree = Math.Max(2, Environment.ProcessorCount);
using var gate = new SemaphoreSlim(maxDegree);

var tasks = files.Select(async path =>
{
    await gate.WaitAsync();
    try
    {
        Console.WriteLine($"Processando arquivo {Path.GetFileName(path)}...");
        var (name, lines, words) = await FileProcessor.ProcessFileAsync(path);
        results.Add((name, lines, words));
    }
    catch (Exception ex)
    {
        errors.Add((Path.GetFileName(path), ex.Message));
    }
    finally
    {
        gate.Release();
    }
});

await Task.WhenAll(tasks);

// 3) Gravar relatório
var exportDir = Path.Combine(AppContext.BaseDirectory, "export");
Directory.CreateDirectory(exportDir);
var reportPath = Path.Combine(exportDir, "relatorio.txt");

var linesOut = results
    .OrderBy(r => r.FileName, StringComparer.OrdinalIgnoreCase)
    .Select(r => $"{r.FileName} - {r.Lines} linhas - {r.Words} palavras");

await File.WriteAllLinesAsync(reportPath, linesOut);

Console.WriteLine($"\nRelatório gerado em: {reportPath}");

if (!errors.IsEmpty)
{
    Console.WriteLine("\nArquivos que falharam:");
    foreach (var e in errors) Console.WriteLine($"- {e.FileName}: {e.Error}");
}

Console.WriteLine("\nConcluído!");
