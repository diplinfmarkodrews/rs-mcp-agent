# Enterprise MCP Server for ReportServer Integration

A sophisticated **Model Context Protocol (MCP)** server implementation that provides AI-powered integration with Java-based ReportServer application. Built with .NET 9.0, this system leverages Microsoft's latest technologies for cloud-native application development and enterprise-grade authentication.

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          RSChatApp.Web (Browser-Based Workspace)               │
│                                   (Blazor UI)                                  │
│                                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────────────┐ │
│  │     Ollama      │    │     Qdrant      │    │    Session Management      │ │
│  │   (AI/LLM)      │    │   (VectorDB)    │    │                             │ │
│  │                 │    │                 │    │  Current: In-Memory         │ │
│  │ • Chat Response │    │ • Vector Search │    │  • Browser Session         │ │
│  │ • Code Analysis │    │ • Embeddings    │    │  • Conversation Context    │ │
│  └─────────────────┘    │ • Semantic RAG  │    │                             │ │
│                         └─────────────────┘    │  Future: Persistent         │ │
│                                 ▲              │  • Topic-Based History     │ │
│                    Knowledge     │              │  • Cross-Session Context   │ │
│                    Base          │              └─────────────────────────────┘ │
│                    Ingestion     │                                              │
│  ┌─────────────────────────────────────────────┐                              │
│  │           Ingested Content                   │                              │
│  │                                             │                              │
│  │  📚 Documentation    🔧 Groovy Scripts     │                              │
│  │  • PDFs, Markdown   • .groovy files        │                              │
│  │  • API Docs         • Build scripts        │                              │
│  │  • User Manuals     • Automation scripts   │                              │
│  │                                             │                              │
│  │  💻 Terminal Commands                       │                              │
│  │  • CLI usage examples                      │                              │
│  │  • Command syntax                          │                              │
│  │  • Shell scripts                           │                              │
│  └─────────────────────────────────────────────┘                              │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      │ MCP Protocol
                                      ▼
                    ┌─────────────────────────────────────────┐
                    │         RsMcpServer.Web                 │
                    │         (MCP Server)                    │
                    │                                         │
                    │  Current Implementation:                │
                    │  ┌─────────────────────────────────┐    │
                    │  │     Basic Terminal Tool         │    │
                    │  │   • Command execution           │    │
                    │  │   • Process management          │    │
                    │  └─────────────────────────────────┘    │
                    │                                         │
                    │  Future Extensions:                     │
                    │  ┌─────────────────────────────────┐    │
                    │  │ • Advanced Report Tool          │    │
                    │  │ • File Management Tool          │    │
                    │  │ • Database Query Tool           │    │
                    │  │ • Workflow Automation Tool      │    │
                    │  └─────────────────────────────────┘    │
                    └─────────────────────────────────────────┘
                                      │
                                      │ RPC/HTTP
                                      ▼
                    ┌─────────────────────────────────┐
                    │      ReportServer               │
                    │      (Java/GWT)                 │
                    └─────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Authentication Layer                        │
