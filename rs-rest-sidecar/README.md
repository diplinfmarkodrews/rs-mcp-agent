# ReportServer Java Sidecar

This Java Spring Boot application serves as a bridge between .NET applications and ReportServer's GWT RPC endpoints, exposing them as REST APIs.

## Overview

The sidecar provides:
- REST API wrapper for ReportServer's authentication system
- REST API wrapper for ReportServer's security management
- Session management and cookie handling
- JSON serialization/deserialization for .NET compatibility
- OpenAPI/Swagger documentation

## Architecture

```
.NET Application <-> HTTP REST API <-> Java Sidecar <-> GWT RPC <-> ReportServer
```

## Features

### Authentication
- **POST** `/api/reportserver/auth/login` - Authenticate user
- **GET** `/api/reportserver/auth/check` - Check session validity
- **POST** `/api/reportserver/auth/logout` - Logout user

### Security Management
- **GET** `/api/reportserver/security/view/{nodeId}` - Load security view information

### Health Check
- **GET** `/api/reportserver/health` - Service health check

## Configuration

Edit `application.properties` to configure:

```properties
# ReportServer Configuration
reportserver.base-url=http://localhost:8084/reportserver
reportserver.timeout.connect=30000
reportserver.timeout.read=60000

# Server Configuration
server.port=8080
```

## Usage

### Authentication Example

```bash
# Login
curl -X POST http://localhost:8080/api/reportserver/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'

# Response
{
  "success": true,
  "sessionId": "ABC123...",
  "user": {
    "username": "admin",
    "firstname": "Admin",
    "lastname": "User",
    "email": "admin@example.com",
    "active": true,
    "superUser": true
  }
}

# Check session
curl -X GET "http://localhost:8080/api/reportserver/auth/check?sessionId=ABC123..."

# Logout
curl -X POST "http://localhost:8080/api/reportserver/auth/logout?sessionId=ABC123..."
```

### .NET Integration Example

```csharp
public class ReportServerClient
{
    private readonly HttpClient _httpClient;
    private string _sessionId;

    public ReportServerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var request = new { username, password };
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:8080/api/reportserver/auth/login", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
            _sessionId = result.SessionId;
            return result.Success;
        }
        
        return false;
    }

    public async Task<SecurityViewInformation> GetSecurityInfoAsync(long nodeId)
    {
        var response = await _httpClient.GetAsync(
            $"http://localhost:8080/api/reportserver/security/view/{nodeId}?sessionId={_sessionId}");
        
        return await response.Content.ReadFromJsonAsync<SecurityViewInformation>();
    }
}
```

## Building and Running

### Prerequisites
- Java 11 or later
- Maven 3.6 or later
- ReportServer instance running

### Build
```bash
mvn clean package
```

### Run
```bash
java -jar target/java-rs-sidecar-1.0.0.jar
```

Or use Maven:
```bash
mvn spring-boot:run
```

## API Documentation

When running, access the Swagger UI at:
- http://localhost:8080/swagger-ui.html

OpenAPI specification available at:
- http://localhost:8080/v3/api-docs

## Extending the Sidecar

The sidecar is designed to be easily extended with additional ReportServer RPC endpoints:

1. Add new DTOs in `model.dto` package
2. Extend `ReportServerGwtRpcService` with new GWT RPC calls
3. Add new REST endpoints in the controller
4. Update documentation

## Security Considerations

- The sidecar runs as a bridge service and should be secured appropriately
- Consider running behind a reverse proxy with SSL termination
- Implement rate limiting for production use
- Session IDs are sensitive and should be handled securely

## Future Extensions

This sidecar will be extended to support:
- User management operations
- Report execution
- Report management
- Dashboard operations
- Full ReportServer RPC interface

## Troubleshooting

### Common Issues

1. **Connection refused**: Check that ReportServer is running at the configured URL
2. **Authentication failed**: Verify username/password and ReportServer configuration
3. **Session timeout**: Implement session refresh logic in your .NET application

### Logging

Increase logging verbosity in `application.properties`:
```properties
logging.level.net.datenwerke.rs.sidecar=DEBUG
logging.level.org.apache.http=DEBUG
```
