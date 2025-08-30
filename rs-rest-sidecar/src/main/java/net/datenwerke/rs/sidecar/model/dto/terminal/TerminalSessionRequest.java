package net.datenwerke.rs.sidecar.model.dto.terminal;

/**
 * Request DTO for initializing a terminal session
 */
public class TerminalSessionRequest {
    
    private Long nodeId;
    
    public TerminalSessionRequest() {}
    
    public TerminalSessionRequest(Long nodeId) {
        this.nodeId = nodeId;
    }
    
    public Long getNodeId() {
        return nodeId;
    }
    
    public void setNodeId(Long nodeId) {
        this.nodeId = nodeId;
    }
}
