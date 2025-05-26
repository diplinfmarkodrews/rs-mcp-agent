using Microsoft.Extensions.Logging;
using MCPClientSDK.Services;
using System.Text.Json;

namespace MCPClientSDK.Services;

/// <summary>
/// Interactive console client for the MCP Report Server
/// </summary>
public class InteractiveClient
{
    private readonly ILogger<InteractiveClient> _logger;
    private readonly McpClientService _mcpClient;

    public InteractiveClient(ILogger<InteractiveClient> logger, McpClientService mcpClient)
    {
        _logger = logger;
        _mcpClient = mcpClient;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("🔄 MCP Report Client SDK");
        Console.WriteLine("=========================");
        Console.WriteLine("Using Microsoft Extensions AI MCP SDK");
        Console.WriteLine();

        // Initial health check
        await CheckHealthAsync();
        
        while (true)
        {
            try
            {
                await ShowMainMenuAsync();
                var choice = Console.ReadLine()?.Trim();

                switch (choice?.ToLower())
                {
                    case "1":
                    case "templates":
                        await ShowTemplatesAsync();
                        break;
                    case "2":
                    case "generate":
                        await GenerateReportInteractiveAsync();
                        break;
                    case "3":
                    case "health":
                        await CheckHealthAsync();
                        break;
                    case "4":
                    case "demo":
                        await RunDemoAsync();
                        break;
                    case "5":
                    case "exit":
                    case "quit":
                        Console.WriteLine("👋 Goodbye!");
                        return;
                    default:
                        Console.WriteLine("❌ Invalid option. Please try again.");
                        break;
                }

                if (choice != "5" && choice != "exit" && choice != "quit")
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in interactive client");
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }

    private static async Task ShowMainMenuAsync()
    {
        await Task.Delay(1); // Make async
        Console.Clear();
        Console.WriteLine("🔄 MCP Report Client SDK - Main Menu");
        Console.WriteLine("====================================");
        Console.WriteLine();
        Console.WriteLine("1. 📋 View Available Templates");
        Console.WriteLine("2. 📊 Generate Report");
        Console.WriteLine("3. 💚 Check Health Status");
        Console.WriteLine("4. 🚀 Run Demo");
        Console.WriteLine("5. 🚪 Exit");
        Console.WriteLine();
        Console.Write("Select an option: ");
    }

    private async Task ShowTemplatesAsync()
    {
        Console.Clear();
        Console.WriteLine("📋 Available Report Templates");
        Console.WriteLine("============================");
        Console.WriteLine();

        var result = await _mcpClient.GetReportTemplatesAsync();
        if (result == null)
        {
            Console.WriteLine("❌ Failed to retrieve templates");
            return;
        }

        foreach (var template in result.Templates)
        {
            Console.WriteLine($"🔸 ID: {template.Id}");
            Console.WriteLine($"   Name: {template.Name}");
            Console.WriteLine($"   Description: {template.Description}");
            Console.WriteLine($"   Formats: {string.Join(", ", template.SupportedFormats)}");
            
            if (template.RequiredParameters.Any())
            {
                Console.WriteLine("   Parameters:");
                foreach (var param in template.RequiredParameters)
                {
                    var required = param.Required ? " (required)" : " (optional)";
                    Console.WriteLine($"     • {param.Name} ({param.Type}){required}: {param.Description}");
                }
            }
            Console.WriteLine();
        }
    }

    private async Task GenerateReportInteractiveAsync()
    {
        Console.Clear();
        Console.WriteLine("📊 Generate Report");
        Console.WriteLine("==================");
        Console.WriteLine();

        // Show available templates
        var templatesResult = await _mcpClient.GetReportTemplatesAsync();
        if (templatesResult == null)
        {
            Console.WriteLine("❌ Failed to retrieve templates");
            return;
        }

        Console.WriteLine("Available templates:");
        for (int i = 0; i < templatesResult.Templates.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {templatesResult.Templates[i].Id} - {templatesResult.Templates[i].Name}");
        }
        Console.WriteLine();

        Console.Write("Enter template ID or number: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine("❌ No template selected");
            return;
        }

        ReportTemplate? selectedTemplate = null;
        if (int.TryParse(input, out int templateNumber) && templateNumber > 0 && templateNumber <= templatesResult.Templates.Count)
        {
            selectedTemplate = templatesResult.Templates[templateNumber - 1];
        }
        else
        {
            selectedTemplate = templatesResult.Templates.FirstOrDefault(t => t.Id.Equals(input, StringComparison.OrdinalIgnoreCase));
        }

        if (selectedTemplate == null)
        {
            Console.WriteLine("❌ Template not found");
            return;
        }

        Console.WriteLine($"\n📋 Selected template: {selectedTemplate.Name}");
        
        // Collect parameters
        var parameters = new Dictionary<string, object>();
        foreach (var param in selectedTemplate.RequiredParameters.Where(p => p.Required))
        {
            Console.Write($"Enter {param.Name} ({param.Description}): ");
            var value = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"❌ {param.Name} is required");
                return;
            }

            // Simple type conversion
            object convertedValue = param.Type.ToLower() switch
            {
                "number" => int.TryParse(value, out int intVal) ? intVal : value,
                "boolean" => bool.TryParse(value, out bool boolVal) ? boolVal : value,
                _ => value
            };

            parameters[param.Name] = convertedValue;
        }

        // Optional parameters
        foreach (var param in selectedTemplate.RequiredParameters.Where(p => !p.Required))
        {
            Console.Write($"Enter {param.Name} ({param.Description}) [optional]: ");
            var value = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(value))
            {
                object convertedValue = param.Type.ToLower() switch
                {
                    "number" => int.TryParse(value, out int intVal) ? intVal : value,
                    "boolean" => bool.TryParse(value, out bool boolVal) ? boolVal : value,
                    _ => value
                };

                parameters[param.Name] = convertedValue;
            }
        }

