using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.StartChat;

public record StartChatCommand(Guid Id, UserId UserId, string Title);
