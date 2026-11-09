using System.Numerics;
using Formloom.Core.Ceramics;
using Formloom.Core.Core;

namespace Formloom.Core.Tulips;

// The one vase-content relationship the grammar supports: a single tulip growing out of a
// single existing vase. The arrangement refers to the vase instance the scene already
// holds rather than duplicating it, so the vase keeps its kind, glaze, body, seed and
// depth, and the flower never becomes a second independently positioned scene object.
public sealed record VaseFlower(CeramicInstance Vase, FlowerKind Flower, TulipColor Color, TulipPlant Plant)
{
    public CeramicProfile Profile => VaseMouth.Profile(Vase);

    // Scene-space opening, derived from the vase transform and profile rather than from
    // canvas pixels.
    public Vector2 OpeningCenter => VaseMouth.Center(Vase, Profile);

    public Vector2 OpeningRadius => VaseMouth.Radius(Vase, Profile);

    // Topmost point of the opening. The flower stays above it, and it is the line the stem
    // disappears behind.
    public float MouthTop => OpeningCenter.Y - OpeningRadius.Y;

    public string SubjectToken =>
        Color.ToString().ToLowerInvariant() + "-tulip-in-" + Vase.Glaze.ToString().ToLowerInvariant() + "-vase";

    public static VaseFlower Create(CeramicInstance vase, TulipColor color, ulong seed, double variation)
    {
        ArgumentNullException.ThrowIfNull(vase);
        var planted = vase.Kind == CeramicKind.Vase ? vase : vase with { Kind = CeramicKind.Vase };
        var plant = TulipGeometry.Create(CeramicForms.For(planted.Kind), planted.Body, seed, variation);
        return new(planted, FlowerKind.Tulip, color, plant);
    }
}
