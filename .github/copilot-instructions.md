<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# MCP Server with Java RPC Client

This workspace contains a .NET implementation of a Model Context Protocol (MCP) server with a Java client that communicates via gRPC.

## Project Structure

- **/MCPServer/**: Contains the .NET MCP server implementation
  - **Program.cs**: Main entry point for the server
  - **Services/MCPServiceImpl.cs**: Implementation of the MCP service
  - **Protos/mcp.proto**: Protocol Buffer definition for the MCP service
  
- **/JavaClient/**: Contains the Java client implementation
  - **src/main/java/com/example/mcpclient/MCPClient.java**: Java client for MCP server
  - **src/main/proto/mcp.proto**: Copy of the Protocol Buffer definition

## Key Technologies

- .NET 9.0 for the server
- gRPC for communication
- Protocol Buffers for message serialization
- Microsoft Semantic Kernel for AI capabilities
- Java 17 for the client
- Maven for Java dependency management

## Common Tasks

### Extending the MCP Service

To add new methods to the MCP service:
1. Update the service definition in mcp.proto
2. Implement the new methods in MCPServiceImpl.cs
3. Update the Java client to use the new methods

### Customizing the AI Model

The MCP server is configured to use OpenAI by default. To change this:
1. Modify the Kernel configuration in Program.cs
2. Update the appsettings.json with appropriate settings

### Working with Protocol Buffers

When modifying the .proto files, remember to:
1. Keep the proto files in sync between the server and client
2. Regenerate the client code after changes (using Maven for Java)

You can find more info and examples about Model Context Protocol at https://modelcontextprotocol.io/llms-full.txt
