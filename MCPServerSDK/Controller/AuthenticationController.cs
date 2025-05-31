// using ReportServerRPCClient;
//
// namespace MCPServerSDK.Controller;
//
// public class AuthenticationController
// {
//     private readonly ReportServerGwtRpcClient _reportServerClient;
//
//     public AuthenticationController(ReportServerGwtRpcClient reportServerClient)
//     {
//         _reportServerClient = reportServerClient;
//     }
//
//     [HttpPost("authenticate")]
//     public async Task<IActionResult> Authenticate([FromBody] LoginRequest request)
//     {
//         var result = await _reportServerClient.AuthenticateAsync(request.Username, request.Password);
//         return Ok(result);
//     }
// }