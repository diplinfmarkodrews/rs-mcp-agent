package net.datenwerke.rs.sidecar.model.dto.terminal;

/**
 * Request DTO for executing terminal commands
 */
public class TerminalExecuteRequest {
    
    private String sessionId;
    private String command;
    
    public TerminalExecuteRequest() {}
    
    public TerminalExecuteRequest(String sessionId, String command) {
        this.sessionId = sessionId;
        this.command = command;
    }
    
    public String getSessionId() {
        return sessionId;
    }
    
    public void setSessionId(String sessionId) {
        this.sessionId = sessionId;
    }
    
    public String getCommand() {
        return command;
    }
    
    public void setCommand(String command) {
        this.command = command;
    }
}
