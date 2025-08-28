#!/bin/bash

# ReportServer Java Sidecar Startup Script

set -e

echo "Starting ReportServer Java Sidecar..."

# Default configuration
JAVA_OPTS="${JAVA_OPTS:--Xmx512m -Xms256m}"
SERVER_PORT="${SERVER_PORT:-8091}"
REPORTSERVER_URL="${REPORTSERVER_URL:-http://localhost:8090}"

# Build the application if jar doesn't exist
if [ ! -f "target/rs-rest-sidecar-1.0.0.jar" ]; then
    echo "Building application..."
    mvn clean package -DskipTests
fi

# Start the application
echo "Starting Java Sidecar on port $SERVER_PORT"
echo "Connecting to ReportServer at $REPORTSERVER_URL"

exec java $JAVA_OPTS \
    -Dserver.port=$SERVER_PORT \
    -Dreportserver.base-url=$REPORTSERVER_URL \
    -jar target/rs-rest-sidecar-1.0.0.jar \
    "$@"
