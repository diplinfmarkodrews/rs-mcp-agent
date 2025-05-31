namespace ReportServerPort.Contracts;

public class ImportTreeModelResult : IContract
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public List<ImportTreeModelResult> Children { get; set; }
}

