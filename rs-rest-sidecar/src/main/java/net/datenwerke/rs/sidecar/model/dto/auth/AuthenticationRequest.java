package net.datenwerke.rs.sidecar.model.dto.auth;

import com.fasterxml.jackson.annotation.JsonProperty;

/**
 * Authentication request DTO matching ReportServer's AuthToken structure
 */
public class AuthenticationRequest {
    
    @JsonProperty("username")
    private String username;
    
    @JsonProperty("password") 
    private String password;
    
    @JsonProperty("encrypted")
    private boolean encrypted = false;
    
    public AuthenticationRequest() {}
    
    public AuthenticationRequest(String username, String password) {
        this.username = username;
        this.password = password;
    }
    
    public AuthenticationRequest(String username, String password, boolean encrypted) {
        this.username = username;
        this.password = password;
        this.encrypted = encrypted;
    }
    
    public String getUsername() {
        return username;
    }
    
    public void setUsername(String username) {
        this.username = username;
    }
    
    public String getPassword() {
        return password;
    }
    
    public void setPassword(String password) {
        this.password = password;
    }
    
    public boolean isEncrypted() {
        return encrypted;
    }
    
    public void setEncrypted(boolean encrypted) {
        this.encrypted = encrypted;
    }
}
