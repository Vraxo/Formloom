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
        var fruits = ParseScene(prompt);
        if (fruits is null || fruits.Value.Length != 1)
        {
            return null;
        }

        return fruits.Value[0];
    }

    public static System.Collections.Immutable.ImmutableArray<FruitPrompt>? ParseScene(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return null;
        }

        var normalized = string.Join(" ", prompt.Trim().TrimEnd('.', '!', '?').Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        const string setting = " on a wooden table";
        if (!normalized.EndsWith(setting, StringComparison.Ordinal))
        {
            return null;
        }

        var list = System.Text.RegularExpressions.Regex.Replace(normalized[..^setting.Length], @"\s*,\s*", ", ");
        const string descriptor = @"(?:a (?:red|green|yellow) apple|a (?:green|yellow) pear|an orange)";
        var match = System.Text.RegularExpressions.Regex.Match(
            list,
            @"\A(?<fruit>" + descriptor + @")(?: and (?<fruit>" + descriptor + @")(?: and (?<fruit>" + descriptor + @"))?|, (?<fruit>" + descriptor + @"),? and (?<fruit>" + descriptor + @"))?\z");
        if (!match.Success)
        {
            return null;
        }

        var result = System.Collections.Immutable.ImmutableArray.CreateBuilder<FruitPrompt>();
        foreach (System.Text.RegularExpressions.Capture capture in match.Groups["fruit"].Captures)
        {
            result.Add(capture.Value switch
            {
                "a red apple" => new(FruitKind.Apple, FruitColor.Red),
                "a green apple" => new(FruitKind.Apple, FruitColor.Green),
                "a yellow apple" => new(FruitKind.Apple, FruitColor.Yellow),
                "a green pear" => new(FruitKind.Pear, FruitColor.Green),
                "a yellow pear" => new(FruitKind.Pear, FruitColor.Yellow),
                _ => new(FruitKind.Orange, FruitColor.Orange),
            });
        }

        return result.ToImmutable();
    }
}
