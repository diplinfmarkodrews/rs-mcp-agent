using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.CloseConversation;

public record CloseConversationCommand(Guid SessionId, long Version, UserId UserId);
