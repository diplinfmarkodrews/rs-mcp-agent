# Clean Authentication Architecture Implementation

## Overview

This document describes the clean authentication architecture for the RsMcpServer system, which operates as an **independent service** supporting multiple authentication mechanisms. Authentication is handled by an ASP.NET policy scheme (Keycloak JWT bearer or legacy session) plus `AuthenticatedSessionMiddleware`.

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

```
Request -> UseSession -> UseAuthentication (policy scheme "MultiAuth")
              Authorization: Bearer a.b.c  -> JwtBearer (Keycloak)
              Authorization: Bearer <GUID> -> LegacyTokenAuthenticationHandler (ISessionStore)
              no header                    -> Cookie (legacy login cookie)
        -> UseAuthorization
        -> AuthenticatedSessionMiddleware
              Legacy  : ReportServerSession.CurrentJsessionId  = claim "JSESSIONID"
              Keycloak: ReportServerSession.CurrentBearerToken = JWT, CurrentBearerKey = "sub"
        -> MCP endpoint (anonymous, so tools/list works without login)
        -> RsGwtRpcClientBase: Keycloak: "Authorization: Bearer <JWT>" + "Cookie: JSESSIONID" (issued by the ReportServer on the
           first call, remembered per Keycloak "sub"); Legacy: "Cookie: JSESSIONID=<id>" -> ReportServer
```

- `KeycloakAuthenticationExtensions`: registers the policy scheme, JwtBearer (`Keycloak:Authority`,
  optional `Keycloak:Audience`, `Keycloak:RequireHttpsMetadata`), the legacy token handler and the cookie.
- `LegacyTokenAuthenticationHandler`: validates GUID tokens issued by `/api/auth/v1/login`.
- `AuthenticatedSessionMiddleware`: hands the caller's credentials to the RPC client per request.
- `ReportServerSession`: `AsyncLocal` carrying either the JSESSIONID or the Keycloak bearer token.

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
4. When calling RsMcpServer, the Keycloak bearer token is forwarded to the ReportServer
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
4. **Unified Interface**: the policy scheme selects the handler per request
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
5. RsMcpServer calls work seamlessly with the forwarded bearer token
6. ReportServer operations use the caller's credentials

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
