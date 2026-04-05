using System.Reflection;
using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.Events;
using ChatRumi.Chat.Domain.ValueObject;
using Xunit;
using ChatEntity = ChatRumi.Chat.Domain.Aggregates.Chat;

namespace ChatRumi.Chat.Domain.Tests;

public class ChatAggregateTests
{
    private static ChatEntity CreateEmptyChat()
    {
        var ctor = typeof(ChatEntity).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        Assert.NotNull(ctor);
        return (ChatEntity)ctor.Invoke(null);
    }

    [Fact]
    public void Apply_ChatStartedEvent_SetsIdentityAndParticipants()
    {
        var chat = CreateEmptyChat();
        var chatId = Guid.NewGuid();
        var creator = new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            NickName = "ada"
        };
        var participants = new List<Participant> { creator };

        var @event = new ChatStartedEvent
        {
            Id = chatId,
            Participants = participants,
            ChatName = "Project",
            Creator = creator
        };

        chat.Apply(@event);

        Assert.Equal(chatId, chat.Id);
        Assert.Equal("Project", chat.Name);
        Assert.Equal(creator, chat.Creator);
        Assert.Single(chat.Participants);
        Assert.True(chat.CreationDate <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Apply_ParticipantModifiedEvent_UpdatesExistingParticipant()
    {
        var chat = CreateEmptyChat();
        var participantId = Guid.NewGuid();
        var original = new Participant
        {
            Id = participantId,
            FirstName = "Old",
            LastName = "Name",
            NickName = "old"
        };
        chat.Apply(
            new ChatStartedEvent
            {
                Id = Guid.NewGuid(),
                Participants = [original],
                ChatName = "C",
                Creator = original
            });

        chat.Apply(new ParticipantModifiedEvent(participantId, "newuser", "New", "Name"));

        var updated = chat.Participants.Single();
        Assert.Equal("New", updated.FirstName);
        Assert.Equal("Name", updated.LastName);
        Assert.Equal("newuser", updated.NickName);
    }

    [Fact]
    public void Apply_ParticipantModifiedEvent_IgnoresUnknownParticipant()
    {
        var chat = CreateEmptyChat();
        var p = new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = "A",
            LastName = "B"
        };
        chat.Apply(
            new ChatStartedEvent
            {
                Id = Guid.NewGuid(),
                Participants = [p],
                ChatName = "C",
                Creator = p
            });

        chat.Apply(new ParticipantModifiedEvent(Guid.NewGuid(), "x", "Y", "Z"));

        Assert.Equal("A", chat.Participants[0].FirstName);
    }

    [Fact]
    public void Apply_MessageSentEvent_AppendsMessage()
    {
        var chat = CreateEmptyChat();
        var sender = new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = "S",
            LastName = "R"
        };
        chat.Apply(
            new ChatStartedEvent
            {
                Id = Guid.NewGuid(),
                Participants = [sender],
                ChatName = "C",
                Creator = sender
            });

        var content = new PlainTextContent { Content = "hello" };
        var messageId = Guid.NewGuid();
        var sent = new MessageSentEvent(chat.Id, sender, content, null) { Id = messageId };

        chat.Apply(sent);

        Assert.Single(chat.Messages);
        Assert.Equal(messageId, chat.Messages[0].Id);
        Assert.Equal(sender, chat.Messages[0].Participant);
        Assert.NotNull(chat.Messages[0].Sent);
    }

    [Fact]
    public void Apply_MessageStatusChangeEvent_UpdatesStatusWhenSenderMatches()
    {
        var chat = CreateEmptyChat();
        var sender = new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = "S",
            LastName = "R"
        };
        chat.Apply(
            new ChatStartedEvent
            {
                Id = Guid.NewGuid(),
                Participants = [sender],
                ChatName = "C",
                Creator = sender
            });

        var messageId = Guid.NewGuid();
        chat.Apply(new MessageSentEvent(chat.Id, sender, new PlainTextContent { Content = "hi" }, null) { Id = messageId });

        chat.Apply(
            new MessageStatusChangeEvent
            {
                MessageId = messageId,
                SenderId = sender,
                Status = MessageStatus.Delivered
            });

        Assert.NotNull(chat.Messages[0].Delivered);
    }

    [Fact]
    public void Apply_MessageStatusChangeEvent_IgnoresWhenSenderDoesNotMatchMessage()
    {
        var chat = CreateEmptyChat();
        var sender = new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = "S",
            LastName = "R"
        };
        var other = new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = "O",
            LastName = "T"
        };
        chat.Apply(
            new ChatStartedEvent
            {
                Id = Guid.NewGuid(),
                Participants = [sender, other],
                ChatName = "C",
                Creator = sender
            });

        var messageId = Guid.NewGuid();
        chat.Apply(new MessageSentEvent(chat.Id, sender, new PlainTextContent { Content = "hi" }, null) { Id = messageId });

        chat.Apply(
            new MessageStatusChangeEvent
            {
                MessageId = messageId,
                SenderId = other,
                Status = MessageStatus.Seen
            });

        Assert.Null(chat.Messages[0].Seen);
    }

    [Fact]
    public void Apply_MarkChatReadEvent_SetsDeliveredAndSeenOnMatchingMessages()
    {
        var chat = CreateEmptyChat();
        var sender = new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = "S",
            LastName = "R"
        };
        chat.Apply(
            new ChatStartedEvent
            {
                Id = Guid.NewGuid(),
                Participants = [sender],
                ChatName = "C",
                Creator = sender
            });

        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();
        chat.Apply(new MessageSentEvent(chat.Id, sender, new PlainTextContent { Content = "a" }, null) { Id = m1 });
        chat.Apply(new MessageSentEvent(chat.Id, sender, new PlainTextContent { Content = "b" }, null) { Id = m2 });

        chat.Apply(new MarkChatReadEvent { MessageIds = [m1] });

        Assert.NotNull(chat.Messages[0].Delivered);
        Assert.NotNull(chat.Messages[0].Seen);
        Assert.Null(chat.Messages[1].Delivered);
    }
}
