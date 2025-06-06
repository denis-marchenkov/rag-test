using System.Text.Json.Serialization;

namespace embedding_service.DTO.ChromaRequest
{
    public class CreateCollectionRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("get_or_create")]
        public bool GetOrCreate { get; set; }

        public static CreateCollectionRequest Create(string name, bool getOrCreate = true)
        {
            return new CreateCollectionRequest
            {
                Name = name,
                GetOrCreate = getOrCreate
            };
        }
    }
}
