using RSChatApp.Domain.ValueObjects;

namespace RSChatApp.Application.Core.Chat.Dtos;

public record ChatMessageDto(ChatRole role, string content);