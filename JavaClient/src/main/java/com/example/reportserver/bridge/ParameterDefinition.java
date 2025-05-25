package com.example.reportserver.bridge;

import java.io.Serializable;

/**
 * Represents a parameter definition for a report template
 */
public class ParameterDefinition implements Serializable {
    private static final long serialVersionUID = 1L;
    
    private String name;
    private String description;
    private String type;
    private boolean required;
    private String defaultValue;
    
    public ParameterDefinition() {
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
    
    public boolean isRequired() {
        return required;
    }
    
    public void setRequired(boolean required) {
        this.required = required;
    }
    
    public String getDefaultValue() {
        return defaultValue;
    }
    
    public void setDefaultValue(String defaultValue) {
        this.defaultValue = defaultValue;
    }
}
