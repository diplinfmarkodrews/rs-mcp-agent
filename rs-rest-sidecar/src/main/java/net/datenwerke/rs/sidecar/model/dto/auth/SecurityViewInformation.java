package net.datenwerke.rs.sidecar.model.dto.auth;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.List;

/**
 * Security view information DTO matching ReportServer's SecurityViewInformation structure
 */
public class SecurityViewInformation {
    
    @JsonProperty("securee")
    private SecureeInfo securee;
    
    @JsonProperty("acl")
    private AccessControlList acl;
    
    @JsonProperty("availableAccessTypes")
    private List<String> availableAccessTypes;
    
    @JsonProperty("inheritancePath")
    private List<SecureeInfo> inheritancePath;
    
    public SecurityViewInformation() {}
    
    public SecureeInfo getSecuree() {
        return securee;
    }
    
    public void setSecuree(SecureeInfo securee) {
        this.securee = securee;
    }
    
    public AccessControlList getAcl() {
        return acl;
    }
    
    public void setAcl(AccessControlList acl) {
        this.acl = acl;
    }
    
    public List<String> getAvailableAccessTypes() {
        return availableAccessTypes;
    }
    
    public void setAvailableAccessTypes(List<String> availableAccessTypes) {
        this.availableAccessTypes = availableAccessTypes;
    }
    
    public List<SecureeInfo> getInheritancePath() {
        return inheritancePath;
    }
    
    public void setInheritancePath(List<SecureeInfo> inheritancePath) {
        this.inheritancePath = inheritancePath;
    }
}
