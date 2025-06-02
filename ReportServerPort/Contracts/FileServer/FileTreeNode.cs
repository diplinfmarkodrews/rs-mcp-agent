using ReportServerPort.Contracts;

namespace ReportServerPort.FileServer.Contracts;

public class FileTreeNode : IContract
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsFolder { get; set; }
    public List<FileTreeNode> Children { get; set; }
}