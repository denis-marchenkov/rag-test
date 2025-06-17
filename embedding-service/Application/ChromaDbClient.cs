using Application.Interfaces;
using embedding_service.Application.DTO;
using embedding_service.Configuration;
using embedding_service.DTO.ChromaRequest;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Application;

public class ChromaDbClient : IChromaDbClient
{
    private readonly HttpClient _httpClient;
    private const int BatchSize = 8;

    private ChromaDbSettings _settings;

    public ChromaDbClient(IHttpClientFactory httpClientFactory, IOptions<ChromaDbSettings> options)
    {
        _httpClient = httpClientFactory.CreateClient();
        _settings = options.Value;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
    }

    public async Task StoreEmbeddingsAsync(IAsyncEnumerable<(string Chunk, float[] Embedding)> embeddings)
    {
        var batchChunks = new List<string>();
        var batchEmbeddings = new List<float[]>();
        var batchMetas = new List<Dictionary<string, object>>();

        int idCounter = 0;
        await foreach (var (chunk, embedding) in embeddings)
        {
            batchChunks.Add(chunk);
            batchEmbeddings.Add(embedding);
            batchMetas.Add(new Dictionary<string, object> { { "source", "pdf" } });
            if (batchChunks.Count >= BatchSize)
            {
                idCounter = await UpsertBatchAsync(_settings.CollectionId, batchChunks, batchEmbeddings, batchMetas, idCounter);
                batchChunks.Clear();
                batchEmbeddings.Clear();
                batchMetas.Clear();
            }
        }

        if (batchChunks.Count > 0)
        {
            idCounter = await UpsertBatchAsync(_settings.CollectionId, batchChunks, batchEmbeddings, batchMetas, idCounter);
        }
    }

    public async Task<ChromaDbCollectionResponse?> GetCollectionAsync(string collection)
    {
        var requestUri = _settings.Collection_GET();
        var resp = await _httpClient.GetAsync(requestUri);

        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await resp.Content.ReadFromJsonAsync<ChromaDbCollectionResponse>();

        return result;
    }

    public async Task<List<(string Chunk, float[] Embedding, Dictionary<string, object> Meta, float Distance)>> QueryEmbeddingsAsync(float[] embedding, int nResults = 5)
    {
        var queryReq = new
        {
            query_embeddings = new List<float[]> { embedding },
            n_results = nResults,
            include = new[] { "documents", "metadatas", "distances", "embeddings" }
        };

        var resp = await _httpClient.PostAsJsonAsync(_settings.Collection_QUERY(_settings.CollectionId), queryReq);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var results = new List<(string, float[], Dictionary<string, object>, float)>();

        var documents = root.GetProperty("documents")[0];
        var embeddings = root.GetProperty("embeddings")[0];
        var metadatas = root.GetProperty("metadatas")[0];
        var distances = root.GetProperty("distances")[0];

        for (int i = 0; i < documents.GetArrayLength(); i++)
        {
            var chunk = documents[i].GetString() ?? string.Empty;
            var embeddingArr = embeddings[i].EnumerateArray().Select(x => x.GetSingle()).ToArray();
            var metaDict = new Dictionary<string, object>();

            foreach (var prop in metadatas[i].EnumerateObject())
            {
                metaDict[prop.Name] = prop.Value.ToString();
            }

            var distance = distances[i].GetSingle();

            results.Add((chunk, embeddingArr, metaDict, distance));
        }

        return results;
    }

    public async Task ConfigureDatabase()
    {
        await EnsureTenantExistsAsync();
        await EnsureDatabaseExistsAsync();
        var collection = await EnsureCollectionExistsAsync();

        _settings.CollectionId = collection.Id.ToString();
    }

    private async Task<ChromaDbCollectionResponse> EnsureCollectionExistsAsync()
    {
        var collection  = await GetCollectionAsync(_settings.Collection);

        if (collection != null)
        {
            return collection;
        }

        var createReq = CreateCollectionRequest.Create(_settings.Collection);

        var createResp = await _httpClient.PostAsJsonAsync(_settings.Collection_POST(), createReq);
        createResp.EnsureSuccessStatusCode();

        collection = await createResp.Content.ReadFromJsonAsync<ChromaDbCollectionResponse>();

        return collection ?? throw new InvalidOperationException("Deserialized collection was null.");
    }

    private async Task EnsureTenantExistsAsync()
    {
        var resp = await _httpClient.GetAsync(_settings.Tenant_GET());

        if (resp.IsSuccessStatusCode) return;

        var createReq = new { name = _settings.Tenant };

        var createResp = await _httpClient.PostAsJsonAsync(_settings.Tenant_POST(), createReq);

        createResp.EnsureSuccessStatusCode();
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        var resp = await _httpClient.GetAsync(_settings.Database_GET());

        if (resp.IsSuccessStatusCode) return;

        var createReq = new { name = _settings.Database };

        var createResp = await _httpClient.PostAsJsonAsync(_settings.Database_POST(), createReq);

        createResp.EnsureSuccessStatusCode();
    }

    private async Task<int> UpsertBatchAsync(string collectionId, List<string> chunks, List<float[]> embeddings, List<Dictionary<string, object>> metas, int idCounter)
    {
        var ids = Enumerable.Range(idCounter, chunks.Count).Select(i => HashUtils.HashToHex(chunks[i])).ToList();
        idCounter += chunks.Count;

        var upsertReq = new
        {
            ids,
            embeddings,
            documents = chunks,
            metadatas = metas
        };

        var resp = await _httpClient.PostAsJsonAsync(_settings.Collection_UPSERT(collectionId), upsertReq);
        resp.EnsureSuccessStatusCode();

        return idCounter;
    }
} 