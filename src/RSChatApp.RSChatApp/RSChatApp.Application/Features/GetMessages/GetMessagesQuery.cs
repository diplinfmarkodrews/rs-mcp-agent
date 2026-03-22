using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Features.GetMessages;

public record GetMessagesQuery(Guid SessionId, UserId UserId);
