package net.datenwerke.rs.sidecar.model.dto.terminal;

import java.util.List;

/**
 * Response DTO for terminal command execution
 */
public class TerminalExecuteResponse {
    
    private boolean success;
    private String message;
    private List<String> results;
    private String displayMode;
    
    public TerminalExecuteResponse() {}
    
    public TerminalExecuteResponse(boolean success, String message) {
        this.success = success;
        this.message = message;
    }
    
    public TerminalExecuteResponse(List<String> results, String displayMode) {
        this.success = true;
        this.results = results;
        this.displayMode = displayMode;
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
    
    public List<String> getResults() {
        return results;
    }
    
    public void setResults(List<String> results) {
        this.results = results;
    }
    
    public String getDisplayMode() {
        return displayMode;
    }
    
    public void setDisplayMode(String displayMode) {
        this.displayMode = displayMode;
    }
}