│                                                                 │
│    RSChatApp.Web ◄──────► Keycloak ◄──────► RsMcpServer.Web    │
│                           (OIDC)                                │
│                                                                 │
│                     ReportServer ◄─────────► Keycloak          │
│                     (Session Bridge)                           │
└─────────────────────────────────────────────────────────────────┘
```

### 📖 Architecture Description

The RSChatApp operates as a **browser-based workspace** that provides an intelligent chat interface powered by AI and enhanced with semantic search capabilities:

**🌐 Browser-Based Workspace (RSChatApp.Web)**
- **Session Management**: Currently maintains conversation context in browser memory for immediate responsiveness
- **Interactive Chat Interface**: Real-time Blazor UI for seamless user interaction with AI models
- **Future Evolution**: Plans for persistent sessions with topic-based conversation history and cross-session context retention

**🧠 Knowledge Base Integration**
The system ingests diverse content types into Qdrant vector database for intelligent retrieval:
- **📚 Documentation**: PDFs, Markdown files, API documentation, and user manuals
- **🔧 Groovy Scripts**: Build scripts, automation scripts, and custom .groovy files  
- **💻 Terminal Commands**: CLI usage examples, command syntax references, and shell scripts

**🤖 AI-Powered Intelligence**
- **Ollama**: Handles chat responses and code analysis using local LLM models
- **Qdrant**: Provides vector search, embeddings, and semantic RAG capabilities for context-aware responses

**🔧 MCP Server Evolution (RsMcpServer.Web)**
- **Current State**: Implements basic terminal tool for command execution and process management
- **Future Roadmap**: Extensible architecture planned for advanced report tools, file management, database queries, and workflow automation

**🔐 Enterprise Authentication**
Centralized Keycloak OIDC authentication ensures secure access across all components with session bridging to ReportServer for seamless enterprise integration.

## 🚀 Key Features

### **Enterprise Authentication & Security**
- ✅ **Centralized Keycloak OIDC Authentication** with PKCE support
- ✅ **Seamless ReportServer Integration** through session bridging
- ✅ **JWT Token Management** with automatic refresh
- ✅ **Cross-System Session Synchronization**
- ✅ **Role-Based Access Control (RBAC)**

### **AI-Powered Chat Interface**
- ✅ **Modern Blazor Web UI** with real-time chat capabilities
- ✅ **Ollama Integration** for local LLM inference
- ✅ **Qdrant Vector Database** for semantic search and RAG
- ✅ **Document Ingestion Pipeline** with PDF support
- ✅ **Semantic Search** across ingested documents

### **MCP Server Integration**
- ✅ **Microsoft Extensions AI Framework** for MCP protocol
- ✅ **Direct ReportServer RPC Client** for Java interoperability
- ✅ **Tool Integration** for AI agent functionality
- ✅ **Terminal Operations** support for ReportServer CLI
- ✅ **HTTP & SSE Transport** protocols

### **Cloud-Native Deployment**
- ✅ **.NET Aspire Orchestration** for microservices
- ✅ **Docker Containerization** with persistent volumes
- ✅ **Health Checks & Monitoring** with OpenTelemetry
- ✅ **Service Discovery** and load balancing
- ✅ **Configuration Management** with environment-specific settings

## 🚀 Quick Start

### **1. Using .NET Aspire (Recommended)**

Start the entire application stack with one command:

```bash
# Navigate to the Aspire host directory
cd RSChatApp.AppHost

# Start all services (Ollama with auto-downloaded models, Qdrant, MCP Server, Web App)
dotnet run
```

This will automatically:
- ✅ Start **Ollama** in Docker with GPU support (if available)
- ✅ Pull and configure required AI models automatically (configurable in appsettings.json)
- ✅ Start **Qdrant** vector database in Docker with persistent storage
- ✅ Launch the **MCP Server** with authentication
- ✅ Start the **Blazor Web Application**
- ✅ Open the **Aspire Dashboard** for monitoring

**Access Points:**
- 📱 **Chat Application**: `http://localhost:5123` (or as shown in Aspire dashboard)
- 🔧 **Aspire Dashboard**: `http://localhost:15986`
- 🤖 **MCP Server API**: `http://localhost:5002`
- 📊 **Qdrant Dashboard**: `http://localhost:6333/dashboard`

**Note:** The first run may take a few minutes as Docker images are downloaded and AI models are pulled automatically.

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
- Docker Desktop (for all containerized services)
- Java JDK 17 or later (for ReportServer - if running locally)
- Keycloak 22+ (for authentication - can be run via Docker)

**Note:** Ollama, Qdrant, and AI models are automatically managed by the .NET Aspire AppHost via Docker containers - no manual installation required!

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

## ⚙️ Configuration

### **Keycloak Authentication Setup**

The system uses Keycloak for centralized authentication across all components. Here's how to set it up:

#### **1. Keycloak Installation & Setup**

**Using Docker (Recommended for Development):**

```bash
# Start Keycloak with Docker
docker run -d \
  --name keycloak \
  -p 8080:8080 \
  -e KEYCLOAK_ADMIN=admin \
  -e KEYCLOAK_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:22.0 \
  start-dev
```

**Access Keycloak Admin Console:**
- URL: `http://localhost:8080`
- Username: `admin`
- Password: `admin`

#### **2. Realm Configuration**

Create a new realm called `reportserver`:

1. **Create Realm:**
   - Go to Administration Console
   - Click "Create Realm"
   - Name: `reportserver`
   - Display Name: `ReportServer Realm`
   - Enable: `true`

2. **Realm Settings:**
   ```json
   {
     "realm": "reportserver",
     "enabled": true,
     "displayName": "ReportServer Realm",
     "registrationAllowed": false,
     "resetPasswordAllowed": true,
     "editUsernameAllowed": false,
     "bruteForceProtected": true
   }
   ```

