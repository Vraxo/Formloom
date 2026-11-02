namespace Formloom.Core;

public static class PromptParser
{
    public static AppleColor? Parse(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return null;
        }

        var normalized = string.Join(" ", prompt.Trim().TrimEnd('.', '!', '?').Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        return normalized switch
        {
            "a red apple on a wooden table" => AppleColor.Red,
            "a green apple on a wooden table" => AppleColor.Green,
            "a yellow apple on a wooden table" => AppleColor.Yellow,
            _ => null,
        };
    }
}
