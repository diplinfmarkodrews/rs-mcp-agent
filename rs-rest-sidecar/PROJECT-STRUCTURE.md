# ReportServer Java Sidecar - Project Structure

## Overview

This Java Spring Boot sidecar application bridges .NET applications with ReportServer's GWT RPC endpoints by exposing them as REST APIs.

## Directory Structure

```
java-rs-sidecar/
├── src/
│   ├── main/
│   │   ├── java/
│   │   │   └── net/
│   │   │       └── datenwerke/
│   │   │           └── rs/
│   │   │               └── sidecar/
│   │   │                   ├── ReportServerSidecarApplication.java  # Main Spring Boot app
│   │   │                   ├── config/
│   │   │                   │   └── OpenApiConfig.java              # Swagger configuration
│   │   │                   ├── controller/
│   │   │                   │   └── ReportServerController.java     # REST endpoints
│   │   │                   ├── model/
│   │   │                   │   └── dto/                            # Data Transfer Objects
│   │   │                   │       ├── AuthenticationRequest.java
│   │   │                   │       ├── AuthenticationResponse.java
│   │   │                   │       ├── UserInfo.java
│   │   │                   │       ├── SecurityViewInformation.java
│   │   │                   │       ├── SecureeInfo.java
│   │   │                   │       ├── AccessControlList.java
│   │   │                   │       └── AccessControlEntry.java
│   │   │                   └── service/
│   │   │                       └── ReportServerGwtRpcService.java  # GWT RPC client
│   │   └── resources/
│   │       └── application.properties                              # Configuration
│   └── test/
│       └── java/
│           └── net/
│               └── datenwerke/
│                   └── rs/
│                       └── sidecar/
│                           └── controller/
│                               └── ReportServerControllerTest.java  # Unit tests
├── examples/
│   └── dotnet-integration.md                                       # .NET usage examples
├── target/                                                         # Build output
setup
├── start-sidecar.sh                                               # Startup script
├── pom.xml                                                         # Maven configuration
└── README.md                                                       # Documentation
```

## Key Components

### 1. Main Application (`ReportServerSidecarApplication.java`)
- Spring Boot entry point
- Configures RestTemplate for HTTP communication
- Simple, focused configuration

### 2. REST Controller (`ReportServerController.java`)
- Exposes authentication endpoints
- Exposes security management endpoints
- Handles session management
- Provides health check endpoint

### 3. GWT RPC Service (`ReportServerGwtRpcService.java`)
- Handles communication with ReportServer's GWT RPC endpoints
- Formats GWT RPC requests
- Parses GWT RPC responses
- Manages session cookies

### 4. Data Transfer Objects (DTOs)
- `AuthenticationRequest/Response` - Login/logout operations
- `UserInfo` - User details
- `SecurityViewInformation` - Security permissions
- `SecureeInfo` - Security target information
- `AccessControlList/Entry` - Permission structures

### 5. Configuration
- `application.properties` - Runtime configuration
- `OpenApiConfig.java` - Swagger documentation setup

## REST API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/reportserver/auth/login` | Authenticate user |
| GET | `/api/reportserver/auth/check` | Check session validity |
| POST | `/api/reportserver/auth/logout` | Logout user |
| GET | `/api/reportserver/security/view/{nodeId}` | Get security information |
| GET | `/api/reportserver/health` | Health check |

## Build and Run

### Local Development
```bash
mvn spring-boot:run
```

### Production Build
```bash
mvn clean package
java -jar target/java-rs-sidecar-1.0.0.jar
```

### Docker
```bash
mvn package -DskipTests
docker build -t reportserver-sidecar .
docker run -p 8080:8080 -e REPORTSERVER_BASE_URL=http://your-rs:8084/reportserver reportserver-sidecar
```

### Docker Compose
```bash
docker-compose up
```

## Configuration Options

| Property | Default | Description |
|----------|---------|-------------|
| `server.port` | 8080 | HTTP port for sidecar |
| `reportserver.base-url` | http://localhost:8084/reportserver | ReportServer URL |
| `reportserver.timeout.connect` | 30000 | Connection timeout (ms) |
| `reportserver.timeout.read` | 60000 | Read timeout (ms) |

## Extension Points

To add new ReportServer RPC endpoints:

1. **Add DTOs** in `model.dto` package for request/response objects
2. **Extend Service** in `ReportServerGwtRpcService` with new GWT RPC calls
3. **Add Endpoints** in `ReportServerController` with new REST mappings
4. **Update Documentation** in README and OpenAPI config

### Example Extension - User Management

```java
// 1. Add DTO
public class CreateUserRequest {
    private String username;
    private String password;
    // getters/setters
}

// 2. Extend service
public UserInfo createUser(CreateUserRequest request, String sessionId) {
    // GWT RPC call to UserManagerRpcService.createUser()
}

// 3. Add endpoint
@PostMapping("/users")
public ResponseEntity<UserInfo> createUser(@RequestBody CreateUserRequest request) {
    // Implementation
}
```

## Security Considerations

- The sidecar acts as a proxy - ReportServer handles authentication
- Session IDs should be transmitted securely (HTTPS)
- Consider implementing rate limiting for production use
- Network access should be restricted between sidecar and ReportServer

## Monitoring and Logging

- Health check endpoint: `/api/reportserver/health`
- Spring Boot Actuator can be added for detailed metrics
- Logs include authentication attempts and errors
- Configurable log levels in `application.properties`

## Future Roadmap

This initial implementation focuses on authentication and basic security operations. Future versions will include:

- Complete user management operations
- Report execution and management
- Dashboard operations
- File system operations
- Complete ReportServer RPC interface coverage
