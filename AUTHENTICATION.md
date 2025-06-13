# Enterprise Keycloak Authentication System

This document describes the sophisticated Keycloak authentication system implemented across **RsMcpServer.Web**, **RSChatApp.Web**, and **ReportServer** integration. The system provides centralized authentication, session management, and seamless integration between modern .NET applications and legacy Java systems.

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Keycloak Realm                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐          │
│  │  reportserver   │  │   Web Clients   │  │   API Clients   │          │
│  │     -app        │  │   (OIDC Flow)   │  │ (Direct Grant)  │          │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘          │
└─────────────────────────────────────────────────────────────────────────┘
                                   │
                     ┌─────────────┼─────────────┐
                     │             │             │
        ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
        │   RSChatApp     │ │ RsMcpServer.Web │ │   ReportServer  │
        │     .Web        │ │  (MCP Server)   │ │   (Java/GWT)    │
        │ ┌─────────────┐ │ │ ┌─────────────┐ │ │ ┌─────────────┐ │
        │ │ OIDC Client │ │ │ │ OIDC Client │ │ │ │   Session   │ │
        │ │  + Session  │ │ │ │  + Session  │ │ │ │   Bridge    │ │
        │ │   Bridge    │ │ │ │   Bridge    │ │ │ │             │ │
        │ └─────────────┘ │ │ └─────────────┘ │ │ └─────────────┘ │
        └─────────────────┘ └─────────────────┘ └─────────────────┘
                     │             │             │
                     └─────────────┼─────────────┘
                                   │
                        ┌─────────────────┐
                        │ Shared Identity │
                        │ Infrastructure  │
                        │ ┌─────────────┐ │
                        │ │   Service   │ │
                        │ │  Defaults   │ │
                        │ └─────────────┘ │
                        └─────────────────┘
```

## 🎯 Overview

The authentication system provides:

- **🔐 Centralized Keycloak OIDC Authentication** with modern security practices
- **🔗 Seamless ReportServer Integration** through session bridging
- **📚 Shared Authentication Infrastructure** across all web applications
- **🔄 Token Management** with automatic refresh capabilities
- **⚡ Session Management** with cross-system synchronization
- **🛡️ Enhanced Security** with PKCE, secure cookies, and proper middleware

## 🏗️ Core Components

### 1. **RsMcpServer.Identity** - Authentication Infrastructure

**Location:** `/RsMcpServer.Identity/`

#### **Core Services:**

- **`IKeycloakAuthenticationService`** - Main Keycloak integration service
- **`IReportServerAuthenticationService`** - ReportServer session bridge
- **`ITokenManagementService`** - Token storage and refresh management
- **`ISessionBridgeService`** - Cross-system session synchronization

#### **Models & Options:**

- **`KeycloakOptions`** - Keycloak configuration settings
- **`ReportServerOptions`** - ReportServer integration settings
- **`AuthenticationResult`** - Authentication operation results
- **`UserInfo`** - User information from Keycloak
- **`TokenResponse`** - Token response handling
- **`SessionBridgeResult`** - Session bridging results

#### **Middleware:**

- **`AuthenticationSessionMiddleware`** - Session management and synchronization

### 2. **Authentication Extensions**

#### **KeycloakAuthenticationExtensions**

Provides seamless integration through extension methods:

```csharp
// In Program.cs
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);

// In application pipeline
app.UseKeycloakAuthentication();
```

## ⚙️ Configuration

### 1. **Keycloak Realm Setup**

**Create Realm:**

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

**Client Configuration:**

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

### 2. **Application Configuration**

#### **Development Settings** (`appsettings.Development.json`):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "RsMcpServer.Identity": "Debug",
      "Microsoft.AspNetCore.Authentication": "Debug"
    }
  },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/reportserver",
    "ClientId": "reportserver-app",
    "ClientSecret": "",
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
  }
}
```

#### **Production Settings** (`appsettings.json`):

```json
{
  "Keycloak": {
    "Authority": "https://keycloak.yourdomain.com/realms/reportserver",
    "ClientId": "reportserver-app",
    "ClientSecret": "${KEYCLOAK_CLIENT_SECRET}",
    "Realm": "reportserver",
    "Scopes": [
      "openid",
      "profile",
      "email",
      "roles",
      "reportserver",
      "chatapp"
    ],
    "RequireHttpsMetadata": true,
    "TokenRefreshThreshold": "00:05:00"
  },
  "ReportServer": {
    "Address": "https://reportserver.yourdomain.com",
    "SessionTimeout": "01:00:00",
    "CookieDomain": "yourdomain.com",
    "EnableSessionBridge": true
  }
}
```

