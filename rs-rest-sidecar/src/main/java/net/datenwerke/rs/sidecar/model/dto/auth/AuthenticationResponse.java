package net.datenwerke.rs.sidecar.model.dto.auth;

import com.fasterxml.jackson.annotation.JsonProperty;

/**
 * Authentication response DTO matching ReportServer's AuthenticateResultDto structure
 */
public class AuthenticationResponse {
    
    @JsonProperty("success")
    private boolean success;
    
    @JsonProperty("sessionId")
    private String sessionId;
    
    @JsonProperty("user")
    private UserInfo user;
    
    @JsonProperty("message")
    private String message;
    
    @JsonProperty("token")
    private String token;
    
    public AuthenticationResponse() {}
    
    public AuthenticationResponse(boolean success, String message) {
        this.success = success;
        this.message = message;
    }
    
    public boolean isSuccess() {
        return success;
    }
    
    public void setSuccess(boolean success) {
        this.success = success;
    }
    
    public String getSessionId() {
        return sessionId;
    }
    
    public void setSessionId(String sessionId) {
        this.sessionId = sessionId;
    }
    
    public UserInfo getUser() {
        return user;
    }
    
    public void setUser(UserInfo user) {
        this.user = user;
    }
    
    public String getMessage() {
        return message;
    }
    
    public void setMessage(String message) {
        this.message = message;
    }
    
    public String getToken() {
        return token;
    }
    
    public void setToken(String token) {
        this.token = token;
    }
}
