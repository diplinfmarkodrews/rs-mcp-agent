#!/bin/bash

# End-to-end test script for MCP Server with Java RPC Client
# This script will:
# 1. Build the Java JNI bridge
# 2. Start the mock ReportServer
# 3. Start the MCP Server
# 4. Run test queries against the MCP Server
# 5. Clean up all processes

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Directory paths
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
JAVA_CLIENT_DIR="$ROOT_DIR/JavaClient"
MCP_SERVER_DIR="$ROOT_DIR/MCPServer"
TEST_CLIENT_DIR="$ROOT_DIR/MCPServer.TestClient"

# Process IDs to kill later
REPORT_SERVER_PID=""
MCP_SERVER_PID=""

# Function to clean up processes on exit
cleanup() {
    echo -e "${YELLOW}Cleaning up...${NC}"
    
    if [ ! -z "$REPORT_SERVER_PID" ]; then
        echo "Stopping mock ReportServer (PID: $REPORT_SERVER_PID)"
        kill $REPORT_SERVER_PID 2>/dev/null || true
    fi
    
    if [ ! -z "$MCP_SERVER_PID" ]; then
        echo "Stopping MCP Server (PID: $MCP_SERVER_PID)"
        kill $MCP_SERVER_PID 2>/dev/null || true
    fi
    
    echo -e "${GREEN}Cleanup completed${NC}"
}

# Register the cleanup function to run on exit
trap cleanup EXIT

# Check prerequisites
if [ -z "$JAVA_HOME" ]; then
    echo -e "${RED}Error: JAVA_HOME environment variable is not set${NC}"
    echo "Please set JAVA_HOME to your Java installation directory"
    exit 1
fi

if [ -z "$OpenAI__ApiKey" ]; then
    echo -e "${YELLOW}Warning: OpenAI__ApiKey environment variable is not set${NC}"
    echo "The MCP Server will run, but AI model interactions will fail"
    echo "Set this variable to test with a real OpenAI API key:"
    echo "export OpenAI__ApiKey=your-api-key"
    echo ""
fi

# Step 1: Build the Java JNI bridge
echo -e "${GREEN}Step 1: Building Java JNI bridge...${NC}"
$ROOT_DIR/build-java-bridge.sh

# Step 2: Start the mock ReportServer
echo -e "${GREEN}Step 2: Starting mock ReportServer...${NC}"
cd $JAVA_CLIENT_DIR
mvn exec:java -Dexec.mainClass="com.example.reportserver.MockReportServer" &
REPORT_SERVER_PID=$!
echo "Mock ReportServer started with PID: $REPORT_SERVER_PID"

# Wait for the ReportServer to start
echo "Waiting for ReportServer to initialize..."
sleep 5

# Step 3: Start the MCP Server
echo -e "${GREEN}Step 3: Starting MCP Server...${NC}"
cd $MCP_SERVER_DIR
dotnet run &
MCP_SERVER_PID=$!
echo "MCP Server started with PID: $MCP_SERVER_PID"

# Wait for the MCP Server to start
echo "Waiting for MCP Server to initialize..."
sleep 10

# Step 4: Build and run the test client
echo -e "${GREEN}Step 4: Building and running test client...${NC}"
cd $TEST_CLIENT_DIR
dotnet build
echo "Running test client..."
dotnet run

# Wait for user to review results
echo ""
echo -e "${GREEN}Tests completed. Press Enter to exit and clean up processes...${NC}"
read

# Cleanup will happen automatically thanks to the trap
