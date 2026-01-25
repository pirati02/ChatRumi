﻿namespace ChatRumi.Chat.Domain.Aggregates;

public record Participant
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? NickName { get; init; }
    
    /// <summary>
    /// Public key for end-to-end encryption (Base64 encoded)
    /// </summary>
    public string? PublicKey { get; init; }
}