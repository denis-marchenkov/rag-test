using Application.Interfaces;

namespace Application;

public class EmbeddingJobStatus : IEmbeddingJobStatus
{
    private string _status = "Idle";
    public void SetStatus(string status) => _status = status;
    public string GetStatus() => _status;
} 