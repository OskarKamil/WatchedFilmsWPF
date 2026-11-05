namespace WatchedFilmsTracker.Source.Models;

public sealed record DelimiterOption(string DisplayName, string Value);

/// <summary>Formats offered by the collection delimiter selector.</summary>
public static class DelimiterOptions
{
    public static IReadOnlyList<DelimiterOption> All { get; } = Array.AsReadOnly(new[]
    {
        new DelimiterOption("Tab", "\t"),
        new DelimiterOption("Comma (,)", ","),
        new DelimiterOption("Semicolon (;)", ";"),
        new DelimiterOption("Pipe (|)", "|"),
    });

    public static bool IsSupported(string? delimiter) => All.Any(option => option.Value == delimiter);
}
