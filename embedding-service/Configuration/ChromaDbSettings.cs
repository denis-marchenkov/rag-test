namespace embedding_service.Configuration
{
    public class ChromaDbSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:8000/api/v2";
        public string Tenant { get; set; } = "test_tenant";
        public string Database { get; set; } = "test_database";
        public string Collection { get; set; } = "test_collection";
        public string CollectionId { get; set; }


        public string Tenant_GET()
        {
            return $"{BaseUrl}/tenants/{Tenant}";
        }
        public string Tenant_POST()
        {
            return $"{BaseUrl}/tenants";
        }

        public string Database_GET()
        {
            return $"{BaseUrl}/tenants/{Tenant}/databases/{Database}";
        }
        public string Database_POST()
        {
            return $"{BaseUrl}/tenants/{Tenant}/databases";
        }

        public string Collection_GET(string collectionId = null)
        {
            collectionId ??= Collection;
            return $"{BaseUrl}/tenants/{Tenant}/databases/{Database}/collections/{collectionId}";
        }
        public string Collection_POST()
        {
            return $"{BaseUrl}/tenants/{Tenant}/databases/{Database}/collections";
        }

        public string Collection_UPSERT(string collectionId)
        {
            return $"{BaseUrl}/tenants/{Tenant}/databases/{Database}/collections/{collectionId}/upsert";
        }

        public string Collection_QUERY(string collectionId)
        {
            return $"{Collection_GET(collectionId)}/query";
        }
    }
}
