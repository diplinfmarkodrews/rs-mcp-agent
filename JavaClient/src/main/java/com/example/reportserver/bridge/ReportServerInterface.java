package com.example.reportserver.bridge;

import java.rmi.Remote;
import java.rmi.RemoteException;
import java.util.List;
import java.util.Map;

/**
 * RMI interface for the ReportServer
 */
public interface ReportServerInterface extends Remote {
    /**
     * Generates a report using the specified template and parameters
     * 
     * @param templateId The template ID for the report
     * @param parameters The parameters for the report
     * @param outputFormat The desired output format
     * @param includeCharts Whether to include charts in the report
     * @return The result of the report generation
     * @throws RemoteException If a remote communication error occurs
     */
    ReportResult generateReport(String templateId, Map<String, String> parameters, 
                               String outputFormat, boolean includeCharts) throws RemoteException;
    
    /**
     * Gets a list of available report templates from the ReportServer
     * 
     * @return List of available report templates
     * @throws RemoteException If a remote communication error occurs
     */
    List<ReportTemplate> getAvailableReportTemplates() throws RemoteException;
    
    /**
     * Checks the health of the ReportServer
     * 
     * @return True if the server is healthy, false otherwise
     * @throws RemoteException If a remote communication error occurs
     */
    boolean checkHealth() throws RemoteException;
}