## 🔗 Authentication Endpoints

The system automatically provides these endpoints:

### **Direct Authentication**
- **POST** `/auth/login` - Direct login with username/password
- **POST** `/auth/logout` - Logout from all systems
- **POST** `/auth/refresh` - Refresh authentication tokens

### **OIDC Flow**
- **GET** `/auth/challenge` - Initiate OIDC authentication flow
- **GET** `/auth/userinfo` - Get current user information
- **GET** `/auth/signin-oidc` - OIDC callback endpoint

### **Legacy Support**
- The original `/rs-authenticate` endpoint is replaced with `/auth/login`

## 🔧 Integration Examples

### 1. **RsMcpServer.Web Integration**

```csharp
// Program.cs
using RsMcpServer.Identity.Extensions;
using RsMcpServer.Identity.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Keycloak authentication with enhanced features
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<ISessionBridgeService, SessionBridgeService>();

var app = builder.Build();

// Use authentication with session management
app.UseKeycloakAuthentication();

// Protect MCP endpoints
app.MapMcp()
    .RequireAuthorization();
```

### 2. **RSChatApp.Web Integration**

```csharp
// Program.cs
using RsMcpServer.Identity.Extensions;
using RsMcpServer.Identity.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Keycloak authentication
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);

var app = builder.Build();

// Use authentication with session management
app.UseAuthentication();
app.UseSession();
app.UseAuthenticationSession();

// Protect Blazor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();
```

### 3. **Using Authentication in Controllers/Services**

```csharp
[ApiController]
[Authorize]
public class ProtectedController : ControllerBase
{
    private readonly IKeycloakAuthenticationService _authService;
    private readonly ISessionBridgeService _sessionBridge;
    
    public ProtectedController(
        IKeycloakAuthenticationService authService,
        ISessionBridgeService sessionBridge)
    {
        _authService = authService;
        _sessionBridge = sessionBridge;
    }
    
    [HttpGet("userinfo")]
    public async Task<IActionResult> GetUserInfo()
    {
        var user = await _authService.GetCurrentUserAsync();
        return Ok(user);
    }
    
    [HttpPost("bridge-session")]
    public async Task<IActionResult> CreateSessionBridge()
    {
        var result = await _sessionBridge.CreateBridgeAsync(
            User, HttpContext.Request.Cookies);
        return Ok(result);
    }
}
```

### 4. **Using Authentication in Blazor Components**

```csharp
@using Microsoft.AspNetCore.Authorization
@using RsMcpServer.Identity.Services
@attribute [Authorize]
@inject IKeycloakAuthenticationService AuthService

<div class="user-info">
    <h3>Welcome, @user?.FullName</h3>
    <p>Email: @user?.Email</p>
    <p>Roles: @string.Join(", ", user?.Roles ?? new List<string>())</p>
</div>

@code {
    private UserInfo? user;
    
    protected override async Task OnInitializedAsync()
    {
        user = await AuthService.GetCurrentUserAsync();
    }
}
```

## 🔒 Authorization Policies

The system provides pre-configured authorization policies:

```csharp
services.AddAuthorizationBuilder()
    .AddPolicy("AuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser())
    .AddPolicy("ReportServerUser", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("scope", "reportserver"))
    .AddPolicy("ReportServerAdmin", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("rs-admin"))
    .AddPolicy("ChatAppUser", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("scope", "chatapp"))
    .AddPolicy("McpServerAccess", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
                  context.User.HasClaim("scope", "reportserver") ||
                  context.User.IsInRole("mcp-user")));
```

## 🛡️ Security Features

### 1. **Enhanced OIDC Security**
- **PKCE (Proof Key for Code Exchange)** enabled by default
- **Secure cookie configuration** with HttpOnly, Secure, and SameSite settings
- **Token validation** with proper JWT handling
- **HTTPS enforcement** in production environments
- **Anti-forgery tokens** for state protection

### 2. **Token Management**
- **Automatic token refresh** before expiration
- **Secure token storage** in encrypted sessions
- **Token introspection** and validation
- **Graceful token expiry handling**
- **Background refresh** for long-running sessions

