package net.datenwerke.rs.sidecar.service;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;
import org.springframework.http.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import net.datenwerke.rs.sidecar.model.dto.*;

import java.util.regex.Pattern;
import java.util.regex.Matcher;

/**
 * Service for communicating with ReportServer's GWT RPC endpoints
 */
@Service
public class ReportServerGwtRpcService {
    
    private static final Logger logger = LoggerFactory.getLogger(ReportServerGwtRpcService.class);
    
    @Value("${reportserver.base-url}")
    private String reportServerBaseUrl;
    
    private final RestTemplate restTemplate;
    
    public ReportServerGwtRpcService(RestTemplate restTemplate) {
        this.restTemplate = restTemplate;
    }
    
    /**
     * Authenticate user with ReportServer
     */
    public AuthenticationResponse authenticate(AuthenticationRequest request) {
        try {
            String url = reportServerBaseUrl + "/login";
            
            // Create GWT RPC payload for authentication
            String payload = buildAuthenticationPayload(request.getUsername(), request.getPassword());
            
            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.TEXT_PLAIN);
            headers.set("X-GWT-Module-Base", reportServerBaseUrl + "/");
            headers.set("X-GWT-Permutation", "PermutationNotUsedByServer");
            
            HttpEntity<String> entity = new HttpEntity<>(payload, headers);
            
            ResponseEntity<String> response = restTemplate.postForEntity(url, entity, String.class);
            
            if (response.getStatusCode().is2xxSuccessful()) {
                return parseAuthenticationResponse(response.getBody(), response.getHeaders());
            } else {
                return new AuthenticationResponse(false, "Authentication failed with status: " + response.getStatusCode());
            }
            
        } catch (Exception e) {
            logger.error("Error during authentication", e);
            return new AuthenticationResponse(false, "Authentication error: " + e.getMessage());
        }
    }
    
    /**
     * Check if user session is still valid
     */
    public AuthenticationResponse checkAuthentication(String sessionId) {
        try {
            String url = reportServerBaseUrl + "/login";
            
            // Create GWT RPC payload for session check
            String payload = buildSessionCheckPayload();
            
            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.TEXT_PLAIN);
            headers.set("X-GWT-Module-Base", reportServerBaseUrl + "/");
            headers.set("X-GWT-Permutation", "PermutationNotUsedByServer");
            
            if (sessionId != null) {
                headers.set("Cookie", "JSESSIONID=" + sessionId);
            }
            
            HttpEntity<String> entity = new HttpEntity<>(payload, headers);
            
            ResponseEntity<String> response = restTemplate.postForEntity(url, entity, String.class);
            
            if (response.getStatusCode().is2xxSuccessful()) {
                return parseSessionCheckResponse(response.getBody(), response.getHeaders());
            } else {
                return new AuthenticationResponse(false, "Session check failed");
            }
            
        } catch (Exception e) {
            logger.error("Error during session check", e);
            return new AuthenticationResponse(false, "Session check error: " + e.getMessage());
        }
    }
    
    /**
     * Logout user from ReportServer
     */
    public void logout(String sessionId) {
        try {
            String url = reportServerBaseUrl + "/login";
            
            String payload = buildLogoutPayload();
            
            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.TEXT_PLAIN);
            headers.set("X-GWT-Module-Base", reportServerBaseUrl + "/");
            headers.set("X-GWT-Permutation", "PermutationNotUsedByServer");
            
            if (sessionId != null) {
                headers.set("Cookie", "JSESSIONID=" + sessionId);
            }
            
            HttpEntity<String> entity = new HttpEntity<>(payload, headers);
            
            restTemplate.postForEntity(url, entity, String.class);
            
        } catch (Exception e) {
            logger.error("Error during logout", e);
        }
    }
    
    /**
     * Load security view information for a node
     */
    public SecurityViewInformation loadSecurityViewInformation(Long nodeId, String sessionId) {
        try {
            String url = reportServerBaseUrl + "/security_security";
            
            String payload = buildSecurityViewPayload(nodeId);
            
            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.TEXT_PLAIN);
            headers.set("X-GWT-Module-Base", reportServerBaseUrl + "/");
            headers.set("X-GWT-Permutation", "PermutationNotUsedByServer");
            
            if (sessionId != null) {
                headers.set("Cookie", "JSESSIONID=" + sessionId);
            }
            
            HttpEntity<String> entity = new HttpEntity<>(payload, headers);
            
            ResponseEntity<String> response = restTemplate.postForEntity(url, entity, String.class);
            
            if (response.getStatusCode().is2xxSuccessful()) {
                return parseSecurityViewResponse(response.getBody());
            } else {
                throw new RuntimeException("Failed to load security view information");
            }
            
        } catch (Exception e) {
            logger.error("Error loading security view information", e);
            throw new RuntimeException("Error loading security view information: " + e.getMessage());
        }
    }
    
    private String buildAuthenticationPayload(String username, String password) {
        // Simplified GWT RPC payload for authentication
        return String.format("7|0|8|%s|%s|%s|%s|%s|1|2|3|4|2|5|6|7|8|%s|%s|",
            reportServerBaseUrl + "/",
            "4A5B1C6BC8C4E4B269F6C40A91D16119",  // Service hash
            "net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler",
            "authenticate",
            "net.datenwerke.security.client.login.AuthToken",
            username,
            password
        );
    }
    
    private String buildSessionCheckPayload() {
        return String.format("7|0|4|%s|%s|%s|%s|1|2|3|4|",
            reportServerBaseUrl + "/",
            "4A5B1C6BC8C4E4B269F6C40A91D16119",
            "net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler",
            "isAuthenticated"
        );
    }
    
    private String buildLogoutPayload() {
        return String.format("7|0|4|%s|%s|%s|%s|1|2|3|4|",
            reportServerBaseUrl + "/",
            "4A5B1C6BC8C4E4B269F6C40A91D16119",
            "net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler",
            "logoff"
        );
    }
    
    private String buildSecurityViewPayload(Long nodeId) {
        return String.format("7|0|6|%s|%s|%s|%s|%s|1|2|3|4|1|5|6|%d|",
            reportServerBaseUrl + "/",
            "1D8BB90B3362E3AB16AD5D9EC9568CE7",  // Security service hash
            "net.datenwerke.security.ext.client.security.rpc.SecurityRpcService",
            "loadSecurityViewInformation",
            "net.datenwerke.treedb.client.treedb.dto.AbstractNodeDto",
            nodeId
        );
    }
    
    private AuthenticationResponse parseAuthenticationResponse(String responseBody, HttpHeaders headers) {
        AuthenticationResponse response = new AuthenticationResponse();
        
        // Parse GWT RPC response
        if (responseBody.contains("//OK")) {
            response.setSuccess(true);
            
            // Extract session ID from Set-Cookie header
            String sessionId = extractSessionId(headers);
            response.setSessionId(sessionId);
            
            // Parse user information from response body
            UserInfo user = parseUserFromResponse(responseBody);
            response.setUser(user);
            
        } else {
            response.setSuccess(false);
            response.setMessage("Authentication failed");
        }
        
        return response;
    }
    
    private AuthenticationResponse parseSessionCheckResponse(String responseBody, HttpHeaders headers) {
        AuthenticationResponse response = new AuthenticationResponse();
        
        if (responseBody.contains("//OK") && !responseBody.contains("null")) {
            response.setSuccess(true);
            UserInfo user = parseUserFromResponse(responseBody);
            response.setUser(user);
        } else {
            response.setSuccess(false);
            response.setMessage("Session not valid");
        }
        
        return response;
    }
    
    private SecurityViewInformation parseSecurityViewResponse(String responseBody) {
        SecurityViewInformation info = new SecurityViewInformation();
        
        // TODO: Implement proper GWT RPC response parsing for security information
        // This is a simplified version - real implementation would need to parse the GWT RPC format
        
        return info;
    }
    
    private String extractSessionId(HttpHeaders headers) {
        if (headers.containsKey("Set-Cookie")) {
            for (String cookie : headers.get("Set-Cookie")) {
                if (cookie.startsWith("JSESSIONID=")) {
                    Pattern pattern = Pattern.compile("JSESSIONID=([^;]+)");
                    Matcher matcher = pattern.matcher(cookie);
                    if (matcher.find()) {
                        return matcher.group(1);
                    }
                }
            }
        }
        return null;
    }
    
    private UserInfo parseUserFromResponse(String responseBody) {
        UserInfo user = new UserInfo();
        
        // TODO: Implement proper GWT RPC response parsing for user information
        // This is a simplified version - real implementation would need to parse the GWT RPC format
        
        user.setUsername("parsed_user");
        user.setActive(true);
        
        return user;
    }
}
