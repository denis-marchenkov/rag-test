namespace Application.Interfaces;

public interface IChromaDbClient
{
    Task StoreEmbeddingsAsync(IAsyncEnumerable<(string Chunk, float[] Embedding)> embeddings);
    Task<List<(string Chunk, float[] Embedding, Dictionary<string, object> Meta, float Distance)>> QueryEmbeddingsAsync(float[] embedding, int nResults = 5);
} 