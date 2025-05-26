#!/bin/bash

# End-to-end test script for MCP Server with Java RPC Client
# This script will orchestrate all four components:
# 1. ReportServerMock (Java-based mock server)
# 2. MCPServerSDK (Microsoft.Extensions.AI-based MCP server with JNI integration)
# 3. MCPClientSDK (Interactive console client for MCP communication)
# 4. MCPChatClient (gRPC-based chat client with web interface)

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Directory paths
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
JAVA_CLIENT_DIR="$ROOT_DIR/JavaClient"
MCP_SERVER_DIR="$ROOT_DIR/MCPServerSDK"
MCP_CLIENT_DIR="$ROOT_DIR/MCPClientSDK"
MCP_CHAT_CLIENT_DIR="$ROOT_DIR/MCPChatClient"

# Process IDs to kill later
REPORT_SERVER_PID=""
MCP_SERVER_PID=""
MCP_CHAT_CLIENT_PID=""

# Function to clean up processes on exit
cleanup() {
    echo -e "${YELLOW}Cleaning up...${NC}"
    
    if [ ! -z "$REPORT_SERVER_PID" ]; then
        echo "Stopping mock ReportServer (PID: $REPORT_SERVER_PID)"
        kill $REPORT_SERVER_PID 2>/dev/null || true
    fi
    
    if [ ! -z "$MCP_SERVER_PID" ]; then
        echo "Stopping MCP Server SDK (PID: $MCP_SERVER_PID)"
        kill $MCP_SERVER_PID 2>/dev/null || true
    fi
    
    if [ ! -z "$MCP_CHAT_CLIENT_PID" ]; then
        echo "Stopping MCP Chat Client (PID: $MCP_CHAT_CLIENT_PID)"
        kill $MCP_CHAT_CLIENT_PID 2>/dev/null || true
    fi
    
    # Kill any remaining processes
    pkill -f "MockReportServer" 2>/dev/null || true
    pkill -f "MCPServerSDK" 2>/dev/null || true
    pkill -f "MCPChatClient" 2>/dev/null || true
    
    echo -e "${GREEN}Cleanup completed${NC}"
}

# Register the cleanup function to run on exit
trap cleanup EXIT

# Function to wait for a service to be ready
wait_for_service() {
    local service_name=$1
    local port=$2
    local max_attempts=30
    local attempt=1
    
    echo "Waiting for $service_name to be ready on port $port..."
    
    while [ $attempt -le $max_attempts ]; do
        if nc -z localhost $port 2>/dev/null; then
            echo -e "${GREEN}$service_name is ready!${NC}"
            return 0
        fi
        echo "Attempt $attempt/$max_attempts: $service_name not ready yet..."
        sleep 2
        attempt=$((attempt + 1))
    done
    
    echo -e "${RED}Error: $service_name failed to start within timeout${NC}"
    return 1
}

# Function to display menu and get user choice
show_menu() {
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}  MCP Server End-to-End Test Suite     ${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo ""
    echo "All components are now running:"
    echo "  ✅ ReportServerMock (Java)"
    echo "  ✅ MCPServerSDK (.NET with JNI integration)"
    echo "  ✅ MCPChatClient (Web interface)"
    echo ""
    echo "Available test options:"
    echo "  1) Run MCPClientSDK (Interactive console client)"
    echo "  2) Open MCPChatClient web interface"
    echo "  3) Show component status"
    echo "  4) View logs"
    echo "  5) Exit and cleanup"
    echo ""
    read -p "Enter your choice (1-5): " choice
    echo ""
}

# Check prerequisites
if [ -z "$JAVA_HOME" ]; then
    echo -e "${RED}Error: JAVA_HOME environment variable is not set${NC}"
    echo "Please set JAVA_HOME to your Java installation directory"
    exit 1
fi

if ! command -v nc &> /dev/null; then
    echo -e "${YELLOW}Warning: netcat (nc) is not installed${NC}"
    echo "Service readiness checks will be skipped"
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
echo -e "${GREEN}Step 2: Starting ReportServerMock (Java)...${NC}"
cd $JAVA_CLIENT_DIR
mvn exec:java -Dexec.mainClass="com.example.reportserver.MockReportServer" > "$ROOT_DIR/reportserver.log" 2>&1 &
REPORT_SERVER_PID=$!
echo "ReportServerMock started with PID: $REPORT_SERVER_PID"

