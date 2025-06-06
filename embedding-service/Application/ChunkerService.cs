using Application.Interfaces;

namespace Application;

public class ChunkerService : IChunkerService
{
    private const int ChunkSize = 1000;
    private const int Overlap = 200;

    public async IAsyncEnumerable<string> ChunkPdfsAsync(IAsyncEnumerable<string> pdfs)
    {
        await foreach (var pdfText in pdfs)
        {
            if (string.IsNullOrWhiteSpace(pdfText)) continue;
            int start = 0;
            while (start < pdfText.Length)
            {
                int length = Math.Min(ChunkSize, pdfText.Length - start);
                var chunk = pdfText.Substring(start, length);
                yield return chunk;
                if (start + length >= pdfText.Length) break;
                start += ChunkSize - Overlap;
            }
        }
    }
} 