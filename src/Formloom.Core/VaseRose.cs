using System.Numerics;

namespace Formloom.Core;

/// <summary>A single red or pink rose as typed content of one existing vase.</summary>
public sealed record VaseRose(CeramicInstance Vase, RoseColor Color, RosePlant Plant)
{
    public FlowerKind Flower { get; init; } = FlowerKind.Rose;

    public CeramicProfile Profile => VaseMouth.Profile(Vase);

    public Vector2 OpeningCenter => VaseMouth.Center(Vase, Profile);

    public Vector2 OpeningRadius => VaseMouth.Radius(Vase, Profile);

    public float MouthTop => OpeningCenter.Y - OpeningRadius.Y;

    public string SubjectToken =>
        Color.ToString().ToLowerInvariant() + "-rose-in-" + Vase.Glaze.ToString().ToLowerInvariant() + "-vase";

    public static VaseRose Create(CeramicInstance vase, RoseColor color, ulong seed, double variation)
    {
        ArgumentNullException.ThrowIfNull(vase);
        var profile = CeramicForms.For(CeramicKind.Vase);
        var plant = RoseGeometry.Create(profile, vase.Body, seed, variation);
        return new(vase, color, plant);
    }
}