### 3. **Session Management**
- **Cross-system session synchronization**
- **Session timeout management**
- **Proper session cleanup** on logout
- **Session bridging** between Keycloak and ReportServer
- **Concurrent session handling**

### 4. **ReportServer Integration**
- **Seamless authentication bridging** from Keycloak to ReportServer
- **Cookie management** for GWT RPC calls
- **Session validation** and refresh
- **Legacy authentication support**
- **Automatic credential mapping**

## 🔄 Session Bridge Architecture

### **How Session Bridging Works**

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   User Login    │    │ Keycloak Token  │    │ ReportServer    │
│   (Keycloak)    │───►│   Validation    │───►│   Session       │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │              ┌─────────────────┐              │
         └─────────────►│ Session Bridge  │◄─────────────┘
                        │    Service      │
                        └─────────────────┘
                                 │
                        ┌─────────────────┐
                        │ Unified Session │
                        │    Context      │
                        └─────────────────┘
```

### **Session Bridge Implementation**

```csharp
public class SessionBridgeService : ISessionBridgeService
{
    public async Task<SessionBridgeResult> CreateBridgeAsync(
        ClaimsPrincipal keycloakUser, 
        IRequestCookieCollection cookies)
    {
        // 1. Extract user information from Keycloak claims
        var userInfo = ExtractUserInfo(keycloakUser);
        
        // 2. Create ReportServer session
        var rsSession = await CreateReportServerSessionAsync(userInfo);
        
        // 3. Store session mapping
        await StoreBridgeSessionAsync(keycloakUser.GetSessionId(), rsSession);
        
        // 4. Return bridge result
        return new SessionBridgeResult
        {
            Success = true,
            ReportServerSessionId = rsSession.SessionId,
            Cookies = rsSession.Cookies
        };
    }
}
```

## 📊 Monitoring and Logging

### **Comprehensive Logging Configuration**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "RsMcpServer.Identity": "Information",
      "Microsoft.AspNetCore.Authentication": "Warning",
      "Microsoft.AspNetCore.Authentication.OpenIdConnect": "Debug"
    }
  }
}
```

### **Log Categories:**

- **Authentication events** - Login, logout, token refresh
- **Session management** - Session creation, expiry, synchronization
- **ReportServer integration** - Session bridging, validation
- **Error handling** - Authentication failures, token issues
- **Security events** - Failed login attempts, unauthorized access

### **Structured Logging Examples**

```csharp
// Authentication success
logger.LogInformation("User {Username} authenticated successfully from {IpAddress}", 
    username, httpContext.Connection.RemoteIpAddress);

// Session bridge creation
logger.LogInformation("Session bridge created for user {UserId} with ReportServer session {SessionId}",
    userId, reportServerSessionId);

// Token refresh
logger.LogDebug("Token refreshed for user {Username}, expires at {ExpiryTime}",
    username, tokenExpiry);
```

## 🚀 Deployment Considerations

### 1. **Production Security Settings**

```json
{
  "Keycloak": {
    "RequireHttpsMetadata": true,
    "TokenRefreshThreshold": "00:02:00"
  },
  "Authentication": {
    "Cookie": {
      "SecurePolicy": "Always",
      "SameSite": "Strict",
      "HttpOnly": true
    }
  }
}
```

### 2. **Environment Variables for Secrets**

```bash
# Production environment variables
export KEYCLOAK__CLIENTSECRET="your-production-secret"
export KEYCLOAK__AUTHORITY="https://prod-keycloak.yourdomain.com/realms/reportserver"
export REPORTSERVER__ADDRESS="https://prod-reportserver.yourdomain.com"

# Development environment variables
export KEYCLOAK__REQUIREHTTPSMETADATA="false"
export ASPNETCORE_ENVIRONMENT="Development"
```

### 3. **Docker Deployment Configuration**

```dockerfile
# Dockerfile for authentication-enabled applications
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "YourApp.dll"]
```

