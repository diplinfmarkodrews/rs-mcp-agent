package com.example.reportserver.bridge;

import java.io.Serializable;

/**
 * Represents the result of a report generation
 */
public class ReportResult implements Serializable {
    private static final long serialVersionUID = 1L;
    
    private boolean success;
    private String errorMessage;
    private byte[] reportData;
    private String reportMimeType;
    private String reportFilename;
    
    public ReportResult() {
    }
    
    public boolean isSuccess() {
        return success;
    }
    
    public void setSuccess(boolean success) {
        this.success = success;
    }
    
    public String getErrorMessage() {
        return errorMessage;
    }
    
    public void setErrorMessage(String errorMessage) {
        this.errorMessage = errorMessage;
    }
    
    public byte[] getReportData() {
        return reportData;
    }
    
    public void setReportData(byte[] reportData) {
        this.reportData = reportData;
    }
    
    public String getReportMimeType() {
        return reportMimeType;
    }
    
    public void setReportMimeType(String reportMimeType) {
        this.reportMimeType = reportMimeType;
    }
    
    public String getReportFilename() {
        return reportFilename;
    }
    
    public void setReportFilename(String reportFilename) {
        this.reportFilename = reportFilename;
    }
}