#### **3. Client Configuration**

Create a client for the MCP Server applications:

1. **Client Settings:**
   - **Client ID:** `reportserver-app`
   - **Client Name:** `ReportServer Application`
   - **Protocol:** `openid-connect`
   - **Client Authentication:** `ON` (Confidential)
   - **Standard Flow:** `ON`
   - **Direct Access Grants:** `ON`

2. **Client Configuration JSON:**
   ```json
   {
     "clientId": "reportserver-app",
     "name": "ReportServer Application",
     "description": "MCP Server and Chat Application Client",
     "enabled": true,
     "protocol": "openid-connect",
     "publicClient": false,
     "standardFlowEnabled": true,
     "implicitFlowEnabled": false,
     "directAccessGrantsEnabled": true,
     "serviceAccountsEnabled": false,
     "authorizationServicesEnabled": false,
     "redirectUris": [
       "http://localhost:5123/*",
       "http://localhost:5002/*",
       "https://yourdomain.com/signin-oidc"
     ],
     "webOrigins": ["+"],
     "attributes": {
       "pkce.code.challenge.method": "S256"
     }
   }
   ```

3. **Valid Redirect URIs:**
   - `http://localhost:5123/*` (RSChatApp.Web)
   - `http://localhost:5002/*` (RsMcpServer.Web)
   - `https://yourdomain.com/signin-oidc` (Production)

#### **4. User Management**

1. **Create Users:**
   - Go to Users → Add user
   - Set username, email, first name, last name
   - Enable user and set temporary password

2. **Assign Roles:**
   - Create roles: `rs-admin`, `rs-user`, `mcp-user`
   - Assign appropriate roles to users

### **ReportServer Setup**

ReportServer is a Java-based reporting platform that integrates with the MCP system.

#### **1. Installation Options**

**Option A: Bitnami Stack (Recommended for Quick Start)**
```bash
# Download from: https://bitnami.com/stack/reportserver
wget https://downloads.bitnami.com/files/stacks/reportserver/[version]/bitnami-reportserver-[version]-linux-x64-installer.run
chmod +x bitnami-reportserver-[version]-linux-x64-installer.run
sudo ./bitnami-reportserver-[version]-linux-x64-installer.run
```

**Option B: Manual WAR Deployment**
```bash
# Build from source
git clone https://github.com/infofabrik/reportserver.git
cd reportserver
mvn clean install

# Deploy to Tomcat
cp target/reportserver.war $TOMCAT_HOME/webapps/
```

**Option C: Docker Deployment**
```bash
# Using custom Docker image (if available)
docker run -d \
  --name reportserver \
  -p 8081:8080 \
  -e DB_HOST=localhost \
  -e DB_PORT=3306 \
  -e DB_NAME=reportserver \
  -e DB_USER=reportserver \
  -e DB_PASS=password \
  reportserver:latest
```

#### **2. ReportServer Configuration**

**Database Setup:**
```properties
# hibernate.properties
hibernate.connection.driver_class=com.mysql.cj.jdbc.Driver
hibernate.connection.url=jdbc:mysql://localhost:3306/reportserver?useSSL=false
hibernate.connection.username=reportserver
hibernate.connection.password=password
hibernate.dialect=org.hibernate.dialect.MySQL8Dialect
```

**Keycloak Integration:**
Configure ReportServer to use Keycloak for authentication by updating the security configuration.

#### **3. Access Points**

- **Web Interface:** `http://localhost:8081/reportserver`
- **REST API:** `http://localhost:8081/reportserver/api`
- **RPC Interface:** Available via HTTP for MCP integration

### **Application Configuration (appsettings.json)**

