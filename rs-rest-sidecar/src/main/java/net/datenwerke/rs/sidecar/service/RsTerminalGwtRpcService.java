package net.datenwerke.rs.sidecar.service;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;
import org.springframework.http.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import net.datenwerke.rs.sidecar.model.dto.terminal.*;

import java.util.*;

/**
 * Service for communicating with ReportServer's Terminal GWT RPC endpoints
 */
@Service
public class RsTerminalGwtRpcService {
    
    private static final Logger logger = LoggerFactory.getLogger(RsTerminalGwtRpcService.class);
    
    @Value("${reportserver.base-url}")
    private String reportServerBaseUrl;
    
    private final RestTemplate restTemplate;
    
    public RsTerminalGwtRpcService(RestTemplate restTemplate) {
        this.restTemplate = restTemplate;
    }
    
    /**
     * Initialize a terminal session
     */
    public TerminalSessionResponse initSession(Long nodeId, String sessionId) {
        try {
            String url = reportServerBaseUrl + "/terminal";
            
            String payload = buildInitSessionPayload(nodeId);
            
            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.TEXT_PLAIN);
            headers.set("X-GWT-Module-Base", reportServerBaseUrl + "/");
            headers.set("X-GWT-Permutation", "PermutationNotUsedByServer");
            
            if (sessionId != null) {
                headers.set("Cookie", "JSESSIONID=" + sessionId);
            }
            
            HttpEntity<String> entity = new HttpEntity<>(payload, headers);
            
            ResponseEntity<String> response = restTemplate.postForEntity(url, entity, String.class);
            
            if (response.getStatusCode().is2xxSuccessful()) {
                return parseInitSessionResponse(response.getBody());
            } else {
                return new TerminalSessionResponse(false, "Failed to initialize terminal session");
            }
            
        } catch (Exception e) {
            logger.error("Error during terminal session initialization", e);
            return new TerminalSessionResponse(false, "Error initializing terminal session: " + e.getMessage());
        }
    }
    
    /**
     * Execute a terminal command
     */
    public TerminalExecuteResponse executeCommand(String terminalSessionId, String command, String sessionId) {
        try {
            String url = reportServerBaseUrl + "/terminal";
            
            String payload = buildExecuteCommandPayload(terminalSessionId, command);
            
            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.TEXT_PLAIN);
            headers.set("X-GWT-Module-Base", reportServerBaseUrl + "/");
            headers.set("X-GWT-Permutation", "PermutationNotUsedByServer");
            
            if (sessionId != null) {
                headers.set("Cookie", "JSESSIONID=" + sessionId);
            }
            
            HttpEntity<String> entity = new HttpEntity<>(payload, headers);
            
            ResponseEntity<String> response = restTemplate.postForEntity(url, entity, String.class);
            
            if (response.getStatusCode().is2xxSuccessful()) {
                return parseExecuteCommandResponse(response.getBody());
            } else {
                return new TerminalExecuteResponse(false, "Failed to execute terminal command");
            }
            
        } catch (Exception e) {
            logger.error("Error during terminal command execution", e);
            return new TerminalExecuteResponse(false, "Error executing terminal command: " + e.getMessage());
        }
    }
    
    private String buildInitSessionPayload(Long nodeId) {
        // Based on rsTerminalRequests.txt:
        // 7|0|6|<base>|<service hash>|net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService|initSession|net.datenwerke.treedb.client.treedb.dto.AbstractNodeDto/45121059|net.datenwerke.gxtdto.client.dtomanager.Dto2PosoMapper|1|2|3|4|2|5|6|0|0|
        return String.format("7|0|6|%s|%s|%s|%s|%s|%s|1|2|3|4|2|5|6|%d|0|",
            reportServerBaseUrl + "/",
            "C363EE187A6E3AED00BD381336F9868C", // service hash for TerminalRpcService
            "net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService",
            "initSession",
            "net.datenwerke.treedb.client.treedb.dto.AbstractNodeDto/45121059",
            "net.datenwerke.gxtdto.client.dtomanager.Dto2PosoMapper",
            nodeId != null ? nodeId : 0
        );
    }
    
    private String buildExecuteCommandPayload(String terminalSessionId, String command) {
        // Based on rsTerminalRequests.txt:
        // 7|0|7|<base>|<service hash>|net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService|execute|java.lang.String/2004016611|<sessionId>|<command>|1|2|3|4|2|5|5|6|7|
        return String.format("7|0|7|%s|%s|%s|%s|%s|%s|%s|1|2|3|4|2|5|5|6|7|",
            reportServerBaseUrl + "/",
            "C363EE187A6E3AED00BD381336F9868C", // service hash for TerminalRpcService
            "net.datenwerke.rs.terminal.client.terminal.rpc.TerminalRpcService",
            "execute",
            "java.lang.String/2004016611",
            terminalSessionId != null ? terminalSessionId : "",
            command != null ? command : ""
        );
    }
    
    private TerminalSessionResponse parseInitSessionResponse(String responseBody) {
        // Example: //OK[5,2,4,2,0,3,2,2,1,["java.util.HashMap/1797211028","java.lang.String/2004016611","pathWay","sessionId","58bf8974-255d-444c-b74e-02999d4983ba"],0,7]
        if (responseBody == null || !responseBody.contains("//OK")) {
            return new TerminalSessionResponse(false, "Invalid response");
        }
        
        try {
            // Extract the session ID from the response
            // The sessionId is the last string in the string array
            int arrStart = responseBody.indexOf("[");
            int arrEnd = responseBody.lastIndexOf("]");
            if (arrStart > 0 && arrEnd > arrStart) {
                String arrContent = responseBody.substring(arrStart + 1, arrEnd);
                int strArrStart = arrContent.lastIndexOf("[");
                int strArrEnd = arrContent.lastIndexOf("]");
                if (strArrStart > 0 && strArrEnd > strArrStart) {
                    String strArr = arrContent.substring(strArrStart + 1, strArrEnd);
                    String[] parts = strArr.split(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    
                    String sessionId = null;
                    String pathWay = null;
                    
                    // Look for sessionId and pathWay in the response
                    for (int i = 0; i < parts.length - 1; i++) {
                        String part = parts[i].trim();
                        if (part.startsWith("\"") && part.endsWith("\"")) {
                            String value = part.substring(1, part.length() - 1);
                            if ("sessionId".equals(value) && i + 1 < parts.length) {
                                String nextPart = parts[i + 1].trim();
                                if (nextPart.startsWith("\"") && nextPart.endsWith("\"")) {
                                    sessionId = nextPart.substring(1, nextPart.length() - 1);
                                }
                            } else if ("pathWay".equals(value) && i + 1 < parts.length) {
                                String nextPart = parts[i + 1].trim();
                                if (nextPart.startsWith("\"") && nextPart.endsWith("\"")) {
                                    pathWay = nextPart.substring(1, nextPart.length() - 1);
                                }
                            }
                        }
                    }
                    
                    if (sessionId != null) {
                        return new TerminalSessionResponse(sessionId, pathWay);
                    }
                }
            }
        } catch (Exception e) {
            logger.warn("Failed to parse terminal session response", e);
        }
        
        return new TerminalSessionResponse(false, "Failed to parse session response");
    }
    
    private TerminalExecuteResponse parseExecuteCommandResponse(String responseBody) {
        // Example response with command results like "ls" showing directories
        if (responseBody == null || !responseBody.contains("//OK")) {
            return new TerminalExecuteResponse(false, "Invalid response");
        }
        
        try {
            // Extract the command results from the response
            // The results are in the string array at the end
            int arrStart = responseBody.indexOf("[");
            int arrEnd = responseBody.lastIndexOf("]");
            if (arrStart > 0 && arrEnd > arrStart) {
                String arrContent = responseBody.substring(arrStart + 1, arrEnd);
                int strArrStart = arrContent.lastIndexOf("[");
                int strArrEnd = arrContent.lastIndexOf("]");
                if (strArrStart > 0 && strArrEnd > strArrStart) {
                    String strArr = arrContent.substring(strArrStart + 1, strArrEnd);
                    String[] parts = strArr.split(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    
                    List<String> results = new ArrayList<>();
                    String displayMode = null;
                    
                    // Parse the string array to extract meaningful results
                    for (String part : parts) {
                        String trimmed = part.trim();
                        if (trimmed.startsWith("\"") && trimmed.endsWith("\"")) {
                            String value = trimmed.substring(1, trimmed.length() - 1);
                            // Skip GWT class names and add actual content
                            if (!value.contains("/") && !value.contains(".") && 
                                !value.isEmpty() && !value.equals("java.lang.String") &&
                                !value.contains("ArrayList") && !value.contains("HashMap")) {
                                results.add(value);
                            }
                        }
                    }
                    
                    return new TerminalExecuteResponse(results, displayMode);
                }
            }
        } catch (Exception e) {
            logger.warn("Failed to parse terminal execute response", e);
        }
        
        return new TerminalExecuteResponse(false, "Failed to parse command response");
    }
}
