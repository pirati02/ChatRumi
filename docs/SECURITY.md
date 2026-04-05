# Security notes (OWASP-oriented)

## Supply chain

- Run `dotnet list package --vulnerable --include-transitive` regularly; CI lists vulnerable packages (non-blocking).
- Dependabot is configured for NuGet (see `.github/dependabot.yml`).

## SSRF (user-controlled URLs)

- Repository scan: no API handlers accept arbitrary HTTP(S) URLs for server-side `HttpClient` calls to user-supplied destinations.
- If you add outbound HTTP from user input (webhooks, import URL, previews), use an allowlist, block private/link-local ranges, and disable redirects or validate the final URL.

## SQL and injection

- Data access uses Marten/Npgsql with parameterized queries; Elasticsearch and Neo4j use driver APIs with bound parameters in application code paths reviewed for this checklist.
- Avoid introducing raw SQL built from request strings.

## Related implementation

- Security headers, CORS from configuration, rate limiting on auth endpoints, JWT object-level checks, telemetry redaction, and PBKDF2 password hashing with legacy migration are implemented in the application code; see recent changes in `ChatRumi.Infrastructure`, `ChatRum.InterCommunication`, and API `Program.cs` files.
