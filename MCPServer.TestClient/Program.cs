using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using MCPServer.Protos;

namespace MCPServer.TestClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("MCP Server Test Client");
            Console.WriteLine("=====================");
            
            // Create gRPC channel
            using var channel = GrpcChannel.ForAddress("http://localhost:5000");
            var client = new MCPService.MCPServiceClient(channel);
            
            // Health check
            try
            {
                Console.WriteLine("Checking MCP server health...");
                var healthResponse = await client.HealthCheckAsync(new HealthCheckRequest { Service = "mcp" });
                Console.WriteLine($"Server status: {healthResponse.Status}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health check failed: {ex.Message}");
                return;
            }
            
            // Test different chat queries
            await TestChat(client, "What can you do?");
            await TestChat(client, "What reports are available?");
            await TestChat(client, "Generate a monthly summary report for May 2025");
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        static async Task TestChat(MCPService.MCPServiceClient client, string query)
        {
            Console.WriteLine($"Query: {query}");
            Console.WriteLine("-------------------");
            
            try
            {
                var request = new ChatRequest
                {
                    Model = "gpt-3.5-turbo",
                    Temperature = 0.7f
                };
                
                // Add system message
                request.Messages.Add(new ChatMessage
                {
                    Role = Role.System,
                    Content = "You are a helpful assistant integrated with a report generation system."
                });
                
                // Add user query
                request.Messages.Add(new ChatMessage
                {
                    Role = Role.User,
                    Content = query
                });
                
                var response = await client.ChatAsync(request);
                Console.WriteLine($"Response: {response.Message.Content}");
                
                // Check if there's any report metadata
                if (response.Metadata.ContainsKey("reportId"))
                {
                    Console.WriteLine($"Report ID: {response.Metadata["reportId"]}");
                }
                
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