# Wait for the ReportServer to start (it typically runs on port 1099 for RMI)
sleep 5

# Step 3: Start the MCPServerSDK
echo -e "${GREEN}Step 3: Starting MCPServerSDK (.NET with JNI integration)...${NC}"
cd $MCP_SERVER_DIR
dotnet run > "$ROOT_DIR/mcpserver.log" 2>&1 &
MCP_SERVER_PID=$!
echo "MCPServerSDK started with PID: $MCP_SERVER_PID"

# Wait for the MCP Server to start (give it more time to initialize JNI bridge)
echo "Waiting for MCPServerSDK to initialize..."
sleep 15

# Step 4: Start the MCPChatClient (web interface)
echo -e "${GREEN}Step 4: Starting MCPChatClient (Web interface)...${NC}"
cd $MCP_CHAT_CLIENT_DIR
dotnet run --web > "$ROOT_DIR/mcpchatclient.log" 2>&1 &
MCP_CHAT_CLIENT_PID=$!
echo "MCPChatClient started with PID: $MCP_CHAT_CLIENT_PID"

# Wait for the Chat Client to start
echo "Waiting for MCPChatClient to initialize..."
sleep 10

echo -e "${GREEN}All components started successfully!${NC}"
echo ""

# Interactive menu loop
while true; do
    show_menu
    
    case $choice in
        1)
            echo -e "${GREEN}Starting MCPClientSDK (Interactive console client)...${NC}"
            cd $MCP_CLIENT_DIR
            dotnet build --nologo -q
            echo "Running interactive MCP client..."
            echo "Press Ctrl+C to return to menu"
            echo ""
            dotnet run
            echo ""
            ;;
        2)
            echo -e "${GREEN}Opening MCPChatClient web interface...${NC}"
            echo "Web interface should be available at: http://localhost:5001"
            echo "Opening in default browser..."
            if command -v xdg-open &> /dev/null; then
                xdg-open http://localhost:5001
            elif command -v open &> /dev/null; then
                open http://localhost:5001
            else
                echo "Please manually open: http://localhost:5001"
            fi
            echo ""
            read -p "Press Enter to continue..."
            ;;
        3)
            echo -e "${GREEN}Component Status:${NC}"
            if kill -0 $REPORT_SERVER_PID 2>/dev/null; then
                echo "  ✅ ReportServerMock (PID: $REPORT_SERVER_PID) - Running"
            else
                echo "  ❌ ReportServerMock - Not running"
            fi
            
            if kill -0 $MCP_SERVER_PID 2>/dev/null; then
                echo "  ✅ MCPServerSDK (PID: $MCP_SERVER_PID) - Running"
            else
                echo "  ❌ MCPServerSDK - Not running"
            fi
            
            if kill -0 $MCP_CHAT_CLIENT_PID 2>/dev/null; then
                echo "  ✅ MCPChatClient (PID: $MCP_CHAT_CLIENT_PID) - Running"
            else
                echo "  ❌ MCPChatClient - Not running"
            fi
            echo ""
            read -p "Press Enter to continue..."
            ;;
        4)
            echo -e "${GREEN}Recent logs:${NC}"
            echo ""
            echo -e "${YELLOW}--- ReportServerMock logs ---${NC}"
            tail -n 10 "$ROOT_DIR/reportserver.log" 2>/dev/null || echo "No logs available"
            echo ""
            echo -e "${YELLOW}--- MCPServerSDK logs ---${NC}"
            tail -n 10 "$ROOT_DIR/mcpserver.log" 2>/dev/null || echo "No logs available"
            echo ""
            echo -e "${YELLOW}--- MCPChatClient logs ---${NC}"
            tail -n 10 "$ROOT_DIR/mcpchatclient.log" 2>/dev/null || echo "No logs available"
            echo ""
            read -p "Press Enter to continue..."
            ;;
        5)
            echo -e "${GREEN}Exiting and cleaning up...${NC}"
            exit 0
            ;;
        *)
            echo -e "${RED}Invalid choice. Please enter 1-5.${NC}"
            ;;
    esac
done

# Cleanup will happen automatically thanks to the trap
