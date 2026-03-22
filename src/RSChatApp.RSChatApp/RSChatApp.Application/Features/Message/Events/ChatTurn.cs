using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.Message.Events;

public record ChatTurn(ChatRole Role, string Content);
