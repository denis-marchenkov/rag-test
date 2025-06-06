using Application;
using Application.Interfaces;
using embedding_service.Configuration;
using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.IncludeFields = true;
});

builder.Services.Configure<ChromaDbSettings>(builder.Configuration.GetSection("ChromaDb"));

var dataFolder = builder.Configuration.GetValue<string>("DataFolder") ?? "./data";
builder.Services.AddSingleton(new DataFolderOptions { FolderPath = dataFolder });


builder.Services.AddSingleton<IPdfReaderService, PdfReaderService>();
builder.Services.AddSingleton<IChunkerService, ChunkerService>();
builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
builder.Services.AddSingleton<IChromaDbClient, ChromaDbClient>();
builder.Services.AddSingleton<IEmbeddingJobStatus, EmbeddingJobStatus>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
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

app.MapGet("/status", (IEmbeddingJobStatus status) => Results.Ok(new { status = status.GetStatus() }));

app.Run();
