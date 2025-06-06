namespace Application.Interfaces;

public interface IEmbeddingJobStatus
{
    void SetStatus(string status);
    string GetStatus();
} 