        // Select format
        Console.WriteLine($"\nAvailable formats: {string.Join(", ", selectedTemplate.SupportedFormats)}");
        Console.Write("Select format [pdf]: ");
        var format = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(format))
        {
            format = "pdf";
        }

        // Generate report
        Console.WriteLine("\n🔄 Generating report...");
        var result = await _mcpClient.GenerateReportAsync(selectedTemplate.Id, parameters, format, true);
        
        if (result == null)
        {
            Console.WriteLine("❌ Failed to generate report");
            return;
        }

        if (!result.Success)
        {
            Console.WriteLine($"❌ Report generation failed: {result.ErrorMessage}");
            return;
        }

        Console.WriteLine("✅ Report generated successfully!");
        Console.WriteLine($"📁 Filename: {result.Filename}");
        Console.WriteLine($"📏 Size: {result.Size} bytes");
        Console.WriteLine($"📄 MIME Type: {result.MimeType}");
        Console.WriteLine($"🕐 Generated at: {result.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");

        if (result.ReportData != null)
        {
            Console.WriteLine("\n📄 Report content preview:");
            var content = System.Text.Encoding.UTF8.GetString(result.ReportData);
            var lines = content.Split('\n').Take(10);
            foreach (var line in lines)
            {
                Console.WriteLine($"  {line}");
            }
            if (content.Split('\n').Length > 10)
            {
                Console.WriteLine("  ... (truncated)");
            }
        }
    }

    private async Task CheckHealthAsync()
    {
        Console.WriteLine("\n💚 Health Status Check");
        Console.WriteLine("=====================");
        
        var health = await _mcpClient.GetHealthStatusAsync();
        if (health == null)
        {
            Console.WriteLine("❌ Failed to get health status");
            return;
        }

        var statusEmoji = health.IsHealthy ? "✅" : "❌";
        Console.WriteLine($"{statusEmoji} Status: {health.Status}");
        Console.WriteLine($"🔢 Version: {health.Version}");
        Console.WriteLine($"🕐 Timestamp: {health.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        
        if (health.Details.Any())
        {
            Console.WriteLine("📋 Details:");
            foreach (var detail in health.Details)
            {
                Console.WriteLine($"   • {detail.Key}: {detail.Value}");
            }
        }
    }

    private async Task RunDemoAsync()
    {
        Console.Clear();
        Console.WriteLine("🚀 Running MCP Demo");
        Console.WriteLine("==================");
        Console.WriteLine();

        // Step 1: Health check
        Console.WriteLine("1. 💚 Checking server health...");
        await CheckHealthAsync();
        await Task.Delay(1000);

        // Step 2: Get templates
        Console.WriteLine("\n2. 📋 Fetching available templates...");
        var templates = await _mcpClient.GetReportTemplatesAsync();
        if (templates != null)
        {
            Console.WriteLine($"   ✅ Found {templates.Templates.Count} templates");
        }
        await Task.Delay(1000);

        // Step 3: Generate a sample report
        Console.WriteLine("\n3. 📊 Generating sample monthly report...");
        var parameters = new Dictionary<string, object>
        {
            ["month"] = 12,
            ["year"] = 2024,
            ["department"] = "Engineering"
        };

        var result = await _mcpClient.GenerateReportAsync("monthly-summary", parameters, "pdf", true);
        if (result?.Success == true)
        {
            Console.WriteLine($"   ✅ Report generated: {result.Filename} ({result.Size} bytes)");
        }
        else
        {
            Console.WriteLine($"   ❌ Failed: {result?.ErrorMessage}");
        }

        // Step 4: Generate another report
        Console.WriteLine("\n4. 📈 Generating quarterly performance report...");
        var quarterlyParams = new Dictionary<string, object>
        {
            ["quarter"] = 4,
            ["year"] = 2024,
            ["includeCharts"] = true
        };

        var quarterlyResult = await _mcpClient.GenerateReportAsync("quarterly-performance", quarterlyParams, "html", true);
        if (quarterlyResult?.Success == true)
        {
            Console.WriteLine($"   ✅ Report generated: {quarterlyResult.Filename} ({quarterlyResult.Size} bytes)");
        }
        else
        {
            Console.WriteLine($"   ❌ Failed: {quarterlyResult?.ErrorMessage}");
        }

        Console.WriteLine("\n🎉 Demo completed successfully!");
        Console.WriteLine("The MCP SDK client has successfully communicated with the server using the Microsoft Extensions AI framework.");
    }
}
