using System.Text.Json.Serialization;

namespace embedding_service.Application.DTO;

public class ChatRequest
{
    public required string UserMessage { get; set; }
    public bool UseEmbeddings { get; set; }
}

public class MessageResponse
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }
    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

public class ChatResponse
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }
    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; set; }
    [JsonPropertyName("message")]
    public required MessageResponse Message { get; set; }
    [JsonPropertyName("done_reason")]
    public required string DoneReason { get; set; }
    [JsonPropertyName("done")]
    public required bool Done { get; set; }
    [JsonPropertyName("total_duration")]
    public required long TotalDuration { get; set; }
    [JsonPropertyName("load_duration")]
    public required long LoadDuration { get; set; }
    [JsonPropertyName("prompt_eval_count")]
    public required int PromptEvalCount { get; set; }
    [JsonPropertyName("prompt_eval_duration")]
    public required long PromptEvalDuration { get; set; }
    [JsonPropertyName("eval_count")]
    public required int EvalCount { get; set; }
    [JsonPropertyName("eval_duration")]
    public required long EvalDuration { get; set; }
} 