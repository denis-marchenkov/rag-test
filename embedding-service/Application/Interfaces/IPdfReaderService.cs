namespace Application.Interfaces;

public interface IPdfReaderService
{
    IAsyncEnumerable<string> ReadAllPdfsAsync();
} 