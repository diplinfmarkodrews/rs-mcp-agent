using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.StartChat;

public record StartChatCommand(Guid Id, UserId UserId, Guid? ParentSessionId = null);
