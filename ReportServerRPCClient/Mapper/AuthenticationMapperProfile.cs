using AutoMapper;
using ReportServerPort.Contracts;
using ReportServerRPCClient.DTOs.Authentication;

namespace ReportServerRPCClient.Mapper;

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