using System.Numerics;
using Formloom.Core.Ceramics;
using Formloom.Core.Core;

namespace Formloom.Core.Daisies;

// The second vase content the grammar supports: a single white daisy with a golden centre
// growing out of a single existing vase. Like the tulip it refers to the vase instance the
// scene already holds rather than duplicating it, so the vessel keeps its kind, glaze, body,
// seed and depth and the flower never becomes an independently positioned scene object. It is
// a typed content of its own rather than a widened tulip, because the two species share no
// petal geometry and only the vase-derived attachment.
public sealed record VaseDaisy(CeramicInstance Vase, DaisyAppearance Appearance, DaisyPlant Plant)
{
    // A stored value rather than a computed one: it is part of the arrangement's identity, the
    // validator refuses anything but a daisy, and a computed constant would be a static member
    // wearing an instance's clothes.
    public FlowerKind Flower { get; init; } = FlowerKind.Daisy;

    public CeramicProfile Profile => VaseMouth.Profile(Vase);

    // Scene-space opening, derived from the vase transform and profile rather than from
    // canvas pixels.
    public Vector2 OpeningCenter => VaseMouth.Center(Vase, Profile);

    public Vector2 OpeningRadius => VaseMouth.Radius(Vase, Profile);

    // Topmost point of the opening. The flower stays above it, and it is the line the stem
    // disappears behind.
    public float MouthTop => OpeningCenter.Y - OpeningRadius.Y;

    public string SubjectToken => "white-daisy-in-" + Vase.Glaze.ToString().ToLowerInvariant() + "-vase";

    public static VaseDaisy Create(CeramicInstance vase, DaisyAppearance appearance, ulong seed, double variation)
    {
        ArgumentNullException.ThrowIfNull(vase);
        var planted = vase.Kind == CeramicKind.Vase ? vase : vase with { Kind = CeramicKind.Vase };
        var plant = DaisyGeometry.Create(CeramicForms.For(planted.Kind), planted.Body, seed, variation);
        return new(planted, appearance, plant);
    }
}
