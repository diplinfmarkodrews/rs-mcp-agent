using RSChatApp.Application.Core.Chat;
using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.SendMessage;

public record SendMessageCommand(
    Guid Id,
    Guid SessionId,
    UserId SenderId,
    string? Content,
    ChatRole Role,
    AiChatRequest  AiChatRequest);
