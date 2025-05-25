#!/bin/bash

# Simple test to verify gRPC server is working without OpenAI dependency

echo "Testing gRPC Health Check using grpcurl..."

# Check if grpcurl is available
if ! command -v grpcurl &> /dev/null; then
    echo "grpcurl not found, installing via go..."
    if command -v go &> /dev/null; then
        go install github.com/fullstorydev/grpcurl/cmd/grpcurl@latest
        export PATH=$PATH:$(go env GOPATH)/bin
    else
        echo "Neither grpcurl nor go found. Please install grpcurl manually:"
        echo "https://github.com/fullstorydev/grpcurl"
        exit 1
    fi
fi

echo "Testing gRPC reflection endpoint..."
grpcurl -plaintext localhost:5000 list

echo ""
echo "Testing health check..."
grpcurl -plaintext -d '{"service": "mcp"}' localhost:5000 mcp.MCPService/HealthCheck
