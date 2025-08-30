package net.datenwerke.rs.sidecar.model.dto.auth;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.List;

/**
 * Access Control List DTO matching ReportServer's ACL structure
 */
public class AccessControlList {
    
    @JsonProperty("aces")
    private List<AccessControlEntry> aces;
    
    @JsonProperty("inheritanceType")
    private String inheritanceType;
    
    @JsonProperty("inheritedFrom")
    private SecureeInfo inheritedFrom;
    
    public AccessControlList() {}
    
    public List<AccessControlEntry> getAces() {
        return aces;
    }
    
    public void setAces(List<AccessControlEntry> aces) {
        this.aces = aces;
    }
    
    public String getInheritanceType() {
        return inheritanceType;
    }
    
    public void setInheritanceType(String inheritanceType) {
        this.inheritanceType = inheritanceType;
    }
    
    public SecureeInfo getInheritedFrom() {
        return inheritedFrom;
    }
    
    public void setInheritedFrom(SecureeInfo inheritedFrom) {
        this.inheritedFrom = inheritedFrom;
    }
}
