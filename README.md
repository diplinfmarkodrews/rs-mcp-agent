# MCP Server with Java RPC Client

This project demonstrates how to implement a Model Context Protocol (MCP) server in .NET that can connect to a Java-based ReportServer via Java RMI with JNI.

## Project Structure

- **MCPServer/**: A .NET implementation of an MCP server using gRPC with JNI bridge to Java
  - **Program.cs**: Main entry point for the server
  - **Services/MCPServiceImpl.cs**: Implementation of the MCP service
  - **Services/ReportServerClient.cs**: Client for communicating with Java ReportServer via JNI
  - **Protos/mcp.proto**: Protocol Buffer definition for the MCP service

- **JavaClient/**: A Java client library with JNI bridge for RMI communication
  - **src/main/java/com/example/reportserver/bridge/**: JNI bridge implementation
    - **ReportServerJniBridge.java**: Main bridge class between C# and Java RMI
    - **ReportServerInterface.java**: RMI interface for the ReportServer
    - **ReportResult.java**, **ReportTemplate.java**, etc.: Model classes for RMI communication

## Prerequisites

- .NET 9.0 SDK or later
- Java JDK 17 or later
- Maven for building the Java client
- An OpenAI API key
- JAVA_HOME environment variable set correctly
- grpcurl (optional, for testing)

## Getting Started

### Building the Java JNI Bridge

1. Set your JAVA_HOME environment variable:

```bash
# For Linux/macOS
export JAVA_HOME=/path/to/your/jdk

# For Windows PowerShell
$env:JAVA_HOME="C:\path\to\your\jdk"
```

2. Run the build script for the Java JNI bridge:

```bash
chmod +x build-java-bridge.sh
./build-java-bridge.sh
```

This will build the Java client and copy the JAR file to the MCPServer's lib directory.

### Setting up the MCP Server

1. Navigate to the MCPServer directory:

```bash
cd MCPServer
```

2. Set your OpenAI API key:

```bash
# For Linux/macOS
export OpenAI__ApiKey="your-openai-api-key"

# For Windows PowerShell
$env:OpenAI__ApiKey="your-openai-api-key"
```

3. Run the server:

```bash
dotnet run
```

The server will start on https://localhost:5001 and http://localhost:5000.

### Testing the MCP Server

You can use the provided test script to verify the MCP server functionality:

```bash
chmod +x test-mcp-server.sh
./test-mcp-server.sh
```

Or test manually using grpcurl:

```bash
# Health check
grpcurl -plaintext -proto ./MCPServer/Protos/mcp.proto localhost:5000 mcp.MCPService/HealthCheck

# Chat request
grpcurl -plaintext -proto ./MCPServer/Protos/mcp.proto -d '{"messages":[{"role":1,"content":"What reports are available?"}],"model":"gpt-3.5-turbo","temperature":0.7}' localhost:5000 mcp.MCPService/Chat
```

## How It Works

### Java Native Interface (JNI) Bridge

The .NET MCP server communicates with a Java-based ReportServer using Java Native Interface (JNI):

1. **ReportServerClient.cs**: Contains the C# code that uses JNI to call Java methods
   - Initializes the JVM and loads the Java bridge JAR
   - Uses JNI P/Invoke to call Java methods
   - Converts between C# and Java data types

2. **ReportServerJniBridge.java**: The Java bridge class that communicates with the ReportServer via RMI
   - Connects to the ReportServer using Java RMI
   - Provides methods that can be called from C# via JNI
   - Handles serialization and error handling

### Model Context Protocol (MCP) Integration

The MCP server provides a gRPC API for AI models and integrates with the ReportServer for generating reports:

1. Users can ask the AI about available reports
2. The MCP service detects report-related queries using keyword matching
3. For report queries, the MCP service uses the ReportServerClient to communicate with the Java ReportServer
4. Reports are generated on the Java side and provided back to the user via the MCP API

## Customizing the MCP Server

You can customize the MCP server by modifying the settings in `appsettings.json`:

- Change the OpenAI model used for chat completions
- Configure the embedding model
- Set the ReportServer address

```json
{
  "OpenAI": {
    "ApiKey": "",
    "ModelId": "gpt-3.5-turbo",
    "EmbeddingModel": "text-embedding-ada-002"
  },
  "ReportServer": {
    "Address": "localhost:1099"
  }
}
```

## Troubleshooting

### JNI Bridge Issues

- Ensure JAVA_HOME is set correctly and points to a Java 17+ JDK
- Check that the ReportServerJniBridge.jar file is correctly built and placed in the MCPServer/lib directory
- Look for JNI-related errors in the logs, which might indicate issues with the JVM setup or method signatures

### ReportServer Connection Issues

- Verify that your Java ReportServer is running and accessible at the configured address
- Check RMI registry connectivity from the ReportServerJniBridge
- Use the checkHealth method to verify the connection to the ReportServer

### MCP Server Issues

- Ensure your OpenAI API key is correctly set
- Check the logs for any errors related to the Semantic Kernel configuration
- Verify that the gRPC endpoints are properly configured and accessible
```

## Extending the Project

### Adding More Report Types

You can extend the report capabilities by:

1. Updating the ReportServerInterface.java with new methods
2. Implementing those methods in your ReportServer
3. Updating the ReportServerClient.cs to support the new methods

### Supporting Other AI Models

The project is set up to use OpenAI models by default, but you can modify it to use other models:

1. Update the Program.cs file to use a different model provider
2. Add the necessary packages for your preferred AI model

## Troubleshooting

- If you encounter JNI issues, make sure your JAVA_HOME is set correctly
- For ReportServer connection issues, verify the RMI registry is running and accessible
- If Java classes aren't found, ensure the JAR file is correctly built and placed in the lib directory

## License

[MIT License](LICENSE)

## Further Resources

- [gRPC in .NET](https://docs.microsoft.com/aspnet/core/grpc)
- [Semantic Kernel Documentation](https://github.com/microsoft/semantic-kernel)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/llms-full.txt)
- [Java gRPC Documentation](https://grpc.io/docs/languages/java/)
