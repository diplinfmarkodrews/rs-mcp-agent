# MCP Client SDK

This project implements a Model Context Protocol (MCP) client using the Microsoft Extensions AI MCP SDK for .NET 9.0.

## Overview

The MCP Client SDK provides an interactive console application that demonstrates communication with MCP servers using Microsoft's Extensions AI framework. It includes a rich user interface for interacting with report generation services.

## Architecture

### Key Components

- **`McpClientService`**: Main MCP client implementation for server communication
- **`InteractiveClient`**: Rich console UI for user interaction
- **Data Models**: Shared models for MCP communication

### Features

#### Interactive Console Menu
- 📋 View Available Templates
- 📊 Generate Reports with Interactive Parameter Collection
- 💚 Real-time Health Status Monitoring
- 🚀 Automated Demo Mode
- Rich formatting with emojis and colors

#### Report Generation
- Template selection and validation
- Interactive parameter collection with type validation
- Progress reporting during generation
- Multiple output format support

#### Health Monitoring
- Real-time server health checks
- Detailed component status reporting
- Service availability tracking

## User Interface

### Main Menu
```
🔄 MCP Report Client SDK - Main Menu
====================================

1. 📋 View Available Templates
2. 📊 Generate Report
3. 💚 Check Health Status
4. 🚀 Run Demo
5. 🚪 Exit
```

### Template Display
```
🔸 ID: monthly-summary
   Name: Monthly Summary Report
   Description: A comprehensive summary of monthly activities and metrics
   Formats: pdf, html, excel
   Parameters:
     • month (number) (required): Month (1-12)
     • year (number) (required): Year (YYYY)
     • department (string) (optional): Department name
```

### Health Status
```
💚 Health Status Check
=====================
✅ Status: Healthy
🔢 Version: 1.0.0
🕐 Timestamp: 2025-05-25 20:31:48 UTC
📋 Details:
   • ReportEngine: Available
   • Database: Connected
   • TemplateCache: Loaded
   • QueueLength: 0
```

## MCP Client Functions

### GetReportTemplatesAsync()
Retrieves available report templates from the MCP server with full metadata including required parameters and supported formats.

### GenerateReportAsync()
Generates reports by:
1. Validating template existence
2. Checking required parameters
3. Validating output format support
4. Processing report generation with progress tracking
5. Returning generated report data with metadata

### GetHealthStatusAsync()
Monitors server health status including:
- Overall service health
- Component availability
- Performance metrics
- Service version information

## Interactive Features

### Report Generation Workflow
1. **Template Selection**: Choose from available templates
2. **Parameter Input**: Interactive collection of required parameters
3. **Format Selection**: Choose output format (PDF, HTML, Excel)
4. **Progress Tracking**: Real-time generation progress
5. **Result Display**: Success confirmation with file details

### Demo Mode
Automated demonstration of all client capabilities:
- Health status verification
- Template discovery
- Sample report generation
- Multiple format testing

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
- **Microsoft.Extensions.Hosting**: Application hosting
- **Microsoft.Extensions.Http**: HTTP client services
- **Microsoft.Extensions.Logging**: Logging infrastructure
- **System.Text.Json**: JSON serialization

## VS Code Tasks

The following VS Code tasks are available:
- `build-mcp-client-sdk`: Build the project
- `run-mcp-client-sdk`: Run the interactive client

## Configuration

The client uses dependency injection for service configuration:

```csharp
// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Register services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<McpClientService>();
builder.Services.AddSingleton<InteractiveClient>();
```

## Usage Examples

### Starting the Client
```bash
cd MCPClientSDK
dotnet run
```

### Using Demo Mode
1. Select option 4 from the main menu
2. Watch automated demonstration of all features
3. See successful MCP function calls and responses

### Manual Report Generation
1. Select option 2 from the main menu
2. Choose a template from the list
3. Enter required parameters interactively
4. Select output format
5. Monitor generation progress
6. View success confirmation

## Error Handling

The client includes comprehensive error handling:
- Template validation
- Parameter type checking
- Format compatibility verification
- Network connection error handling
- User input validation

## Next Steps

To integrate with actual MCP protocol communication:

1. Replace simulation methods with real MCP transport layer
2. Add authentication and authorization
3. Implement persistent session management
4. Add advanced reporting features
5. Configure for production deployment

## License

This project follows the same license as the parent workspace.
