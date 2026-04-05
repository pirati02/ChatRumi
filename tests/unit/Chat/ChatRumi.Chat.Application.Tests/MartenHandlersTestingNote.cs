namespace ChatRumi.Chat.Application.Tests;

/// <summary>
/// Handlers that use Marten event streams and <c>IDocumentStore</c> are intended to be tested with
/// integration-style fixtures (in-memory document store or containers), not shallow mocks of <c>IDocumentSession</c>.
/// </summary>
internal static class MartenHandlersTestingNote;
