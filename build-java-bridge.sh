#!/bin/bash

# Build script for the ReportServer JNI Bridge

# Ensure JAVA_HOME is set
if [ -z "$JAVA_HOME" ]; then
  echo "Error: JAVA_HOME environment variable is not set."
  echo "Please set JAVA_HOME to your Java installation directory."
  exit 1
fi

# Build the Java client
echo "Building Java client..."
cd JavaClient
mvn clean package

# Create lib directory in MCPServer if it doesn't exist
echo "Creating lib directory in MCPServer..."
mkdir -p ../MCPServer/lib

# Copy the JAR file to the MCPServer lib directory
echo "Copying JAR to MCPServer/lib..."
cp target/mcp-client-1.0-SNAPSHOT-jar-with-dependencies.jar ../MCPServer/lib/ReportServerJniBridge.jar

echo "Build completed successfully!"
