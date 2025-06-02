# MCP Server for ReportServer Java Application

This workspace contains a Model Context Protocol (MCP) server implementation for connecting to a Java ReportServer application, with a chat agent interface. The project leverages Microsoft's latest technologies for AI integration and cloud-native application development.

## 🚧 Project Status: Under Development

This project is under development. The following components are being implemented:
- ReportServer RPC interface integration
- MCP Client connection
- Independent MCP server deployment for VS Code integration

## 🎯 Quick Start (Development)

### Aspire App Host with MCP Server SDK

The fastest way to get started with the development environment is using Aspire AppHost:

```bash
# Start the Aspire AppHost
cd RSChatApp.AppHost
dotnet run
```

This will start the following containerized services:
- MCP Server (RsMcpServerSDK.Web)
- Chat Web Application (RSChatApp.Web)
- Ollama for AI models
- Qdrant for vector search

## Core Components

### 🚀 MCP Server with ReportServer Integration

- **RsMcpServerSDK.Web/**: Modern MCP server using Microsoft Extensions AI framework
- **RSChatApp.Web/**: Interactive Blazor web client with chat UI
- **ReportServerRPCClient/**: Direct RPC client for Java ReportServer integration
- **RSChatApp.AppHost/**: .NET Aspire orchestration for cloud-native deployment

#### Key Features
- ✅ Uses official Microsoft Extensions AI SDK
- ✅ Full .NET 9.0 integration with Aspire orchestration
- ✅ Direct ReportServer RPC integration
- ✅ Comprehensive logging and error handling

## Project Structure

### Modern MCP Server Implementation with .NET Aspire

- **RSChatApp.AppHost/**: .NET Aspire app host that orchestrates all components
  - **Program.cs**: Configures and links Ollama, Qdrant, MCP Server, and Web App
  - **appsettings.json**: Configuration settings

- **RsMcpServerSDK.Web/**: Modern MCP server implementation
  - **Program.cs**: Entry point with Microsoft.Extensions.AI MCP server configuration
  - **Services/McpReportServer.cs**: MCP server with decorated functions
  - **Models/**: Data models for MCP responses

- **RSChatApp.Web/**: Interactive chat web application
  - **Program.cs**: Web app configuration with AI client setup
  - **Components/**: Blazor UI components
  - **Services/**: AI chat services, vector search, and data ingestion

- **ReportServerRPCClient/**: Direct RPC client for Java ReportServer
  - **Services/**: Implementation of RPC client
  - **DTOs/**: Data transfer objects for RPC communication

- **ReportServerPort/**: Interface definitions for Report Server communication
  - **IReportServerClient.cs**: Main interface for communicating with ReportServer
  - **Contracts/**: Data contracts for the ReportServer API

## Prerequisites

- .NET 9.0 SDK or later
- Java JDK 17 or later (for ReportServer)
- An Ollama-compatible AI model (default: mistral-nemo:12b for chat, all-minilm for embeddings)
- Docker (for Aspire containerized resources)

## Getting Started

### Starting the Application with Aspire

1. Ensure you have Docker running on your system

2. Navigate to the RSChatApp.AppHost directory:

```bash
cd RSChatApp.AppHost
```

3. Run the application:

```bash
dotnet run
```

This will start all required services in the correct order:
- Ollama (with specified models)
- Qdrant vector database
- RsMcpServerSDK.Web MCP server
- RSChatApp.Web Blazor web application

4. Open the Aspire dashboard at the provided URL (typically http://localhost:15986) to monitor all services

5. Access the chat web interface at the URL shown in the dashboard (typically http://localhost:5123)

### Testing the MCP Server

You can test the MCP server functionality using the provided test script:

```bash
chmod +x test-mcp-server.sh
./test-mcp-server.sh
```

Or test directly using the Aspire dashboard to monitor service health and interactions.

## How It Works

### Direct ReportServer RPC Client

The .NET MCP server communicates with the Java-based ReportServer using a direct RPC client:

1. **ReportServerRPCClient**: Contains the C# code that directly communicates with the ReportServer
   - Implements a client for the ReportServer's RPC protocol
   - Handles authentication and session management
   - Provides strongly-typed methods for report operations

2. **RsMcpServerSDK.Web**: The MCP server that exposes the functionality via the Model Context Protocol
   - Implements the Microsoft.Extensions.AI MCP server framework
   - Provides tool functions that can be called by AI agents
   - Maps between MCP requests and ReportServer operations

### Model Context Protocol (MCP) Integration

The MCP server provides an HTTP API for AI models and integrates with the ReportServer for generating reports:

1. Users can ask the AI about available reports through the chat interface
2. The AI detects report-related queries and leverages MCP tools
3. For report queries, the MCP service uses the ReportServerRPCClient to communicate with the Java ReportServer
4. Reports are generated on the Java side and provided back to the user via the AI-powered chat interface

## Customizing the MCP Server

You can customize the MCP server by modifying the settings in `appsettings.json`:

- Change the Ollama model used for chat completions
- Configure the embedding model
- Set the ReportServer address

```json
{
  "Ollama": {
    "Model": "mistral-nemo:12b",
    "EmbeddingModel": "all-minilm"
  },
  "ReportServer": {
    "Address": "localhost:1099"
  }
}
```

## Roadmap

### Current Development Focus

1. Completing the ReportServer RPC interface implementation
   - Adding full support for report generation
   - Implementing authentication and session management
   - Adding data source integration

2. Enhancing the MCP Client connection
   - Improving tool definitions for AI model integration
   - Extending the chat context with report metadata
   - Adding support for complex parameter handling

3. Enabling independent MCP server deployment
   - Creating standalone deployment options
   - Adding VS Code extension integration
   - Supporting containerized deployments

## Troubleshooting

### ReportServer Connection Issues

- Verify that your Java ReportServer is running and accessible at the configured address
- Check connectivity from the ReportServerRPCClient
- Use the test-mcp-server.sh script to verify the connection to the ReportServer

### MCP Server Issues

- Ensure Docker is running for the Aspire containerized services
- Check the logs in the Aspire dashboard for any errors
- Verify that Ollama is properly initialized with the required models

## Extending the Project

### Adding More Report Types

You can extend the report capabilities by:

1. Updating the ReportServerRPCClient with new methods
2. Adding corresponding functions to the McpReportServer class
3. Decorating new functions with the [McpServerTool] attribute

### Supporting Other AI Models

The project is set up to use Ollama models by default, but you can modify it to use other models:

1. Update the RSChatApp.Web/Program.cs file to use a different model provider
2. Add the necessary packages for your preferred AI model

## License

[MIT License](LICENSE)

## Further Resources

- [Microsoft Extensions AI Documentation](https://github.com/microsoft/microsoft-extensions-ai)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/llms-full.txt)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [Ollama Documentation](https://ollama.ai/docs)

## ReportServer

### Starting ReportServer

ReportServer is a Java web application that can be started in several ways:

1. Using Bitnami Stack (Recommended for quick start)
   - Download from: https://bitnami.com/stack/reportserver
   - This provides a pre-configured environment with all dependencies

2. Manual Deployment
   - Build the WAR file from source
   - Deploy to a Java application server (Tomcat, JBoss, etc.)
   - Configure database connection in hibernate.properties
   - Ensure all required dependencies are in the classpath

3. Development Environment
   - Use Maven/Gradle to build: `mvn clean install`
   - Deploy to your development server
   - The application will be available at http://localhost:8080/reportserver (default)

### RPC Interface Details

Based on the codebase analysis, ReportServer has an extensive RPC interface system. Here are the main RPC services that will be integrated:

1. Report Management
   - ReportManagerTreeHandlerImpl - Report tree operations
   - ReportExecutorRpcService (@RemoteServiceRelativePath("executor"))
   - executeAs() - Execute report in specific format
   
2. Report Export
   - ReportExportRpcServiceImpl
   - storeInSessionForExport() - Prepare reports for export

3. Report Properties
   - ReportPropertiesRpcService (@RemoteServiceRelativePath("reportproperties"))
   - getPropertyKeys() - Get available property keys
   - updateProperties() - Update report properties

4. Data Source Management
   - DatasourceManagerTreeHandlerRpcServiceImpl
   - BaseDatasourceRpcServiceImpl

5. Access Points
   - Web Interface: http://localhost:8080/reportserver
   - REST API: Various endpoints for programmatic access
