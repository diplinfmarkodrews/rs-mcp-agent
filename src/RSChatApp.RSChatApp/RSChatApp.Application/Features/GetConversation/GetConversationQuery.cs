using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.GetConversation;

public record GetConversationQuery(Guid SessionId, UserId UserId);
