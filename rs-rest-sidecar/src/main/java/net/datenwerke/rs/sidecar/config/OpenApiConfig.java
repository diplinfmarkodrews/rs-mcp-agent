package net.datenwerke.rs.sidecar.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.info.Info;
import io.swagger.v3.oas.models.info.Contact;
import io.swagger.v3.oas.models.info.License;

/**
 * Configuration for OpenAPI/Swagger documentation
 */
@Configuration
public class OpenApiConfig {
    
    @Bean
    public OpenAPI customOpenAPI() {
        return new OpenAPI()
                .info(new Info()
                        .title("ReportServer Java Sidecar REST API")
                        .version("1.0.0")
                        .description("REST API exposing ReportServer GWT RPC endpoints")
                        .contact(new Contact()
                                .name("ReportServer Team")
                                .url("https://reportserver.net"))
                        .license(new License()
                                .name("Apache License 2.0")
                                .url("https://www.apache.org/licenses/LICENSE-2.0")));
    }
}
