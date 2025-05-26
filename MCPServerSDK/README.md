# MCP Server SDK

This project implements a Model Context Protocol (MCP) server using the Microsoft Extensions AI MCP SDK for .NET 9.0.

## Overview

The MCP Server SDK provides a standardized way to implement MCP servers using Microsoft's Extensions AI framework. This implementation showcases a report generation service with three main functions:

- **GetReportTemplatesAsync**: Returns available report templates
- **GenerateReportAsync**: Creates reports based on templates and parameters
- **GetHealthStatusAsync**: Provides server health monitoring

## Architecture

### Key Components

- **`McpReportServer`**: Main MCP server implementation with decorated methods
- **`McpServerHostedService`**: Background service host for the MCP server
- **Data Models**: Comprehensive models for templates, parameters, and results

### MCP Functions

#### GetReportTemplatesAsync()
```csharp
[Description("Gets a list of all available report templates")]
public async Task<GetTemplatesResult> GetReportTemplatesAsync()
```

Returns available report templates with their required parameters and supported formats.

#### GenerateReportAsync()
```csharp
[Description("Generates a report using the specified template and parameters")]
public async Task<GenerateReportResult> GenerateReportAsync(
    [Description("The ID of the report template to use")] string templateId,
    [Description("Parameters for the report generation")] Dictionary<string, object> parameters,
    [Description("Output format (pdf, html, excel)")] string format = "pdf",
    [Description("Whether to include charts and visualizations")] bool includeCharts = true)
```

Generates reports with parameter validation and progress tracking.

#### GetHealthStatusAsync()
```csharp
[Description("Checks the health status of the report generation service")]
public async Task<HealthStatus> GetHealthStatusAsync()
```

Provides comprehensive health monitoring with service status details.

## Configuration

The server uses `appsettings.json` for configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "McpServer": {
    "ServiceName": "MCP Report Server",
    "Version": "1.0.0"
  }
}
```

## Building and Running

### Prerequisites
- .NET 9.0 SDK
- Microsoft.Extensions.AI package (preview)

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

## Dependencies

- **Microsoft.Extensions.AI**: Core MCP SDK framework
- **Microsoft.Extensions.Hosting**: Background service hosting
- **Microsoft.Extensions.Logging**: Logging infrastructure
- **System.Text.Json**: JSON serialization

## VS Code Tasks

The following VS Code tasks are available:
- `build-mcp-server-sdk`: Build the project
- `run-mcp-server-sdk`: Run the server in background

## Features

### Template Management
- Pre-defined report templates with metadata
- Parameter validation and type checking
- Multiple output format support (PDF, HTML, Excel)

### Report Generation
- Asynchronous report processing
- Progress tracking and logging
- Comprehensive error handling
- Sample data generation for testing

### Health Monitoring
- Real-time health status reporting
- Service component status tracking
- Performance metrics collection

## Example Usage

The server automatically starts when run and exposes MCP functions that can be called by compatible MCP clients. The implementation includes comprehensive logging and error handling.

## Next Steps

To integrate with actual MCP protocol communication:

1. Configure MCP transport layer (stdio, HTTP, WebSocket)
2. Replace simulation methods with real MCP protocol handling
3. Add authentication and security features
4. Implement persistent storage for templates and reports

## License

This project follows the same license as the parent workspace.
