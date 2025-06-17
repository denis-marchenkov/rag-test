namespace embedding_service.Application.DTO.LLM;

public class Message
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}

public class LLMChatRequest
{
    public required List<Message> Messages { get; set; }
    public string Model { get; set; } = "llama2:latest";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = -1;
    public bool Stream { get; set; } = false;
} 