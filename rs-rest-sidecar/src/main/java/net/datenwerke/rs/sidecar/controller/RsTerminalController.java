package net.datenwerke.rs.sidecar.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import net.datenwerke.rs.sidecar.model.dto.terminal.*;
import net.datenwerke.rs.sidecar.service.RsTerminalGwtRpcService;

/**
 * REST Controller exposing ReportServer terminal endpoints
 * for consumption by .NET applications
 */
@RestController
@RequestMapping("/api/terminal")
@CrossOrigin(origins = "*", maxAge = 3600)
public class RsTerminalController {
    
    private static final Logger logger = LoggerFactory.getLogger(RsTerminalController.class);
    
    private final RsTerminalGwtRpcService terminalService;
    
    public RsTerminalController(RsTerminalGwtRpcService terminalService) {
        this.terminalService = terminalService;
    }
    
    /**
     * Initialize a terminal session
     * 
     * @param request Terminal session request containing optional node ID
     * @param sessionId Session ID for authentication
     * @return Terminal session response with session ID and path
     */
    @PostMapping("/init")
    public ResponseEntity<TerminalSessionResponse> initSession(
            @RequestBody TerminalSessionRequest request,
            @RequestParam(value = "sessionId", required = false) String sessionId,
            @CookieValue(value = "JSESSIONID", required = false) String cookieSessionId) {
        
        String effectiveSessionId = sessionId != null ? sessionId : cookieSessionId;
        logger.debug("Initializing terminal session for node: {} with session: {}", 
                     request.getNodeId(), effectiveSessionId);
        
        try {
            TerminalSessionResponse response = terminalService.initSession(request.getNodeId(), effectiveSessionId);
            
            if (response.isSuccess()) {
                logger.info("Terminal session initialized successfully: {}", response.getSessionId());
                return ResponseEntity.ok(response);
            } else {
                logger.warn("Failed to initialize terminal session: {}", response.getMessage());
                return ResponseEntity.status(400).body(response);
            }
            
        } catch (Exception e) {
            logger.error("Error during terminal session initialization", e);
            TerminalSessionResponse errorResponse = new TerminalSessionResponse(false, "Internal server error");
            return ResponseEntity.status(500).body(errorResponse);
        }
    }
    
    /**
     * Execute a command in the terminal
     * 
     * @param request Terminal execute request containing session ID and command
     * @param sessionId Session ID for authentication
     * @return Terminal execute response with command results
     */
    @PostMapping("/execute")
    public ResponseEntity<TerminalExecuteResponse> executeCommand(
            @RequestBody TerminalExecuteRequest request,
            @RequestParam(value = "sessionId", required = false) String sessionId,
            @CookieValue(value = "JSESSIONID", required = false) String cookieSessionId) {
        
        String effectiveSessionId = sessionId != null ? sessionId : cookieSessionId;
        logger.debug("Executing terminal command '{}' in session: {} with auth session: {}", 
                     request.getCommand(), request.getSessionId(), effectiveSessionId);
        
        try {
            TerminalExecuteResponse response = terminalService.executeCommand(
                request.getSessionId(), request.getCommand(), effectiveSessionId);
            
            if (response.isSuccess()) {
                logger.info("Terminal command executed successfully: {}", request.getCommand());
                return ResponseEntity.ok(response);
            } else {
                logger.warn("Failed to execute terminal command: {}", response.getMessage());
                return ResponseEntity.status(400).body(response);
            }
            
        } catch (Exception e) {
            logger.error("Error during terminal command execution", e);
            TerminalExecuteResponse errorResponse = new TerminalExecuteResponse(false, "Internal server error");
            return ResponseEntity.status(500).body(errorResponse);
        }
    }
    
    /**
     * Execute a simple command (convenience endpoint)
     * 
     * @param terminalSessionId Terminal session ID
     * @param command Command to execute
     * @param sessionId Session ID for authentication
     * @return Terminal execute response with command results
     */
    @PostMapping("/execute/{terminalSessionId}")
    public ResponseEntity<TerminalExecuteResponse> executeSimpleCommand(
            @PathVariable String terminalSessionId,
            @RequestParam String command,
            @RequestParam(value = "sessionId", required = false) String sessionId,
            @CookieValue(value = "JSESSIONID", required = false) String cookieSessionId) {
        
        String effectiveSessionId = sessionId != null ? sessionId : cookieSessionId;
        logger.debug("Executing simple terminal command '{}' in session: {}", command, terminalSessionId);
        
        try {
            TerminalExecuteResponse response = terminalService.executeCommand(
                terminalSessionId, command, effectiveSessionId);
            
            if (response.isSuccess()) {
                return ResponseEntity.ok(response);
            } else {
                return ResponseEntity.status(400).body(response);
            }
            
        } catch (Exception e) {
            logger.error("Error during simple terminal command execution", e);
            TerminalExecuteResponse errorResponse = new TerminalExecuteResponse(false, "Internal server error");
            return ResponseEntity.status(500).body(errorResponse);
        }
    }
}
