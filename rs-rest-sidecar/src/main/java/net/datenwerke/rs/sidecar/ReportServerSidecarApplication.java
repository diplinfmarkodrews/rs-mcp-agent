package net.datenwerke.rs.sidecar;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.web.client.RestTemplate;

/**
 * Main application class for the ReportServer Java Sidecar
 * 
 * This application serves as a bridge between .NET applications and ReportServer's GWT RPC endpoints,
 * exposing them as REST APIs that can be easily consumed by .NET HttpClient.
 */
@SpringBootApplication
public class ReportServerSidecarApplication {

    public static void main(String[] args) {
        SpringApplication.run(ReportServerSidecarApplication.class, args);
    }

    /**
     * Configure HTTP client for communicating with ReportServer
     */
    @Bean
    public RestTemplate restTemplate() {
        return new RestTemplate();
    }
}
