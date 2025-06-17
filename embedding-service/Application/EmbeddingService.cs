using Application.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;

namespace Application;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _pythonServiceUrl;

    public EmbeddingService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();

        _pythonServiceUrl = Environment.GetEnvironmentVariable("PYTHON_EMBEDDING_URL") ?? "http://localhost:5001/embed";
    }

    public async IAsyncEnumerable<(string, float[])> GenerateEmbeddingsAsync(IAsyncEnumerable<string> chunks)
    {
        var chunkList = new List<string>();
        await foreach (var chunk in chunks)
        {
            chunkList.Add(chunk);
            // Optionally, batch for efficiency
            if (chunkList.Count >= 8)
            {
                foreach (var result in await EmbedBatch(chunkList))
                    yield return result;
                chunkList.Clear();
            }
        }
        if (chunkList.Count > 0)
        {
            foreach (var result in await EmbedBatch(chunkList))
                yield return result;
        }
    }

    private async Task<List<(string, float[])>> EmbedBatch(List<string> chunkBatch)
    {
        var request = new { texts = chunkBatch };
        var response = await _httpClient.PostAsJsonAsync(_pythonServiceUrl, request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        if (result == null || result.embeddings == null)
            throw new Exception("Failed to get embeddings from Python service");
        var embeddings = result.embeddings;
        var output = new List<(string, float[])>();
        for (int i = 0; i < chunkBatch.Count; i++)
        {
            output.Add((chunkBatch[i], embeddings[i]));
        }
        return output;
    }

    private class EmbeddingResponse
    {
        public List<float[]> embeddings { get; set; } = new();
    }
} 