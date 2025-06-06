namespace Application.Interfaces;

public interface IEmbeddingService
{
    IAsyncEnumerable<(string Chunk, float[] Embedding)> GenerateEmbeddingsAsync(IAsyncEnumerable<string> chunks);
} 