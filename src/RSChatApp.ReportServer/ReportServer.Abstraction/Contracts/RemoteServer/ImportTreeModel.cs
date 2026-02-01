namespace ReportServer.Abstraction.Contracts.RemoteServer;

public class ImportTreeModel : IContract
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public List<ImportTreeModel> Children { get; set; }
}

