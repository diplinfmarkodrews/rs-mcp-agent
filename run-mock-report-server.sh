#!/bin/bash

# Script to run the mock ReportServer for testing

# Ensure JAVA_HOME is set
if [ -z "$JAVA_HOME" ]; then
  echo "Error: JAVA_HOME environment variable is not set."
  echo "Please set JAVA_HOME to your Java installation directory."
  exit 1
fi

# Check if the client has been built
if [ ! -f ./JavaClient/target/mcp-client-1.0-SNAPSHOT-jar-with-dependencies.jar ]; then
  echo "Java client has not been built yet. Building now..."
  ./build-java-bridge.sh
fi

# Run the mock ReportServer
echo "Starting mock ReportServer..."
cd JavaClient
mvn exec:java -Dexec.mainClass="com.example.reportserver.MockReportServer"

# The server will continue to run until manually stopped with Ctrl+C