#### **RSChatApp.Web Configuration**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "RSChatApp.ServiceDefaults.Authentication": "Information"
    }
  },
  "AllowedHosts": "*",
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/reportserver",
    "ClientId": "reportserver-app",
    "ClientSecret": "your-client-secret-here",
    "Realm": "reportserver",
    "Scopes": [
      "openid",
      "profile",
      "email",
      "roles"
    ],
    "RequireHttpsMetadata": false,
    "TokenRefreshThreshold": "00:05:00"
  },
  "ReportServer": {
    "Address": "http://localhost:8081",
    "SessionTimeout": "01:00:00",
    "CookieDomain": "localhost",
    "EnableSessionBridge": true
  },
  "Ollama": {
    "Address": "http://0.0.0.0:11434",
    "Model": "mistral-nemo:12b",
    "EmbeddingModel": "llama3.2:1b",
    "MaxTokens": 4096,
    "Temperature": 0.7
  },
  "Qdrant": {
    "Address": "http://localhost:6334"
  },
  "RsMcpServer": {
    "Address": "http://localhost:5002"
  }
}
```

#### **RsMcpServer.Web Configuration**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "RSChatApp.ServiceDefaults.Authentication": "Information"
    }
  },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/reportserver",
    "ClientId": "reportserver-app",
    "ClientSecret": "your-client-secret-here",
    "Realm": "reportserver",
    "Scopes": [
      "openid",
      "profile",
      "email",
      "roles"
    ],
    "RequireHttpsMetadata": false,
    "TokenRefreshThreshold": "00:05:00"
  },
  "ReportServer": {
    "Address": "http://localhost:8081/",
    "SessionTimeout": "01:00:00",
    "CookieDomain": "localhost",
    "EnableSessionBridge": true
  }
}
```

#### **Configuration Parameters Explained**

**Keycloak Settings:**
- `Authority`: Keycloak realm URL
- `ClientId`: Client identifier in Keycloak
- `ClientSecret`: Client secret (get from Keycloak admin console)
- `Realm`: Keycloak realm name
- `Scopes`: OpenID Connect scopes to request
- `RequireHttpsMetadata`: Set to `false` for development, `true` for production
- `TokenRefreshThreshold`: Time before token expiry to refresh

**ReportServer Settings:**
- `Address`: ReportServer base URL
- `SessionTimeout`: Session timeout duration
- `CookieDomain`: Domain for session cookies
- `EnableSessionBridge`: Enable session bridging between Keycloak and ReportServer

**Ollama Settings:**
- `Address`: Ollama server URL
- `Model`: Chat completion model
- `EmbeddingModel`: Text embedding model
- `MaxTokens`: Maximum tokens per response
- `Temperature`: Response randomness (0.0-1.0)

**Qdrant Settings:**
- `Address`: Qdrant vector database URL

#### **Environment-Specific Configuration**

**Development Environment:**
```json
{
  "Keycloak": {
    "RequireHttpsMetadata": false,
    "Authority": "http://localhost:8080/realms/reportserver"
  },
  "ReportServer": {
    "Address": "http://localhost:8081"
  }
}
```

**Production Environment:**
```json
{
  "Keycloak": {
    "RequireHttpsMetadata": true,
    "Authority": "https://keycloak.yourdomain.com/realms/reportserver",
    "ClientSecret": "${KEYCLOAK_CLIENT_SECRET}"
  },
  "ReportServer": {
    "Address": "https://reportserver.yourdomain.com"
  }
}
```

### **2. Alternative: Manual Service Setup**

**⚠️ Note:** We recommend using the .NET Aspire approach above as it automatically handles Docker containers, model downloads, and service orchestration. However, if you need to run services individually:

#### **Prerequisites for Manual Setup**
- Docker Desktop running
- .NET 9.0 SDK
- All services will still use Docker containers (managed manually)

#### **Start Required Services**

**1. Start Keycloak (Authentication)**
```bash
docker run -d --name keycloak -p 8080:8080 \
  -e KEYCLOAK_ADMIN=admin \
  -e KEYCLOAK_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:22.0 start-dev
```

**2. Start Qdrant (Vector Database)**
```bash
docker run -d --name qdrant -p 6333:6333 -p 6334:6334 \
  -v qdrant_storage:/qdrant/storage \
  qdrant/qdrant:latest
```

**3. Start Ollama (AI Models)**
```bash
# Start Ollama in Docker with GPU support
docker run -d --gpus=all -v ollama:/root/.ollama -p 11434:11434 --name ollama ollama/ollama

# Pull required models
docker exec ollama ollama pull mistral-nemo:12b
docker exec ollama ollama pull llama3.2:1b
```

**4. Start ReportServer (Optional - if available)**
```bash
# Assuming Bitnami installation
sudo /opt/bitnami/reportserver/ctlscript.sh start
```

**5. Start MCP Server**
```bash
cd RsMcpServer.Web
dotnet run
```

**6. Start Chat Application**
```bash
cd RSChatApp.Web
dotnet run
```

**Note:** Manual setup requires you to configure all the networking and dependencies yourself. The Aspire approach handles all of this automatically with proper service discovery and health checks.
