using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Formloom.Core;

public static class PromptParser
{
    private const string FruitDescriptor = @"(?:a (?:red|green|yellow) apple|a (?:green|yellow) pear|an orange)";

    private const string CeramicDescriptor = @"a (?:blue|cream) ceramic (?:bowl|cup|vase)";

    private const string CupDescriptor = @"a (?:blue|cream) ceramic cup";

    private const string BeverageDescriptor = @"(?:coffee|tea)";

    private const string TulipDescriptor = @"a (?:red|yellow) tulip";

    // The daisy's appearance is not a colour word the grammar varies: white petals and a
    // golden centre are the only daisy the vocabulary names.
    private const string DaisyDescriptor = @"a white daisy";

    private const string VaseDescriptor = @"a (?:blue|cream) ceramic vase";

    private const string Setting = " on a wooden table";

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

        return fruits.ToImmutable();
    }

    // A single fruit contained by a single bowl. Roles are typed, so a caller cannot
    // mistake the content for a neighbour, and anything wider than the exact
    // "<fruit> in <bowl> on a wooden table" shape is rejected outright.
    public static ContainmentPrompt? ParseContainment(string? prompt)
    {
        var normalized = Normalize(prompt);
        if (normalized is null)
        {
            return null;
        }

        var match = Regex.Match(
            normalized[..^Setting.Length],
            @"\A(?<fruit>" + FruitDescriptor + @") in (?<bowl>" + CeramicDescriptor + @")\z");
        if (!match.Success)
        {
            return null;
        }

        var bowl = ParseDescriptor(match.Groups["bowl"].Value).Ceramic;
        if (bowl is null || bowl.Kind != CeramicKind.Bowl)
        {
            return null;
        }

        return new(ParseDescriptor(match.Groups["fruit"].Value).Fruit!, bowl);
    }

    // A single existing cup filled with a single still beverage. The exact grammar is
    // "<cup> filled with <beverage> on a wooden table"; anything else is rejected whole
    // rather than being read as an ordinary empty cup.
    public static CupContentsPrompt? ParseCupContents(string? prompt)
    {
        var normalized = Normalize(prompt);
        if (normalized is null)
        {
            return null;
        }

        var match = Regex.Match(
            normalized[..^Setting.Length],
            @"\A(?<cup>" + CupDescriptor + @") filled with (?<beverage>" + BeverageDescriptor + @")\z");
        if (!match.Success)
        {
            return null;
        }

        var cup = ParseDescriptor(match.Groups["cup"].Value).Ceramic;
        if (cup is null || cup.Kind != CeramicKind.Cup)
        {
            return null;
        }

        var beverage = match.Groups["beverage"].Value == "coffee" ? BeverageKind.Coffee : BeverageKind.Tea;
        return new(cup, beverage);
    }

    // A single tulip growing out of a single existing vase. The exact grammar is
    // "<tulip> in <vase> on a wooden table"; anything else is rejected whole rather than
    // being read as an ordinary empty vase.
    public static VaseFlowerPrompt? ParseVaseFlower(string? prompt)
    {
        var normalized = Normalize(prompt);
        if (normalized is null)
        {
            return null;
        }

        var match = Regex.Match(
            normalized[..^Setting.Length],
            @"\A(?<flower>" + TulipDescriptor + @") in (?<vase>" + VaseDescriptor + @")\z");
        if (!match.Success)
        {
            return null;
        }

        var vase = ParseDescriptor(match.Groups["vase"].Value).Ceramic;
        if (vase is null || vase.Kind != CeramicKind.Vase)
        {
            return null;
        }

        var color = match.Groups["flower"].Value == "a red tulip" ? TulipColor.Red : TulipColor.Yellow;
        return new(vase, FlowerKind.Tulip, color);
    }

    // A single white daisy growing out of a single existing vase. The exact grammar is
    // "a white daisy in <vase> on a wooden table"; any other colour, species or quantity is
    // rejected whole rather than being read as an ordinary empty vase.
    public static VaseDaisyPrompt? ParseVaseDaisy(string? prompt)
    {
        var normalized = Normalize(prompt);
        if (normalized is null)
        {
            return null;
        }

        var match = Regex.Match(
            normalized[..^Setting.Length],
            @"\A(?<flower>" + DaisyDescriptor + @") in (?<vase>" + VaseDescriptor + @")\z");
        if (!match.Success)
        {
            return null;
        }

        var vase = ParseDescriptor(match.Groups["vase"].Value).Ceramic;
        if (vase is null || vase.Kind != CeramicKind.Vase)
        {
            return null;
        }

        return new(vase, DaisyAppearance.WhiteWithGoldenDisk);
    }

    public static ImmutableArray<ObjectPrompt>? ParseObjects(string? prompt)
    {
        var normalized = Normalize(prompt);
        if (normalized is null)
        {
            return null;
        }

        var list = Regex.Replace(normalized[..^Setting.Length], @"\s*,\s*", ", ");
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

        return result.ToImmutable();
    }

    // Shared prompt normalization; returns null unless the prompt names the table setting.
    private static string? Normalize(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return null;
        }

        var normalized = string.Join(" ", prompt.Trim().TrimEnd('.', '!', '?').Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        return normalized.EndsWith(Setting, StringComparison.Ordinal) ? normalized : null;
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
