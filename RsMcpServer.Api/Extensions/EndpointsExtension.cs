using Microsoft.AspNetCore.Mvc;
using ReportServer.Abstraction;
using ReportServer.Abstraction.Contracts;
using ReportServer.Abstraction.Contracts.Terminal;

namespace RsMcpServer.Web.Extensions;

public static class EndpointsExtension
{
    public static IEndpointRouteBuilder MapRsRestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var rsRest = endpoints.MapGroup("/api/rs-rest")
            .WithTags("RsRest");

        // V1 endpoints (Legacy ReportServer authentication for direct access)
        var terminal = rsRest.MapGroup("/terminal")
            .WithTags("Terminal");

        terminal.MapPost("/init-session", InitTerminalSessionAsync)
            .WithName("InitTerminalSession")
            .WithSummary("Initialize a new terminal session to gain sessionId for subsequent terminal commands")
            .RequireAuthorization()
            .Produces<Result<TerminalSessionInfo>>();

        terminal.MapPost("/execute-command", ExecuteTerminalCommandAsync) 
            .WithName("ExecuteTerminalCommand")
            .WithSummary("Execute a terminal command using an existing terminal session identified by sessionId")
            .RequireAuthorization()
            .Produces<Result<CommandResult>>();
        
        terminal.MapPost("/close-session", CloseTerminalSessionAsync)
            .WithName("CloseTerminalSession")
            .WithSummary("Close an existing terminal session identified by sessionId")
            .RequireAuthorization()
            .Produces<Result>();
        return endpoints;
    }
    private static async Task<IResult> InitTerminalSessionAsync(
        [FromServices] IReportServerClient rsClient, HttpContext context)
    {
        var sessionInfo = await rsClient.InitSessionAsync();
        return Results.Ok(sessionInfo);
    }
    public record TerminalCommandRequest(string SessionId, string Command);
    private static async Task<IResult> ExecuteTerminalCommandAsync(
        [FromBody] TerminalCommandRequest request,
        [FromServices] IReportServerClient rsClient, HttpContext context)
    {
        var cmdResult = await rsClient.ExecuteAsync(
            request.SessionId, 
            request.Command, 
            context.RequestAborted);
        return Results.Ok(cmdResult);
    }
    
    private static async Task<IResult> CloseTerminalSessionAsync(
        [FromQuery] string sessionId,
        [FromServices] IReportServerClient rsClient, HttpContext context)
    {
        var closeResult = await rsClient.CloseSessionAsync(sessionId);
        return Results.Ok(closeResult);
    }
    
}