package net.datenwerke.rs.sidecar.model.dto.auth;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.Map;

/**
 * Access Control Entry DTO matching ReportServer's AceDto structure
 */
public class AccessControlEntry {
    
    @JsonProperty("id")
    private Long id;
    
    @JsonProperty("folk")
    private UserInfo folk;
    
    @JsonProperty("accessMap")
    private Map<String, String> accessMap;
    
    @JsonProperty("inheritanceType")
    private String inheritanceType;
    
    @JsonProperty("negative")
    private boolean negative;
    
    public AccessControlEntry() {}
    
    public Long getId() {
        return id;
    }
    
    public void setId(Long id) {
        this.id = id;
    }
    
    public UserInfo getFolk() {
        return folk;
    }
    
    public void setFolk(UserInfo folk) {
        this.folk = folk;
    }
    
    public Map<String, String> getAccessMap() {
        return accessMap;
    }
    
    public void setAccessMap(Map<String, String> accessMap) {
        this.accessMap = accessMap;
    }
    
    public String getInheritanceType() {
        return inheritanceType;
    }
    
    public void setInheritanceType(String inheritanceType) {
        this.inheritanceType = inheritanceType;
    }
    
    public boolean isNegative() {
        return negative;
    }
    
    public void setNegative(boolean negative) {
        this.negative = negative;
    }
}
