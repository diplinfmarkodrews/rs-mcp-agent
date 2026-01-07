# Clean Authentication Architecture Implementation

## Overview

This document describes the clean authentication architecture for the RsMcpServer system, which operates as an **independent service** supporting multiple authentication mechanisms. The architecture uses `ISessionBridgeService` and `AuthenticatedSessionMiddleware` as key components to provide unified authentication handling.

## Architecture Principles

### RsMcpServer as Independent Service
- **Self-Contained**: RsMcpServer operates independently with its own authentication system
- **Multi-Client Support**: Supports various client types (web apps, VS Code extensions, direct API access)
- **Dual Authentication**: Handles both Legacy ReportServer and modern Keycloak authentication
- **Unified Interface**: Provides consistent authentication experience regardless of the underlying mechanism

### Client Flexibility
- **RSChatApp**: Uses both Legacy and Keycloak authentication based on user preference and availability
- **VS Code Extensions**: Primarily uses Legacy authentication for direct access
- **Other Clients**: Can choose the most appropriate authentication method

## Key Components

### 1. `AuthenticationContext`
A unified model representing the complete authentication state:
- `IsAuthenticated`: Whether the user is authenticated
- `Type`: Authentication type (None, Legacy, Keycloak)
- `SessionId`: Appropriate session ID based on auth type
- `AuthenticationToken`: Authentication token (GUID for Legacy, JWT for Keycloak)
- `User`: Claims principal with user information
- `Properties`: Additional authentication properties

### 2. `ISessionBridgeService`
The unified gateway for all authentication concerns:

```csharp
public interface ISessionBridgeService
{
    // Primary method - gets complete authentication context
    Task<AuthenticationContext> GetAuthenticationContextAsync();
    
    // Convenience methods
    Task<string?> GetAuthenticationTokenAsync();
    Task<string?> GetSessionIdAsync();
    Task<ClaimsPrincipal?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    Task ClearSessionAsync();
}
```

### 3. `AuthenticatedSessionMiddleware`
Simplified middleware that delegates to `ISessionBridgeService`:

```csharp
public async Task InvokeAsync(HttpContext context, ISessionBridgeService sessionBridge)
{
    if (ShouldSkipAuthentication(context)) 
    {
        await _next(context);
        return;
    }

    var authContext = await sessionBridge.GetAuthenticationContextAsync();
    
    if (authContext.IsAuthenticated && authContext.User != null)
    {
        context.User = authContext.User;
    }

    await _next(context);
}
```

## Authentication Flow

### RSChatApp Dual Authentication Strategy

**RSChatApp** implements a flexible authentication approach that supports both mechanisms:

1. **Primary: Keycloak Authentication** (Modern Web Flow)
   - User initiates authentication through Keycloak OIDC
   - Receives JWT tokens and establishes web session
   - Automatically bridges to ReportServer session when needed
   - Preferred method for modern web-based interactions

2. **Fallback: Legacy Authentication** (Direct ReportServer Access)
   - Used when Keycloak is unavailable or user prefers direct access
   - Authenticates directly against ReportServer with username/password
   - Receives GUID token for subsequent API calls
   - Provides backward compatibility and offline scenarios

3. **Intelligent Selection**
   - RSChatApp attempts Keycloak authentication first
   - Falls back to Legacy authentication if needed
   - User can manually choose authentication method
   - Seamless switching between methods during session

### Authentication Flows by Client Type

#### 1. RSChatApp Web Application (Dual Mode)

**Keycloak Flow (Primary):**
1. RSChatApp initiates Keycloak OIDC authentication
2. User authenticates through Keycloak interface
3. RSChatApp receives JWT tokens and establishes web session
4. When calling RsMcpServer, session bridge ensures ReportServer token availability
5. RsMcpServer middleware validates existing user context from Keycloak

**Legacy Flow (Fallback):**
1. RSChatApp presents login form for direct ReportServer authentication
2. Credentials sent to RsMcpServer `/api/auth/v1/login` endpoint
3. RsMcpServer validates against ReportServer and returns GUID token
4. RSChatApp stores token and includes it in subsequent RsMcpServer requests
5. RsMcpServer middleware validates GUID token and sets user context

#### 2. VS Code Extensions (Legacy Primary)

1. Extension prompts user for ReportServer credentials
2. Authenticates via RsMcpServer `/api/auth/v1/login` endpoint
3. Receives GUID token and stores securely
4. Includes token in `Authorization: Bearer <GUID>` header for all requests
5. RsMcpServer middleware validates token and provides API access

#### 3. Direct API Access (Legacy Only)

1. Client application authenticates via `/api/auth/v1/login`
2. Receives GUID token for session management
3. Uses token for all subsequent API calls
4. RsMcpServer provides full API access based on ReportServer permissions

## RsMcpServer Internal Architecture

### Unified Authentication Detection

The `SessionBridgeService.GetAuthenticationContextAsync()` method provides intelligent authentication detection:

1. **Legacy Token Detection**: 
   - Checks `Authorization` header for GUID tokens
   - Validates via `IAuthenticationService.ValidateTokenAsync()`
   - Returns `AuthenticationContext.Legacy()` with ReportServer session ID

