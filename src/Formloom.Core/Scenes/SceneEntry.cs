using System.Numerics;
using Formloom.Core.Ceramics;
using Formloom.Core.Core;
using Formloom.Core.Fruits;

namespace Formloom.Core.Scenes;

// Closed two-case projection over the scene's fruit and ceramic instances. This keeps
// ordering, depth and bounds handling explicit without a generic object registry.
public readonly record struct SceneEntry(FruitInstance? Fruit, CeramicInstance? Ceramic)
{
    public bool IsFruit => Fruit is not null;

    public bool IsCeramic => Ceramic is not null;

    public int Depth => Fruit?.Depth ?? Ceramic!.Depth;

    public Vector2 Center => Fruit?.Body.Center ?? Ceramic!.Body.Center;

    public Vector2 Size => Fruit?.Body.Size ?? Ceramic!.Body.Size;

    public SceneBounds Bounds => Fruit is { } fruit
        ? SceneBounds.From(FruitBounds.For(fruit))
        : SceneBounds.From(CeramicBounds.For(Ceramic!));

    // Distance from the transform center to the table contact, as a fraction of height.
    public float GroundOffset => Fruit is not null
        ? 0.5f
        : CeramicForms.For(Ceramic!.Kind).BaseBottom - 0.5f;

    // How far the visible silhouette's center sits right of the transform center.
    public float HorizontalShift
    {
        get
        {
            if (Fruit is not null)
            {
                return 0;
            }

            var profile = CeramicForms.For(Ceramic!.Kind);
            return ((profile.OutlineLeft + profile.OutlineRight) / 2) - 0.5f;
        }
    }

    public static SceneEntry ForFruit(FruitInstance fruit) => new(fruit, null);

    public static SceneEntry ForCeramic(CeramicInstance ceramic) => new(null, ceramic);

    public string DescriptorKey => Fruit is { } fruit
        ? $"{fruit.Subject}.{fruit.Color}"
        : $"{Ceramic!.Kind}.{Ceramic.Glaze}";

    public SceneEntry WithCenter(Vector2 center, Vector2 size, int depth) => Fruit is { } fruit
        ? ForFruit(fruit with { Body = fruit.Body with { Center = center, Size = size }, Depth = depth })
        : ForCeramic(Ceramic! with { Body = Ceramic.Body with { Center = center, Size = size }, Depth = depth });
}
