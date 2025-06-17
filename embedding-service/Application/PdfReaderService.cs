using Application.Interfaces;
using embedding_service.Configuration;
using UglyToad.PdfPig;

namespace Application;

public class PdfReaderService : IPdfReaderService
{
    private readonly DataFolderOptions _options;
    public PdfReaderService(DataFolderOptions options)
    {
        _options = options;
    }

    public async IAsyncEnumerable<string> ReadAllPdfsAsync()
    {
        var pdfFiles = Directory.GetFiles(_options.FolderPath, "*.pdf");
        foreach (var file in pdfFiles)
        {
            using var pdf = PdfDocument.Open(file);
            var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
            yield return text;
            await Task.Yield();
        }
    }
} 