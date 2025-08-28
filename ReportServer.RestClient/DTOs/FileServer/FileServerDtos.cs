namespace ReportServer.RestClient.DTOs.FileServer;

public class FileInfoDto
{
    public string? Name { get; set; }
    public long Size { get; set; }
    public bool IsDirectory { get; set; }
    public DateTime LastModified { get; set; }
    public string? Path { get; set; }
    public string? ContentType { get; set; }
}

public class FileListResponseDto
{
    public List<FileInfoDto>? Files { get; set; }
    public string? CurrentPath { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UploadFileRequestDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string? TargetPath { get; set; }
}

public class UploadFileResponseDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FilePath { get; set; }
}
