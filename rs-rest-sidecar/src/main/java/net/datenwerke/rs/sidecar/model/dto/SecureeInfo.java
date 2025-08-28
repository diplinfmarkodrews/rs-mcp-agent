package net.datenwerke.rs.sidecar.model.dto;

import com.fasterxml.jackson.annotation.JsonProperty;

/**
 * Securee information DTO matching ReportServer's SecureeDto structure
 */
public class SecureeInfo {
    
    @JsonProperty("id")
    private Long id;
    
    @JsonProperty("name")
    private String name;
    
    @JsonProperty("description")
    private String description;
    
    @JsonProperty("type")
    private String type;
    
    @JsonProperty("path")
    private String path;
    
    public SecureeInfo() {}
    
    public Long getId() {
        return id;
    }
    
    public void setId(Long id) {
        this.id = id;
    }
    
    public String getName() {
        return name;
    }
    
    public void setName(String name) {
        this.name = name;
    }
    
    public String getDescription() {
        return description;
    }
    
    public void setDescription(String description) {
        this.description = description;
    }
    
    public String getType() {
        return type;
    }
    
    public void setType(String type) {
        this.type = type;
    }
    
    public String getPath() {
        return path;
    }
    
    public void setPath(String path) {
        this.path = path;
    }
}
