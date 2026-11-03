namespace Formloom.Core;

public static class PromptParser
{
    public static AppleColor? Parse(string? prompt)
    {
        var parsed = ParseFruit(prompt);
        if (parsed is null || parsed.Kind != FruitKind.Apple)
        {
            return null;
        }

        return (AppleColor)parsed.Color;
    }

    public static FruitPrompt? ParseFruit(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return null;
        }

        var normalized = string.Join(" ", prompt.Trim().TrimEnd('.', '!', '?').Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        return normalized switch
        {
            "a red apple on a wooden table" => new(FruitKind.Apple, FruitColor.Red),
            "a green apple on a wooden table" => new(FruitKind.Apple, FruitColor.Green),
            "a yellow apple on a wooden table" => new(FruitKind.Apple, FruitColor.Yellow),
            "a green pear on a wooden table" => new(FruitKind.Pear, FruitColor.Green),
            "a yellow pear on a wooden table" => new(FruitKind.Pear, FruitColor.Yellow),
            "an orange on a wooden table" => new(FruitKind.Orange, FruitColor.Orange),
            _ => null,
        };
    }
}
