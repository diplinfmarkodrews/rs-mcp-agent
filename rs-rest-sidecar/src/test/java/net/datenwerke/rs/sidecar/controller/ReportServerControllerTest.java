package net.datenwerke.rs.sidecar.controller;

import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.TestPropertySource;

/**
 * Integration tests for ReportServer Controller
 */
@SpringBootTest
@TestPropertySource(properties = {
    "reportserver.base-url=http://localhost:8090/reportserver"
})
public class ReportServerControllerTest {
    
    @Test
    public void contextLoads() {
        // Test that the Spring context loads successfully
    }
}
