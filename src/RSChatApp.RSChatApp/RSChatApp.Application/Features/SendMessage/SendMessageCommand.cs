using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.SendMessage;

public record SendMessageCommand(Guid Id, Guid SessionId, UserId SenderId, string Content);
