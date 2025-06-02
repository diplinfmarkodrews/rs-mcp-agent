using AutoMapper;

namespace ReportServerRPCClient.Mapper;

public class TerminalMapperProfile: Profile
{
    public TerminalMapperProfile()
    {
        CreateMap<DTOs.Terminal.AbstractNodeDto, ReportServerPort.Contracts.Terminal.AbstractNode>()
            .ReverseMap();
        
        CreateMap<DTOs.Terminal.CommandResultDto, ReportServerPort.Contracts.Terminal.CommandResult>()
            .ReverseMap();
        
        CreateMap<DTOs.Terminal.TerminalSessionInfoDto, ReportServerPort.Contracts.Terminal.TerminalSessionInfo>()
            .ReverseMap();
    }
}