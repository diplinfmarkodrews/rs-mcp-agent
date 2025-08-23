using Microsoft.Extensions.VectorData;

namespace RSChatApp.Web.Models.Ingestion;

public class IngestedChunk
{
    private const int VectorDimensions = 1024; // 1024 is the vector size for the snowflake-arctic-embed2 model
    private const string VectorDistanceFunction = DistanceFunction.CosineSimilarity;

    [VectorStoreKey]
    public required Guid Key { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public required string DocumentId { get; set; }

    [VectorStoreData]
    public int PageNumber { get; set; }

    [VectorStoreData]
    public required string Text { get; set; }

    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction)]
    public string? Vector => Text;
}
