package net.datenwerke.rs.sidecar.model.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.List;

/**
 * User information DTO matching ReportServer's UserDto structure
 */
public class UserInfo {
    
    @JsonProperty("id")
    private Long id;
    
    @JsonProperty("username")
    private String username;
    
    @JsonProperty("firstname")
    private String firstname;
    
    @JsonProperty("lastname")
    private String lastname;
    
    @JsonProperty("email")
    private String email;
    
    @JsonProperty("active")
    private boolean active;
    
    @JsonProperty("superUser")
    private boolean superUser;
    
    @JsonProperty("groups")
    private List<String> groups;
    
    @JsonProperty("roles")
    private List<String> roles;
    
    public UserInfo() {}
    
    public Long getId() {
        return id;
    }
    
    public void setId(Long id) {
        this.id = id;
    }
    
    public String getUsername() {
        return username;
    }
    
    public void setUsername(String username) {
        this.username = username;
    }
    
    public String getFirstname() {
        return firstname;
    }
    
    public void setFirstname(String firstname) {
        this.firstname = firstname;
    }
    
    public String getLastname() {
        return lastname;
    }
    
    public void setLastname(String lastname) {
        this.lastname = lastname;
    }
    
    public String getEmail() {
        return email;
    }
    
    public void setEmail(String email) {
        this.email = email;
    }
    
    public boolean isActive() {
        return active;
    }
    
    public void setActive(boolean active) {
        this.active = active;
    }
    
    public boolean isSuperUser() {
        return superUser;
    }
    
    public void setSuperUser(boolean superUser) {
        this.superUser = superUser;
    }
    
    public List<String> getGroups() {
        return groups;
    }
    
    public void setGroups(List<String> groups) {
        this.groups = groups;
    }
    
    public List<String> getRoles() {
        return roles;
    }
    
    public void setRoles(List<String> roles) {
        this.roles = roles;
    }
}
