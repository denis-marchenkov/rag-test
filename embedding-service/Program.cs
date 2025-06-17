using Application;
using Application.Interfaces;
using embedding_service.Configuration;
using embedding_service.Application.DTO;
using embedding_service.Application.DTO.LLM;
using System.Text.Json;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configure the port from settings
var embeddingSettings = builder.Configuration.GetSection("EmbeddingService").Get<EmbeddingServiceSettings>();
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(embeddingSettings.Port);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.IncludeFields = true;
});

builder.Services.Configure<ChromaDbSettings>(builder.Configuration.GetSection("ChromaDb"));

builder.Services.Configure<LLMSettings>(builder.Configuration.GetSection("LLM"));

builder.Services.Configure<EmbeddingServiceSettings>(builder.Configuration.GetSection("EmbeddingService"));

builder.Services.Configure<UISettings>(builder.Configuration.GetSection("UISettings"));

builder.Services.AddSingleton(new DataFolderOptions { FolderPath = builder.Configuration.GetValue<string>("DataFolder") ?? "./data" });


builder.Services.AddSingleton<IPdfReaderService, PdfReaderService>();
builder.Services.AddSingleton<IChunkerService, ChunkerService>();
builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
builder.Services.AddSingleton<IChromaDbClient, ChromaDbClient>();
builder.Services.AddSingleton<IEmbeddingJobStatus, EmbeddingJobStatus>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policyBuilder =>
        {
            var uiSettings = builder.Configuration.GetSection("UISettings").Get<UISettings>();

            policyBuilder.WithOrigins(uiSettings.AllowOrigin)
                       .AllowAnyHeader()
                       .AllowAnyMethod();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigin");

// Configure ChromaDB
using (var scope = app.Services.CreateScope())
{
    var chromaDbClient = scope.ServiceProvider.GetRequiredService<IChromaDbClient>();
    await chromaDbClient.ConfigureDatabase();
}

app.MapPost("/process-pdfs", async (IPdfReaderService pdfReader, IChunkerService chunker, IEmbeddingService embedder, IChromaDbClient chroma, IEmbeddingJobStatus status) =>
{
    status.SetStatus("Processing");

    var pdfs = pdfReader.ReadAllPdfsAsync();
    var chunks = chunker.ChunkPdfsAsync(pdfs);
    var embeddings = embedder.GenerateEmbeddingsAsync(chunks);

    await chroma.StoreEmbeddingsAsync(embeddings);

    status.SetStatus("Idle");

    return Results.Ok(new { message = "PDFs processed and embeddings stored." });
});

app.MapPost("/chat", async (ChatRequest request, IEmbeddingService embedder, IChromaDbClient chroma, IHttpClientFactory httpClientFactory, IOptions<LLMSettings> llmSettingsOptions) =>
{
    var llmSettings = llmSettingsOptions.Value;
    var llmClient = httpClientFactory.CreateClient();
    llmClient.BaseAddress = new Uri(llmSettings.BaseUrl);

    var chatMessages = new List<Message>();
    string userQuery = request.UserMessage;

    if (request.UseEmbeddings)
    {
        float[] queryEmbedding = null;
        await foreach (var item in embedder.GenerateEmbeddingsAsync(new List<string> { userQuery }.ToAsyncEnumerable()))
        {
            queryEmbedding = item.Embedding;
            break;
        }

        if (queryEmbedding == null)
        {
            return Results.BadRequest(new { message = "Failed to generate embedding for the query." });
        }

        var relevantChunks = await chroma.QueryEmbeddingsAsync(queryEmbedding, nResults: 3);

        var context = string.Join("\n\n", relevantChunks.Select(c => c.Chunk));

        chatMessages.Add(new Message { Role = "system", Content = $"You are a helpful assistant. Use the following context to answer the user's question:\n\n{context}" });
    }

    chatMessages.Add(new Message { Role = "user", Content = userQuery });

    var llmRequest = new LLMChatRequest
    {
        Messages = chatMessages,
        Model = llmSettings.Model
    };

    var llmResponse = await llmClient.PostAsJsonAsync("api/chat", llmRequest);
    llmResponse.EnsureSuccessStatusCode();

    var fullLLMResponse = await llmResponse.Content.ReadFromJsonAsync<ChatResponse>();

    return Results.Ok(new { LLMResponse = fullLLMResponse?.Message?.Content ?? "No response from LLM." });
});


app.MapPost("/finetune", async (ChatRequest request, IEmbeddingService embedder, IChromaDbClient chroma, IHttpClientFactory httpClientFactory, IOptions<LLMSettings> llmSettingsOptions) =>
{
    var llmSettings = llmSettingsOptions.Value;
    var llmClient = httpClientFactory.CreateClient();
    llmClient.BaseAddress = new Uri(llmSettings.BaseUrl);

    var chatMessages = new List<Message>();
    string userQuery = request.UserMessage;

    float[] queryEmbedding = null;
    await foreach (var item in embedder.GenerateEmbeddingsAsync(new List<string> { userQuery }.ToAsyncEnumerable()))
    {
        queryEmbedding = item.Embedding;
        break;
    }

    if (queryEmbedding == null)
    {
        return Results.BadRequest(new { message = "Failed to generate embedding for the query." });
    }

    var embeddings = new List<(string, float[])> { (userQuery, queryEmbedding) }.ToAsyncEnumerable();
    await chroma.StoreEmbeddingsAsync(embeddings);


    return Results.Ok(new {Embeddings = embeddings });
});

app.MapGet("/status", (IEmbeddingJobStatus status) => Results.Ok(new { status = status.GetStatus() }));

app.Run();
