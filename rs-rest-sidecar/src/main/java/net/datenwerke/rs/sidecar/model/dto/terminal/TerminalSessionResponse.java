package net.datenwerke.rs.sidecar.model.dto.terminal;

import java.util.Map;

/**
 * Response DTO for terminal session initialization
 */
public class TerminalSessionResponse {
    
    private String sessionId;
    private String pathWay;
    private boolean success;
    private String message;
    
    public TerminalSessionResponse() {}
    
    public TerminalSessionResponse(String sessionId, String pathWay) {
        this.sessionId = sessionId;
        this.pathWay = pathWay;
        this.success = true;
    }
    
    public TerminalSessionResponse(boolean success, String message) {
        this.success = success;
        this.message = message;
    }
    
    public String getSessionId() {
        return sessionId;
    }
    
    public void setSessionId(String sessionId) {
        this.sessionId = sessionId;
    }
    
    public String getPathWay() {
        return pathWay;
    }
    
    public void setPathWay(String pathWay) {
        this.pathWay = pathWay;
    }
    
    public boolean isSuccess() {
        return success;
    }
    
    public void setSuccess(boolean success) {
        this.success = success;
    }
    
    public String getMessage() {
        return message;
    }
    
    public void setMessage(String message) {
        this.message = message;
    }
}
