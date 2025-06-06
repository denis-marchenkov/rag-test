namespace Application.Interfaces;

public interface IChunkerService
{
    IAsyncEnumerable<string> ChunkPdfsAsync(IAsyncEnumerable<string> pdfs);
} 