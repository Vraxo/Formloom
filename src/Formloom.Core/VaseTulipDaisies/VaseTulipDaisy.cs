using System.Numerics;
using Formloom.Core.Ceramics;
using Formloom.Core.Core;
using Formloom.Core.Daisies;
using Formloom.Core.Tulips;

namespace Formloom.Core.VaseTulipDaisies;

// One fixed tulip-and-daisy arrangement as typed contents of one existing vase.
// The plants retain their species-specific geometry and the vase remains the only scene entry.
public sealed record VaseTulipDaisy(
    CeramicInstance Vase,
    TulipColor TulipColor,
    DaisyAppearance DaisyAppearance,
    TulipPlant Tulip,
    DaisyPlant Daisy)
{
    public CeramicProfile Profile => VaseMouth.Profile(Vase);

    public Vector2 OpeningCenter => VaseMouth.Center(Vase, Profile);

    public Vector2 OpeningRadius => VaseMouth.Radius(Vase, Profile);

    public float MouthTop => OpeningCenter.Y - OpeningRadius.Y;

    public SceneBounds Bounds => new(
        MathF.Min(Tulip.Bounds.Left, Daisy.Bounds.Left),
        MathF.Min(Tulip.Bounds.Top, Daisy.Bounds.Top),
        MathF.Max(Tulip.Bounds.Right, Daisy.Bounds.Right),
        MathF.Max(Tulip.Bounds.Bottom, Daisy.Bounds.Bottom));

    public string SubjectToken =>
        TulipColor.ToString().ToLowerInvariant() + "-tulip-and-white-daisy-in-" +
        Vase.Glaze.ToString().ToLowerInvariant() + "-vase";

    public static VaseTulipDaisy Create(
        CeramicInstance vase,
        TulipColor tulipColor,
        DaisyAppearance daisyAppearance,
        ulong seed,
        double variation)
    {
        ArgumentNullException.ThrowIfNull(vase);
        var planted = vase.Kind == CeramicKind.Vase ? vase : vase with { Kind = CeramicKind.Vase };
        var profile = CeramicForms.For(planted.Kind);
        var tulip = TulipGeometry.Create(profile, planted.Body, seed, variation);
        var daisy = DaisyGeometry.Create(profile, planted.Body, seed, variation);
        var placed = VaseTulipDaisyGeometry.Place(
            tulip,
            daisy,
            VaseMouth.Center(planted, profile),
            VaseMouth.Radius(planted, profile),
            seed,
            variation);

        return new(planted, tulipColor, daisyAppearance, placed.Tulip, placed.Daisy);
    }
}