### 4. **Kubernetes Configuration**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: rsmcp-server
spec:
  replicas: 3
  selector:
    matchLabels:
      app: rsmcp-server
  template:
    spec:
      containers:
      - name: rsmcp-server
        image: rsmcp-server:latest
        env:
        - name: Keycloak__Authority
          value: "https://keycloak.yourdomain.com/realms/reportserver"
        - name: Keycloak__ClientSecret
          valueFrom:
            secretKeyRef:
              name: keycloak-secrets
              key: client-secret
        ports:
        - containerPort: 80
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
```

## 🏥 Health Checks

### **Authentication Health Check Implementation**

```csharp
public class KeycloakHealthCheck : IHealthCheck
{
    private readonly IKeycloakAuthenticationService _authService;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check Keycloak connectivity
            var httpClient = _httpClientFactory.CreateClient("keycloak");
            var response = await httpClient.GetAsync("/.well-known/openid_configuration", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Keycloak is accessible");
            }
            
            return HealthCheckResult.Degraded($"Keycloak returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Keycloak is not accessible", ex);
        }
    }
}
```

### **Registration in Program.cs**

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<KeycloakHealthCheck>("keycloak")
    .AddCheck<ReportServerHealthCheck>("reportserver")
    .AddCheck<SessionBridgeHealthCheck>("session-bridge");
```

## 🔧 Troubleshooting

### **Common Issues**

#### **1. "Token refresh failed"**
```bash
# Check Keycloak logs
docker logs keycloak-container

# Verify configuration
curl -X GET "http://localhost:8080/realms/reportserver/.well-known/openid_configuration"

# Test token endpoint
curl -X POST "http://localhost:8080/realms/reportserver/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=reportserver-app&client_secret=YOUR_SECRET"
```

#### **2. "ReportServer session bridge failed"**
```bash
# Check ReportServer connectivity
curl -X GET "http://localhost:8081/reportserver/health"

# Verify session bridge configuration
curl -X POST "http://localhost:5002/auth/bridge-session" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### **3. "OIDC configuration not found"**
```bash
# Verify Keycloak authority URL
curl -X GET "http://localhost:8080/realms/reportserver/.well-known/openid_configuration"

# Check network connectivity
ping localhost
telnet localhost 8080
```

### **Debug Configuration**

```json
{
  "Logging": {
    "LogLevel": {
      "RsMcpServer.Identity": "Debug",
      "Microsoft.AspNetCore.Authentication": "Debug",
      "Microsoft.AspNetCore.Authentication.OpenIdConnect": "Trace",
      "Microsoft.AspNetCore.Authentication.Cookies": "Debug"
    }
  }
}
```

### **Diagnostic Endpoints**

```csharp
// Add diagnostic endpoints in development
if (app.Environment.IsDevelopment())
{
    app.MapGet("/auth/debug", async (HttpContext context, IKeycloakAuthenticationService authService) =>
    {
        var user = await authService.GetCurrentUserAsync();
        return Results.Ok(new
        {
            IsAuthenticated = context.User.Identity?.IsAuthenticated,
            Claims = context.User.Claims.Select(c => new { c.Type, c.Value }),
            User = user
        });
    });
}
```

## 🔮 Future Enhancements

### **Phase 1: Enhanced Security** 🚧
- [x] PKCE implementation
- [x] Secure cookie configuration
- [ ] Certificate-based authentication
- [ ] Hardware security module (HSM) integration

### **Phase 2: Multi-Tenancy** 📋
- [ ] Multi-realm support
- [ ] Tenant-specific configurations
- [ ] Cross-tenant user management
- [ ] Tenant isolation and security

### **Phase 3: Advanced Features** 🔮
- [ ] Single Sign-Out (SLO) implementation
- [ ] Social identity provider integration
- [ ] Advanced audit logging
- [ ] Real-time session monitoring

### **Phase 4: Enterprise Integration** 🏢
- [ ] Active Directory integration
- [ ] SAML 2.0 support
- [ ] Custom claim transformations
- [ ] Enterprise policy enforcement

## 📞 Support

### **For Authentication Issues:**

1. **Check logs** for detailed error information in the application and Keycloak
2. **Verify configuration** settings match your Keycloak setup
3. **Test endpoints** directly using curl or Postman
4. **Review middleware pipeline** order in your application
5. **Validate certificates** and HTTPS configuration in production

### **Getting Help:**

- 📚 Check the [Keycloak Documentation](https://www.keycloak.org/documentation)
- 🔍 Review the [Microsoft Authentication Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/)
- 🐛 Open an issue in the project repository
- 💬 Join the community discussions

---

**🔐 Enterprise-Grade Security Made Simple**

This authentication system is designed to be production-ready with enterprise-grade security features. Always test thoroughly in your specific environment before deployment.
