namespace ReportServerPort.Contracts;

public class FileTreeNodeResult : IContract
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsFolder { get; set; }
    public List<FileTreeNodeResult> Children { get; set; }
}