using AutoMapper;
using ReportServer.Abstraction.Contracts.Authentication;
using ReportServer.Abstraction.Contracts.Terminal;
using ReportServer.Abstraction.Contracts.FileServer;
using ReportServer.RestClient.DTOs.Authentication;
using ReportServer.RestClient.DTOs.Terminal;
using ReportServer.RestClient.DTOs.FileServer;

namespace ReportServer.RestClient.Mapper;

public class RestClientMappingProfile : Profile
{
    public RestClientMappingProfile()
    {
        // Authentication mappings
        CreateMap<UserDto, User>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username ?? string.Empty))
            .ForMember(dest => dest.Firstname, opt => opt.MapFrom(src => src.Firstname ?? string.Empty))
            .ForMember(dest => dest.Lastname, opt => opt.MapFrom(src => src.Lastname ?? string.Empty))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
            .ForMember(dest => dest.Properties, opt => opt.MapFrom(src => src.Properties ?? new Dictionary<string, string>()))
            .ForMember(dest => dest.Groups, opt => opt.MapFrom(src => src.Groups ?? new List<GroupDto>()));

        CreateMap<GroupDto, Group>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name ?? string.Empty))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? string.Empty));

        // Terminal mappings
        CreateMap<TerminalSessionInfoDto, TerminalSessionInfo>()
            .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => src.SessionId ?? string.Empty))
            .ForMember(dest => dest.Prompt, opt => opt.MapFrom(src => src.Prompt ?? string.Empty))
            .ForMember(dest => dest.WorkingDirectory, opt => opt.MapFrom(src => src.WorkingDirectory ?? string.Empty));

        CreateMap<CommandResultDto, CommandResult>()
            .ForMember(dest => dest.Result, opt => opt.MapFrom(src => src.Result ?? string.Empty))
            .ForMember(dest => dest.Error, opt => opt.MapFrom(src => src.Error ?? string.Empty))
            .ForMember(dest => dest.Data, opt => opt.MapFrom(src => src.Data ?? string.Empty))
            .ForMember(dest => dest.NewPrompt, opt => opt.MapFrom(src => src.NewPrompt ?? string.Empty));

        // File server mappings
        CreateMap<FileInfoDto, FileTreeNode>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0)) // REST API doesn't provide ID
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name ?? string.Empty))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Path ?? string.Empty))
            .ForMember(dest => dest.IsFolder, opt => opt.MapFrom(src => src.IsDirectory))
            .ForMember(dest => dest.Children, opt => opt.MapFrom(src => new List<FileTreeNode>()));

        // Reverse mappings for requests
        CreateMap<User, AuthenticationRequestDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Password, opt => opt.Ignore()); // Password not available in User object

        CreateMap<AbstractNode, AbstractNodeDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type));
    }
}
