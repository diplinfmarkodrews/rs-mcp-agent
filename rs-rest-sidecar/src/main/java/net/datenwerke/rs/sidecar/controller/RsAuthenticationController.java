package net.datenwerke.rs.sidecar.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import net.datenwerke.rs.sidecar.model.dto.auth.*;
import net.datenwerke.rs.sidecar.service.RsAuthenticationGwtRpcService;

/**
 * REST Controller exposing ReportServer authentication and security endpoints
 * for consumption by .NET applications
 */
@RestController
@RequestMapping("/api")
@CrossOrigin(origins = "*", maxAge = 3600)
public class RsAuthenticationController {
    
    private static final Logger logger = LoggerFactory.getLogger(RsAuthenticationController.class);
    
    private final RsAuthenticationGwtRpcService authService;
    
    public RsAuthenticationController(RsAuthenticationGwtRpcService authService) {
        this.authService = authService;
    }
    
    /**
     * Authenticate user with ReportServer
     * 
     * @param request Authentication request containing username and password
     * @return Authentication response with session information
     */
    @PostMapping("/auth/login")
    public ResponseEntity<AuthenticationResponse> login(@RequestBody AuthenticationRequest request) {
        logger.info("Authentication request for user: {}", request.getUsername());
        
        try {
            AuthenticationResponse response = authService.authenticate(request);
            
            if (response.isSuccess()) {
                logger.info("Authentication successful for user: {}", request.getUsername());
                return ResponseEntity.ok(response);
            } else {
                logger.warn("Authentication failed for user: {}", request.getUsername());
                return ResponseEntity.status(401).body(response);
            }
            
        } catch (Exception e) {
            logger.error("Error during authentication for user: " + request.getUsername(), e);
            AuthenticationResponse errorResponse = new AuthenticationResponse(false, "Internal server error");
            return ResponseEntity.status(500).body(errorResponse);
        }
    }
    
    /**
     * Check if user session is still valid
     * 
     * @param sessionId Session ID to validate
     * @return Authentication status and user information
     */
    @GetMapping("/auth/check")
    public ResponseEntity<AuthenticationResponse> checkAuthentication(
            @RequestParam(value = "sessionId", required = false) String sessionId,
            @CookieValue(value = "JSESSIONID", required = false) String cookieSessionId) {
        
        String effectiveSessionId = sessionId != null ? sessionId : cookieSessionId;
        logger.debug("Checking authentication for session: {}", effectiveSessionId);
        
        try {
            AuthenticationResponse response = authService.checkAuthentication(effectiveSessionId);
            
            if (response.isSuccess()) {
                return ResponseEntity.ok(response);
            } else {
                return ResponseEntity.status(401).body(response);
            }
            
        } catch (Exception e) {
            logger.error("Error during session check", e);
            AuthenticationResponse errorResponse = new AuthenticationResponse(false, "Internal server error");
            return ResponseEntity.status(500).body(errorResponse);
        }
    }
    
    /**
     * Logout user from ReportServer
     * 
     * @param sessionId Session ID to logout
     * @return Success response
     */
    @PostMapping("/auth/logout")
    public ResponseEntity<String> logout(
            @RequestParam(value = "sessionId", required = false) String sessionId,
            @CookieValue(value = "JSESSIONID", required = false) String cookieSessionId) {
        
        String effectiveSessionId = sessionId != null ? sessionId : cookieSessionId;
        logger.info("Logout request for session: {}", effectiveSessionId);
        
        try {
            authService.logout(effectiveSessionId);
            return ResponseEntity.ok("Logout successful");
            
        } catch (Exception e) {
            logger.error("Error during logout", e);
            return ResponseEntity.status(500).body("Logout failed");
        }
    }

    /**
     * Get the HMAC challenge passphrase for ReportServer passwords
     * 
     * @param sessionId Session ID for authentication
     * @return The challenge passphrase string
     */
    @GetMapping("/auth/challenge")
    public ResponseEntity<String> getChallengePassphrase(
            @RequestParam(value = "sessionId", required = false) String sessionId,
            @CookieValue(value = "JSESSIONID", required = false) String cookieSessionId) {
        String effectiveSessionId = sessionId != null ? sessionId : cookieSessionId;
        logger.debug("Requesting challenge passphrase with session: {}", effectiveSessionId);
        try {
            String passphrase = authService.getHmacPassphrase(effectiveSessionId);
            if (passphrase != null) {
                return ResponseEntity.ok(passphrase);
            } else {
                return ResponseEntity.status(404).body("Challenge passphrase not found");
            }
        } catch (Exception e) {
            logger.error("Error getting challenge passphrase", e);
            return ResponseEntity.status(500).body("Error getting challenge passphrase");
        }
    }
    /**
     * Load security view information for a node
     * 
     * @param nodeId Node ID to get security information for
     * @param sessionId Session ID for authentication
     * @return Security view information
     */
    @GetMapping("/security/view/{nodeId}")
    public ResponseEntity<SecurityViewInformation> getSecurityViewInformation(
            @PathVariable Long nodeId,
            @RequestParam(value = "sessionId", required = false) String sessionId,
            @CookieValue(value = "JSESSIONID", required = false) String cookieSessionId) {
        
        String effectiveSessionId = sessionId != null ? sessionId : cookieSessionId;
        logger.debug("Loading security view for node: {} with session: {}", nodeId, effectiveSessionId);
        
        try {
            SecurityViewInformation info = authService.loadSecurityViewInformation(nodeId, effectiveSessionId);
            return ResponseEntity.ok(info);
            
        } catch (Exception e) {
            logger.error("Error loading security view information for node: " + nodeId, e);
            return ResponseEntity.status(500).build();
        }
    }
    
    /**
     * Health check endpoint
     * 
     * @return Health status
     */
    @GetMapping("/health")
    public ResponseEntity<String> health() {
        return ResponseEntity.ok("ReportServer Sidecar is running");
    }
}
