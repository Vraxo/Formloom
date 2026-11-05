using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Formloom.Core;

public static class PromptParser
{
    private const string FruitDescriptor = @"(?:a (?:red|green|yellow) apple|a (?:green|yellow) pear|an orange)";

    private const string CeramicDescriptor = @"a (?:blue|cream) ceramic (?:bowl|cup|vase)";

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

    public static ImmutableArray<FruitPrompt>? ParseScene(string? prompt)
    {
        var objects = ParseObjects(prompt);
        if (objects is null)
        {
            return null;
        }

        var fruits = ImmutableArray.CreateBuilder<FruitPrompt>(objects.Value.Length);
        foreach (var item in objects.Value)
        {
            if (item.Fruit is null)
            {
                return null;
            }

            fruits.Add(item.Fruit);
        }

        return fruits.MoveToImmutable();
    }

    public static ImmutableArray<ObjectPrompt>? ParseObjects(string? prompt)
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

        var list = Regex.Replace(normalized[..^setting.Length], @"\s*,\s*", ", ");
        const string descriptor = "(?:" + FruitDescriptor + "|" + CeramicDescriptor + ")";
        var match = Regex.Match(
            list,
            @"\A(?<item>" + descriptor + @")(?: and (?<item>" + descriptor + @")(?: and (?<item>" + descriptor + @"))?|, (?<item>" + descriptor + @"),? and (?<item>" + descriptor + @"))?\z");
        if (!match.Success)
        {
            return null;
        }

        var result = ImmutableArray.CreateBuilder<ObjectPrompt>();
        foreach (Capture capture in match.Groups["item"].Captures)
        {
            result.Add(ParseDescriptor(capture.Value));
        }

        return result.MoveToImmutable();
    }

    private static ObjectPrompt ParseDescriptor(string value) => value switch
    {
        "a red apple" => ObjectPrompt.FromFruit(new(FruitKind.Apple, FruitColor.Red)),
        "a green apple" => ObjectPrompt.FromFruit(new(FruitKind.Apple, FruitColor.Green)),
        "a yellow apple" => ObjectPrompt.FromFruit(new(FruitKind.Apple, FruitColor.Yellow)),
        "a green pear" => ObjectPrompt.FromFruit(new(FruitKind.Pear, FruitColor.Green)),
        "a yellow pear" => ObjectPrompt.FromFruit(new(FruitKind.Pear, FruitColor.Yellow)),
        "an orange" => ObjectPrompt.FromFruit(new(FruitKind.Orange, FruitColor.Orange)),
        _ => ObjectPrompt.FromCeramic(new(
            value.EndsWith("bowl", StringComparison.Ordinal) ? CeramicKind.Bowl
                : value.EndsWith("cup", StringComparison.Ordinal) ? CeramicKind.Cup
                : CeramicKind.Vase,
            value.StartsWith("a blue", StringComparison.Ordinal) ? CeramicGlaze.Blue : CeramicGlaze.Cream)),
    };
}
