namespace MusicRec.Web.Options;

public sealed class ServiceEndpointsOptions
{
    public const string SectionName = "ServiceEndpoints";

    public string IdentityApi { get; set; } = "http://localhost:5071";

    public string CatalogApi { get; set; } = "http://localhost:5072";

    public string RecommendationApi { get; set; } = "http://localhost:5073";
}
