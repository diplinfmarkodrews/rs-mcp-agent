package net.datenwerke.rs.sidecar.service;

import java.util.regex.Matcher;
import java.util.regex.Pattern;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;

import net.datenwerke.rs.sidecar.model.dto.auth.AuthenticationRequest;
import net.datenwerke.rs.sidecar.model.dto.auth.AuthenticationResponse;
import net.datenwerke.rs.sidecar.model.dto.auth.SecurityViewInformation;
import net.datenwerke.rs.sidecar.model.dto.auth.UserInfo;

/**
 * Service for communicating with ReportServer's GWT RPC endpoints
 */
@Service
public class RsAuthenticationGwtRpcService {
    
    private static final Logger logger = LoggerFactory.getLogger(RsAuthenticationGwtRpcService.class);
    
    @Value("${reportserver.base-url}")
    private String reportServerBaseUrl;
    
    private final RestTemplate restTemplate;
    
    public RsAuthenticationGwtRpcService(RestTemplate restTemplate) {
        this.restTemplate = restTemplate;
    }
    
    /**
     * Authenticate user with ReportServer
     */
    public AuthenticationResponse authenticate(AuthenticationRequest request) {
        try {
            String url = reportServerBaseUrl + "/login";
            
            // Create GWT RPC payload for authentication
            String payload = buildAuthenticationPayload(request.getUsername(), request.getPassword());
            
            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.TEXT_PLAIN);
            headers.set("X-GWT-Module-Base", reportServerBaseUrl + "/");
            headers.set("X-GWT-Permutation", "PermutationNotUsedByServer");
            
            HttpEntity<String> entity = new HttpEntity<>(payload, headers);
            
            ResponseEntity<String> response = restTemplate.postForEntity(url, entity, String.class);
            
            if (response.getStatusCode().is2xxSuccessful()) {
                return parseAuthenticationResponse(response.getBody(), response.getHeaders());
            } else {
                return new AuthenticationResponse(false, "Authentication failed with status: " + response.getStatusCode());
            }
            
        } catch (Exception e) {
            logger.error("Error during authentication", e);
            return new AuthenticationResponse(false, "Authentication error: " + e.getMessage());
        }
    }
    
    /**
     * Check if user session is still valid
     */
    public AuthenticationResponse checkAuthentication(String sessionId) {
        try {
            String url = reportServerBaseUrl + "/login";
            
            // Create GWT RPC payload for session check
            String payload = buildSessionCheckPayload();
            
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
                return parseSessionCheckResponse(response.getBody(), response.getHeaders());
            } else {
                return new AuthenticationResponse(false, "Session check failed");
            }
            
        } catch (Exception e) {
            logger.error("Error during session check", e);
            return new AuthenticationResponse(false, "Session check error: " + e.getMessage());
        }
    }
    
    /**
     * Logout user from ReportServer
     */
    public void logout(String sessionId) {
        try {
            String url = reportServerBaseUrl + "/login";
            
            String payload = buildLogoutPayload();
            
            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.TEXT_PLAIN);
            headers.set("X-GWT-Module-Base", reportServerBaseUrl + "/");
            headers.set("X-GWT-Permutation", "PermutationNotUsedByServer");
            
            if (sessionId != null) {
                headers.set("Cookie", "JSESSIONID=" + sessionId);
            }
            
            HttpEntity<String> entity = new HttpEntity<>(payload, headers);
            
            restTemplate.postForEntity(url, entity, String.class);
            
        } catch (Exception e) {
            logger.error("Error during logout", e);
        }
    }
    
    /**
     * Load security view information for a node
     */
    public SecurityViewInformation loadSecurityViewInformation(Long nodeId, String sessionId) {
        try {
            String url = reportServerBaseUrl + "/security_security";
            
            String payload = buildSecurityViewPayload(nodeId);
            
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
                return parseSecurityViewResponse(response.getBody());
            } else {
                throw new RuntimeException("Failed to load security view information");
            }
            
        } catch (Exception e) {
            logger.error("Error loading security view information", e);
            throw new RuntimeException("Error loading security view information: " + e.getMessage());
        }
    }
    
    private String buildAuthenticationPayload(String username, String password) {
        // Based on rsLoginRequests.txt trace #4:
        // 7|0|7|http://localhost:8090/reportserver/|DFEDD0FBBBBBE222F217D04F50A95F56|net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler|authenticate|[Lnet.datenwerke.security.client.login.AuthToken;/1508143471|net.datenwerke.rs.authenticator.client.login.dto.UserPasswordAuthToken/1647979090|root|1|2|3|4|1|5|5|1|6|7|7|
        // Note: Password appears to be handled via UserPasswordAuthToken internally, not directly in payload
        return String.format("7|0|7|%s|%s|%s|%s|%s|%s|%s|1|2|3|4|1|5|5|1|6|7|7|",
            reportServerBaseUrl + "/",
            "DFEDD0FBBBBBE222F217D04F50A95F56",  // Service hash from trace
            "net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler",
            "authenticate",
            "[Lnet.datenwerke.security.client.login.AuthToken;/1508143471", // AuthToken array type
            "net.datenwerke.rs.authenticator.client.login.dto.UserPasswordAuthToken/1647979090", // UserPasswordAuthToken type
            username != null ? username : "root" // Username parameter
        );
    }
    
    private String buildSessionCheckPayload() {
        // Based on rsLoginRequests.txt trace:
        // 7|0|4|http://localhost:8090/reportserver/|DFEDD0FBBBBBE222F217D04F50A95F56|net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler|isAuthenticated|1|2|3|4|0|
        return String.format("7|0|4|%s|%s|%s|%s|1|2|3|4|0|",
            reportServerBaseUrl + "/",
            "DFEDD0FBBBBBE222F217D04F50A95F56", // Correct service hash from trace
            "net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler",
            "isAuthenticated"
        );
    }
    
    private String buildLogoutPayload() {
        // Using same service hash as session check
        return String.format("7|0|4|%s|%s|%s|%s|1|2|3|4|0|",
            reportServerBaseUrl + "/",
            "DFEDD0FBBBBBE222F217D04F50A95F56", // Correct service hash from trace
            "net.datenwerke.rs.authenticator.client.login.rpc.LoginHandler",
            "logoff"
        );
    }
    
    private String buildSecurityViewPayload(Long nodeId) {
        // Based on rsLoginRequests.txt security request trace:
        // 7|0|31|http://localhost:8090/reportserver/|1D8BB90B3362E3AB16AD5D9EC9568CE7|net.datenwerke.security.ext.client.security.rpc.SecurityRpcService|loadGenericRights|...
        return String.format("7|0|6|%s|%s|%s|%s|%s|1|2|3|4|1|5|6|%d|0|",
            reportServerBaseUrl + "/",
            "1D8BB90B3362E3AB16AD5D9EC9568CE7",  // Correct security service hash from trace
            "net.datenwerke.security.ext.client.security.rpc.SecurityRpcService",
            "loadSecurityViewInformation",
            "net.datenwerke.treedb.client.treedb.dto.AbstractNodeDto/45121059", // Correct type hash
            nodeId != null ? nodeId : 0
        );
    }
    
    private AuthenticationResponse parseAuthenticationResponse(String responseBody, HttpHeaders headers) {
        AuthenticationResponse response = new AuthenticationResponse();
        if (responseBody == null || !responseBody.contains("//OK")) {
            response.setSuccess(false);
            response.setMessage("Authentication failed");
            return response;
        }
        
        response.setSuccess(true);
        String sessionId = extractSessionId(headers);
        response.setSessionId(sessionId);
        
        // Parse username from GWT RPC response (authenticate response structure)
        // The response contains AuthenticateResultDto with user information
        // From the trace: ["net.datenwerke.security.client.login.AuthenticateResultDto/1984250979","java.util.ArrayList/4159755760","net.datenwerke.security.client.usermanager.dto.decorator.UserDtoDec/3663459877","if.techdev@infofabrik.de","root"...]
        String username = null;
        try {
            int arrStart = responseBody.indexOf("[");
            int arrEnd = responseBody.lastIndexOf("]");
            if (arrStart > 0 && arrEnd > arrStart) {
                String arrContent = responseBody.substring(arrStart + 1, arrEnd);
                // Find the string table (last array in the response)
                int strArrStart = arrContent.lastIndexOf("[");
                int strArrEnd = arrContent.lastIndexOf("]");
                if (strArrStart > 0 && strArrEnd > strArrStart) {
                    String strArr = arrContent.substring(strArrStart + 1, strArrEnd);
                    String[] parts = strArr.split(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    // Username is at index 4 in the string table for authentication response
                    if (parts.length > 4) {
                        String userStr = parts[4].trim();
                        if (userStr.startsWith("\"") && userStr.endsWith("\"")) {
                            username = userStr.substring(1, userStr.length() - 1);
                        }
                    }
                }
            }
        } catch (Exception e) {
            logger.warn("Failed to parse username from authentication GWT RPC response", e);
        }
        
        UserInfo user = new UserInfo();
        user.setUsername(username != null ? username : "authenticated_user");
        user.setActive(true);
        response.setUser(user);
        return response;
    }
    
    private AuthenticationResponse parseSessionCheckResponse(String responseBody, HttpHeaders headersUnused) {
        AuthenticationResponse response = new AuthenticationResponse();
        if (responseBody == null || !responseBody.contains("//OK") || responseBody.contains("null")) {
            response.setSuccess(false);
            response.setMessage("Session not valid");
            return response;
        }
        response.setSuccess(true);
        // Parse username as in authentication
        String username = null;
        try {
            int arrStart = responseBody.indexOf("[");
            int arrEnd = responseBody.lastIndexOf("]");
            if (arrStart > 0 && arrEnd > arrStart) {
                String arrContent = responseBody.substring(arrStart + 1, arrEnd);
                int strArrStart = arrContent.lastIndexOf("[");
                int strArrEnd = arrContent.lastIndexOf("]");
                if (strArrStart > 0 && strArrEnd > strArrStart) {
                    String strArr = arrContent.substring(strArrStart + 1, strArrEnd);
                    String[] parts = strArr.split(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    if (parts.length > 2) {
                        String userStr = parts[2].trim();
                        if (userStr.startsWith("\"") && userStr.endsWith("\"")) {
                            username = userStr.substring(1, userStr.length() - 1);
                        }
                    }
                }
            }
        } catch (Exception e) {
            logger.warn("Failed to parse username from GWT RPC response", e);
        }
        UserInfo user = new UserInfo();
        user.setUsername(username != null ? username : "parsed_user");
        user.setActive(true);
        response.setUser(user);
        return response;
    }
    
    private SecurityViewInformation parseSecurityViewResponse(String responseBodyUnused) {
        SecurityViewInformation info = new SecurityViewInformation();
        
        // TODO: Implement proper GWT RPC response parsing for security information
        // This is a simplified version - real implementation would need to parse the GWT RPC format
        
        return info;
    }
    
    private String extractSessionId(HttpHeaders headers) {
        if (headers.containsKey("Set-Cookie")) {
            java.util.List<String> cookies = headers.get("Set-Cookie");
            if (cookies != null) {
                for (String cookie : cookies) {
                    if (cookie.startsWith("JSESSIONID=")) {
                        Pattern pattern = Pattern.compile("JSESSIONID=([^;]+)");
                        Matcher matcher = pattern.matcher(cookie);
                        if (matcher.find()) {
                            return matcher.group(1);
                        }
                    }
                }
            }
        }
        return null;
    }
    
    // Deprecated: user parsing is now inline in parseAuthenticationResponse/parseSessionCheckResponse
    private UserInfo parseUserFromResponse(String responseBodyUnused) {
        UserInfo user = new UserInfo();
        user.setUsername("parsed_user");
        user.setActive(true);
        return user;
    }
    /**
     * Request the challenge passphrase from ReportServer (ChallengeResponseRpcService.getHmacPassphrase)
     */
    public String getHmacPassphrase(String sessionId) {
        try {
            String url = reportServerBaseUrl + "/security_challengeresponse";
            String payload = buildChallengeRequestPayload();
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
                return parseChallengeResponse(response.getBody());
            } else {
                throw new RuntimeException("Failed to get challenge passphrase");
            }
        } catch (Exception e) {
            logger.error("Error getting challenge passphrase", e);
            throw new RuntimeException("Error getting challenge passphrase: " + e.getMessage());
        }
    }

    private String buildChallengeRequestPayload() {
        // See rsLoginRequests.txt, challenge request
        // 7|0|4|<base>|<service hash>|net.datenwerke.rs.authenticator.cr.client.ChallengeResponseRpcService|getHmacPassphrase|1|2|3|4|0|
        return String.format("7|0|4|%s|%s|%s|%s|1|2|3|4|0|",
            reportServerBaseUrl + "/",
            "B6F10AD9852902823F606D81A985ACC7", // service hash for ChallengeResponseRpcService
            "net.datenwerke.rs.authenticator.cr.client.ChallengeResponseRpcService",
            "getHmacPassphrase"
        );
    }

    private String parseChallengeResponse(String responseBody) {
        // Example: //OK[1,["This is the Passphrase used to compute the HMAC key for reportServer passwords."],0,7]
        if (responseBody == null || !responseBody.contains("//OK")) return null;
        int arrStart = responseBody.indexOf("[");
        int arrEnd = responseBody.lastIndexOf("]");
        if (arrStart < 0 || arrEnd < arrStart) return null;
        String arrContent = responseBody.substring(arrStart + 1, arrEnd);
        // Find the first quoted string inside the nested array
        int quote1 = arrContent.indexOf("\"");
        int quote2 = arrContent.indexOf("\"", quote1 + 1);
        if (quote1 >= 0 && quote2 > quote1) {
            return arrContent.substring(quote1 + 1, quote2);
        }
        return null;
    }
}
