namespace ChatRumi.Account.Application.Tests;

/// <summary>
/// Command and query handlers that depend on Marten <c>IDocumentStore</c> are best covered by
/// integration tests (in-memory store or Testcontainers), not by unit tests that mock the full session API.
/// </summary>
internal static class MartenHandlersTestingNote;
