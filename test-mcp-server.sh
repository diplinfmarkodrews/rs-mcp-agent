#!/bin/bash

# Test script for the MCP Server with Java RPC Client

# Set environment variables
export JAVA_HOME=/path/to/your/jdk
export OpenAI__ApiKey="your-openai-api-key"

# Build the Java JNI bridge
echo "Building Java JNI bridge..."
./build-java-bridge.sh

# Start a mock ReportServer for testing (if available)
echo "Starting mock ReportServer..."
# Uncomment if you have a mock ReportServer implementation
# cd JavaClient
# mvn exec:java -Dexec.mainClass="com.example.reportserver.MockReportServer"
# cd ..

# Start the MCP Server
echo "Starting MCP Server..."
cd MCPServer
dotnet run &
MCP_SERVER_PID=$!

# Wait for the server to start
echo "Waiting for MCP Server to start..."
sleep 5

# Test health check endpoint using grpcurl (if installed)
if command -v grpcurl >/dev/null 2>&1; then
    echo "Testing MCP Server health check..."
    grpcurl -plaintext -proto ./Protos/mcp.proto localhost:5000 mcp.MCPService/HealthCheck
else
    echo "grpcurl not found. Install it to test the gRPC endpoints directly."
fi

# Test chat endpoint using a simple request
if command -v grpcurl >/dev/null 2>&1; then
    echo "Testing MCP Server chat endpoint..."
    grpcurl -plaintext -proto ./Protos/mcp.proto -d '{"messages":[{"role":1,"content":"What reports are available?"}],"model":"gpt-3.5-turbo","temperature":0.7}' localhost:5000 mcp.MCPService/Chat
fi

# Clean up
echo "Cleaning up..."
kill $MCP_SERVER_PID

echo "Test completed."
