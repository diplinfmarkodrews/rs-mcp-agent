package com.example.reportserver.bridge;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;

/**
 * Represents a report template
 */
public class ReportTemplate implements Serializable {
    private static final long serialVersionUID = 1L;
    
    private String id;
    private String name;
    private String description;
    private List<ParameterDefinition> requiredParameters = new ArrayList<>();
    private List<String> supportedFormats = new ArrayList<>();
    
    public ReportTemplate() {
    }
    
    public String getId() {
        return id;
    }
    
    public void setId(String id) {
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
    
    public List<ParameterDefinition> getRequiredParameters() {
        return requiredParameters;
    }
    
    public void setRequiredParameters(List<ParameterDefinition> requiredParameters) {
        this.requiredParameters = requiredParameters;
    }
    
    public void addRequiredParameter(ParameterDefinition parameter) {
        if (this.requiredParameters == null) {
            this.requiredParameters = new ArrayList<>();
        }
        this.requiredParameters.add(parameter);
    }
    
    public List<String> getSupportedFormats() {
        return supportedFormats;
    }
    
    public void setSupportedFormats(List<String> supportedFormats) {
        this.supportedFormats = supportedFormats;
    }
    
    public void addSupportedFormat(String format) {
        if (this.supportedFormats == null) {
            this.supportedFormats = new ArrayList<>();
        }
        this.supportedFormats.add(format);
    }
}
