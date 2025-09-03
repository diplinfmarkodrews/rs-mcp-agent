using AutoMapper;
using ReportServer.Abstraction.Contracts.Authentication;
using ReportServer.RpcClient.DTOs.Authentication;

namespace ReportServer.RpcClient.Mapper;

public class AuthenticationMapperProfile : Profile
{
    public AuthenticationMapperProfile()
    {
        // 
        CreateMap<UserDto, User>()
            .ReverseMap();
        
        CreateMap<GroupDto, Group>()
            .ReverseMap();
        
        
    }
}