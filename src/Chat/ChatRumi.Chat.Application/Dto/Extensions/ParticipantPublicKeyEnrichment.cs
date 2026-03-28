using ChatRumi.Chat.Application.Dto.Response;

namespace ChatRumi.Chat.Application.Dto.Extensions;

public static class ParticipantPublicKeyEnrichment
{
    /// <summary>Merge rule: prefer Account value; if Account returned null for key, keep participant fallback.</summary>
    public static ParticipantDto MergePublicKey(this ParticipantDto dto, IReadOnlyDictionary<Guid, string?> lookup)
    {
        if (!lookup.TryGetValue(dto.Id, out var fromAccount))
            return dto;

        var merged = fromAccount ?? dto.PublicKey;
        return dto with { PublicKey = merged };
    }

    public static IEnumerable<Guid> CollectAccountIds(ChatResponse response)
    {
        foreach (var p in response.Participants)
            yield return p.Id;

        yield return response.Creator.Id;

        foreach (var m in response.Messages)
            yield return m.Sender.Id;
    }

    public static ChatResponse EnrichPublicKeys(this ChatResponse response, IReadOnlyDictionary<Guid, string?> lookup)
    {
        return response with
        {
            Participants = response.Participants.Select(p => p.MergePublicKey(lookup)).ToArray(),
            Creator = response.Creator.MergePublicKey(lookup),
            Messages = response.Messages
                .Select(m => m with { Sender = m.Sender.MergePublicKey(lookup) })
                .ToArray()
        };
    }

    public static IEnumerable<Guid> CollectAccountIds(LatestChatResponse response)
    {
        foreach (var p in response.Participants)
            yield return p.Id;

        if (response.Message is not null)
            yield return response.Message.Sender.Id;
    }

    public static LatestChatResponse EnrichPublicKeys(this LatestChatResponse response,
        IReadOnlyDictionary<Guid, string?> lookup)
    {
        return response with
        {
            Participants = response.Participants.Select(p => p.MergePublicKey(lookup)).ToList(),
            Message = response.Message is null
                ? null
                : response.Message with { Sender = response.Message.Sender.MergePublicKey(lookup) }
        };
    }
}
