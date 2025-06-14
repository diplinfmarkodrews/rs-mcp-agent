using System.IO;
using RSChatApp.Web.Services;

namespace RSChatApp.Web.Services.Ingestion;

public class TextDirectorySource(string directoryPath) : IIngestionSource
{
    public string SourceId => $"{nameof(TextDirectorySource)}:{directoryPath}";

    public Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var results = new List<IngestedDocument>();
        var sourceFiles = Directory.GetFiles(directoryPath, "*.txt");
        var existingDocumentsById = existingDocuments.ToDictionary(d => d.DocumentId);

        foreach (var sourceFile in sourceFiles)
        {
            var sourceFileId = Path.GetFileName(sourceFile);
            var sourceFileVersion = File.GetLastWriteTimeUtc(sourceFile).ToString("o");
            var existingDocumentVersion = existingDocumentsById.TryGetValue(sourceFileId, out var existingDocument) ? existingDocument.DocumentVersion : null;
            if (existingDocumentVersion != sourceFileVersion)
            {
                results.Add(new IngestedDocument
                {
                    Key = Guid.NewGuid(),
                    SourceId = SourceId,
                    DocumentId = sourceFileId,
                    DocumentVersion = sourceFileVersion
                });
            }
        }
        return Task.FromResult((IEnumerable<IngestedDocument>)results);
    }

    public Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var currentFiles = Directory.GetFiles(directoryPath, "*.txt");
        var currentFileIds = currentFiles.Select(Path.GetFileName).ToHashSet();
        var deletedDocuments = existingDocuments.Where(d => !currentFileIds.Contains(d.DocumentId));
        return Task.FromResult(deletedDocuments);
    }

    public Task<IEnumerable<IngestedChunk>> CreateChunksForDocumentAsync(IngestedDocument document)
    {
        var filePath = Path.Combine(directoryPath, document.DocumentId);
        var text = File.ReadAllText(filePath);
        var chunks = SplitTextIntoChunks(text, 2000) // 2000 chars per chunk
            .Select((chunk, idx) => new IngestedChunk
            {
                Key = Guid.NewGuid(),
                DocumentId = document.DocumentId,
                PageNumber = idx + 1,
                Text = chunk
            });
        return Task.FromResult(chunks);
    }

    private static IEnumerable<string> SplitTextIntoChunks(string text, int maxChunkSize)
    {
        for (int i = 0; i < text.Length; i += maxChunkSize)
        {
            yield return text.Substring(i, Math.Min(maxChunkSize, text.Length - i));
        }
    }
}

