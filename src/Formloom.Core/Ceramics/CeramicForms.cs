using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;

namespace Formloom.Core.Ceramics;

// The three ceramic silhouettes. Geometry is fixed and normalized: only glaze, seed
// and placement vary per instance, so blue and cream share the same construction.
public static class CeramicForms
{
    private static readonly CeramicProfile BowlProfile = new(
        CeramicKind.Bowl,
        CurveBuilder.ClosedCubic(
        [
            new(0.03f, 0.20f),
            new(0.055f, 0.33f),
            new(0.09f, 0.47f),
            new(0.15f, 0.60f),
            new(0.26f, 0.75f),
            new(0.38f, 0.845f),
            new(0.50f, 0.862f),
            new(0.62f, 0.845f),
            new(0.74f, 0.75f),
            new(0.85f, 0.60f),
            new(0.91f, 0.47f),
            new(0.945f, 0.33f),
            new(0.97f, 0.20f),
            new(0.86f, 0.13f),
            new(0.68f, 0.105f),
            new(0.50f, 0.098f),
            new(0.32f, 0.105f),
            new(0.14f, 0.13f),
        ]),
        new Vector2(0.5f, 0.20f),
        new Vector2(0.47f, 0.085f),
        0.06f,
        0.012f,
        0.862f,
        0.24f,
        null);

    private static readonly CeramicProfile CupProfile = new(
        CeramicKind.Cup,
        CurveBuilder.ClosedCubic(
        [
            new(0.20f, 0.10f),
            new(0.195f, 0.24f),
            new(0.20f, 0.40f),
            new(0.215f, 0.56f),
            new(0.235f, 0.70f),
            new(0.27f, 0.83f),
            new(0.36f, 0.846f),
            new(0.50f, 0.85f),
            new(0.64f, 0.846f),
            new(0.73f, 0.83f),
            new(0.765f, 0.70f),
            new(0.785f, 0.56f),
            new(0.80f, 0.40f),
            new(0.805f, 0.24f),
            new(0.80f, 0.10f),
            new(0.72f, 0.06f),
            new(0.61f, 0.048f),
            new(0.50f, 0.045f),
            new(0.39f, 0.048f),
            new(0.28f, 0.06f),
            new(0.22f, 0.078f),
        ]),
        new Vector2(0.5f, 0.10f),
        new Vector2(0.30f, 0.055f),
        0.05f,
        0.010f,
        0.85f,
        0.46f,
        new(new Vector2(0.825f, 0.40f), new Vector2(0.155f, 0.255f), new Vector2(0.088f, 0.168f), new Vector2(0.795f, 0.27f), new Vector2(0.79f, 0.53f)));

    private static readonly CeramicProfile VaseProfile = new(
        CeramicKind.Vase,
        CurveBuilder.ClosedCubic(
        [
            new(0.36f, 0.115f),
            new(0.345f, 0.155f),
            new(0.385f, 0.215f),
            new(0.39f, 0.27f),
            new(0.30f, 0.345f),
            new(0.185f, 0.47f),
            new(0.16f, 0.575f),
            new(0.20f, 0.70f),
            new(0.285f, 0.80f),
            new(0.315f, 0.865f),
            new(0.36f, 0.895f),
            new(0.50f, 0.90f),
            new(0.64f, 0.895f),
            new(0.685f, 0.865f),
            new(0.715f, 0.80f),
            new(0.80f, 0.70f),
            new(0.84f, 0.575f),
            new(0.815f, 0.47f),
            new(0.70f, 0.345f),
            new(0.61f, 0.27f),
            new(0.615f, 0.215f),
            new(0.655f, 0.155f),
            new(0.64f, 0.115f),
            new(0.575f, 0.085f),
            new(0.50f, 0.078f),
            new(0.425f, 0.085f),
        ]),
        new Vector2(0.5f, 0.115f),
        new Vector2(0.14f, 0.04f),
        0.035f,
        0.012f,
        0.90f,
        0.37f,
        null);

    public static CeramicProfile For(CeramicKind kind) => kind switch
    {
        CeramicKind.Bowl => BowlProfile,
        CeramicKind.Cup => CupProfile,
        CeramicKind.Vase => VaseProfile,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static ImmutableArray<CeramicKind> Kinds { get; } =
        [CeramicKind.Bowl, CeramicKind.Cup, CeramicKind.Vase];

    public static ImmutableArray<CeramicGlaze> Glazes { get; } =
        [CeramicGlaze.Blue, CeramicGlaze.Cream];

    internal static ProfileExtent OutlineExtent(ImmutableArray<CubicSegment> outline) => CurveBuilder.Extent(outline);
}
