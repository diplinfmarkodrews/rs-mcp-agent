using AutoMapper;
using ReportServer.Abstraction.Contracts.Terminal;

namespace ReportServer.RpcClient.Mapper;

public class TerminalMapperProfile: Profile
{
    public TerminalMapperProfile()
    {
        CreateMap<DTOs.Terminal.AbstractNodeDto, AbstractNode>()
            .ReverseMap();
        
        CreateMap<DTOs.Terminal.CommandResultDto, CommandResult>()
            .ReverseMap();
        
        CreateMap<DTOs.Terminal.TerminalSessionInfoDto, TerminalSessionInfo>()
            .ReverseMap();
    }
}