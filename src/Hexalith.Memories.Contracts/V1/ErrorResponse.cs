namespace Hexalith.Memories.Contracts.V1;

/// <summary>Standard API error response with an actionable suggestion.</summary>
public sealed record ErrorResponse(string Code, string Message, string Suggestion);
