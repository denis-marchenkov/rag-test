namespace embedding_service.Configuration;

public class LLMSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434/";
    public string Model { get; set; }
} 