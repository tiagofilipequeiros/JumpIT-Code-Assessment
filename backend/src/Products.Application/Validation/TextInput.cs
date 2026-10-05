namespace Products.Application.Validation;

// Request text is trimmed as it is read, so validation (e.g. minimum length) applies to what is stored.
public static class TextInput
{
    public static string Trim(string? value) => value?.Trim() ?? string.Empty;

    public static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
