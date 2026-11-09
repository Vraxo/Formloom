using Formloom.Core.Ceramics;
using Formloom.Core.Fruits;

namespace Formloom.Core.Scenes;

public readonly record struct ObjectPrompt(FruitPrompt? Fruit, CeramicPrompt? Ceramic)
{
    public bool IsFruit => Fruit is not null;

    public string Key => Fruit is { } fruit
        ? $"{fruit.Kind}.{fruit.Color}"
        : $"{Ceramic!.Kind}.{Ceramic.Glaze}";

    public static ObjectPrompt FromFruit(FruitPrompt fruit) => new(fruit, null);

    public static ObjectPrompt FromCeramic(CeramicPrompt ceramic) => new(null, ceramic);
}
