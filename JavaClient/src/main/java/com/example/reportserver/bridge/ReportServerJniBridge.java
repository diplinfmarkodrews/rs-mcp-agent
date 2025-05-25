package com.example.reportserver.bridge;

import java.rmi.Naming;
import java.rmi.RemoteException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * JNI Bridge for communicating with the Java ReportServer via RMI.
 * This class serves as the bridge between C# (via JNI) and Java RMI.
 */
public class ReportServerJniBridge {
    private String serverAddress;
    private ReportServerInterface reportServer;
    
    /**
     * Constructor that connects to the ReportServer via RMI
     * 
     * @param serverAddress The address of the ReportServer
     */
    public ReportServerJniBridge(String serverAddress) {
        this.serverAddress = serverAddress;
        
        try {
            // Look up the ReportServer in the RMI registry
            String rmiUrl = "rmi://" + serverAddress + "/ReportServer";
            System.out.println("Looking up ReportServer at " + rmiUrl);
            reportServer = (ReportServerInterface) Naming.lookup(rmiUrl);
            System.out.println("Connected to ReportServer");
        } catch (Exception e) {
            System.err.println("Error connecting to ReportServer: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Generates a report using the ReportServer
     * 
     * @param templateId The template ID for the report
     * @param parameters The parameters for the report
     * @param outputFormat The desired output format
     * @param includeCharts Whether to include charts in the report
     * @return The result of the report generation
     */
    public ReportResult generateReport(String templateId, Map<String, String> parameters, 
                                      String outputFormat, boolean includeCharts) {
        try {
            if (reportServer == null) {
                ReportResult result = new ReportResult();
                result.setSuccess(false);
                result.setErrorMessage("Not connected to ReportServer");
                return result;
            }
            
            return reportServer.generateReport(templateId, parameters, outputFormat, includeCharts);
        } catch (RemoteException e) {
            System.err.println("Error generating report: " + e.getMessage());
            e.printStackTrace();
            
            ReportResult result = new ReportResult();
            result.setSuccess(false);
            result.setErrorMessage("Error generating report: " + e.getMessage());
            return result;
        }
    }
    
    /**
     * Gets a list of available report templates from the ReportServer
     * 
     * @return List of available report templates
     */
    public List<ReportTemplate> getAvailableReportTemplates() {
        try {
            if (reportServer == null) {
                return new ArrayList<>();
            }
            
            return reportServer.getAvailableReportTemplates();
        } catch (RemoteException e) {
            System.err.println("Error retrieving report templates: " + e.getMessage());
            e.printStackTrace();
            return new ArrayList<>();
        }
    }
    
    /**
     * Checks the health of the ReportServer
     * 
     * @return True if the server is healthy, false otherwise
     */
    public boolean checkHealth() {
        try {
            if (reportServer == null) {
                return false;
            }
            
            return reportServer.checkHealth();
        } catch (RemoteException e) {
            System.err.println("Error checking ReportServer health: " + e.getMessage());
            e.printStackTrace();
            return false;
        }
    }
}