2. **Keycloak Context Detection**:
   - Detects existing authenticated `HttpContext.User` from Keycloak pipeline
   - Retrieves ASP.NET Core session ID and access token
   - Returns `AuthenticationContext.Keycloak()` with web session ID

3. **Unified Processing**:
   - Middleware uses single entry point regardless of authentication type
   - Sets `context.User` appropriately for downstream services
   - Provides consistent session and token access

## Session ID Resolution Strategy

The `GetSessionIdAsync()` method intelligently returns the appropriate session ID based on authentication context:

- **Legacy Authentication**: Returns ReportServer session ID (JSESSIONID) from user claims
  - Used for tracking ReportServer operations and session lifecycle
  - Essential for maintaining server-side session state
  
- **Keycloak Authentication**: Returns ASP.NET Core session ID for request correlation
  - Used for web application session tracking
  - Enables request correlation and logging across the web stack
  
- **Unauthenticated Requests**: Returns ASP.NET Core session ID if available
  - Allows session tracking for anonymous operations
  - Useful for analytics and request correlation

## Client Integration Patterns

### RSChatApp Integration Strategy

RSChatApp implements a sophisticated client-side authentication strategy:

```csharp
// Pseudo-code for RSChatApp authentication logic
public class AuthenticationService
{
    public async Task<AuthResult> AuthenticateAsync()
    {
        // Try Keycloak first (modern approach)
        var keycloakResult = await TryKeycloakAuthenticationAsync();
        if (keycloakResult.Success)
        {
            return keycloakResult;
        }
        
        // Fallback to Legacy authentication
        var legacyResult = await TryLegacyAuthenticationAsync();
        return legacyResult;
    }
    
    private async Task<AuthResult> TryKeycloakAuthenticationAsync()
    {
        // Attempt OIDC flow with Keycloak
        // Establishes web session and JWT tokens
    }
    
    private async Task<AuthResult> TryLegacyAuthenticationAsync()
    {
        // Direct authentication with RsMcpServer
        // POST to /api/auth/v1/login
        // Receive and store GUID token
    }
}
```

### Benefits of This Architecture

1. **Independent Service Design**: RsMcpServer operates autonomously with its own authentication system
2. **Client Flexibility**: Each client can choose the most appropriate authentication method
3. **Graceful Degradation**: RSChatApp can fall back from Keycloak to Legacy authentication seamlessly
4. **Unified Interface**: `ISessionBridgeService` provides consistent authentication access regardless of mechanism
5. **Future Proof**: Easy to add new authentication providers (OAuth2, SAML, etc.)
6. **Backward Compatibility**: Existing Legacy authentication clients continue to work unchanged
7. **Modern Standards**: Supports contemporary OIDC/JWT authentication patterns
8. **Session Consistency**: Intelligent session ID resolution maintains proper context for each auth type

## Real-World Usage Scenarios

### Scenario 1: RSChatApp User with Keycloak Available
1. User opens RSChatApp
2. App attempts Keycloak authentication (modern, secure)
3. User authenticates through Keycloak interface
4. App receives JWT tokens and establishes session
5. RsMcpServer calls work seamlessly through session bridge
6. ReportServer operations use bridged authentication context

### Scenario 2: RSChatApp User with Keycloak Unavailable
1. User opens RSChatApp
2. Keycloak authentication fails (server down, network issues)
3. App automatically falls back to Legacy authentication
4. User enters ReportServer credentials
5. App receives GUID token from RsMcpServer
6. All operations continue normally with Legacy authentication

### Scenario 3: VS Code Extension User
1. Extension prompts for ReportServer credentials
2. Authenticates directly via RsMcpServer Legacy endpoints
3. Receives GUID token for session management
4. All extension features work with full ReportServer integration
5. No dependency on external authentication providers

### Scenario 4: Automated API Client
1. Service authenticates via `/api/auth/v1/login`
2. Receives GUID token for programmatic access
3. Includes token in all API requests
4. Full programmatic access to RsMcpServer functionality
5. Perfect for CI/CD pipelines and automated reporting

## Usage Examples

### Getting Authentication Context
```csharp
var authContext = await sessionBridge.GetAuthenticationContextAsync();

if (authContext.IsAuthenticated)
{
    var sessionId = authContext.SessionId;
    var token = authContext.AuthenticationToken;
    var user = authContext.User;
    
    Console.WriteLine($"User {user.Identity.Name} authenticated via {authContext.Type}");
}
```

### Getting Session ID (Context-Aware)
```csharp
// Returns the appropriate session ID based on authentication type
var sessionId = await sessionBridge.GetSessionIdAsync();

// For Legacy auth: ReportServer session ID (JSESSIONID)
// For Keycloak auth: ASP.NET Core session ID
// For unauthenticated: ASP.NET Core session ID (if available)
```

### Getting Authentication Token
```csharp
// Returns the appropriate token based on authentication type
var token = await sessionBridge.GetAuthenticationTokenAsync();

// For Legacy auth: GUID token
// For Keycloak auth: JWT access token
```
