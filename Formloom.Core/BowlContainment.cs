using System.Numerics;

namespace Formloom.Core;

// The one containment relationship the grammar supports: a fruit resting inside a
// bowl. The content is a child of the bowl, not a scene sibling, so every existing
// fruit/vessel code path keeps handling a single object and only the renderer needs
// to know that the container is split into a rear and a front stage.
public sealed record BowlContainment(CeramicInstance Bowl, FruitInstance Content)
{
    public static BowlContainment Create(CeramicInstance bowl, FruitInstance content) =>
        new(bowl with { Kind = CeramicKind.Bowl }, content);

    public CeramicProfile Profile => CeramicForms.For(Bowl.Kind);

    // World-space box of the bowl in normalized scene space.
    public Vector2 BowlOrigin => new(Bowl.Body.Center.X - (Bowl.Body.Size.X / 2), Bowl.Body.Center.Y - (Bowl.Body.Size.Y / 2));

    public Vector2 OpeningCenter => new(
        BowlOrigin.X + (Profile.MouthCenter.X * Bowl.Body.Size.X),
        BowlOrigin.Y + (Profile.MouthCenter.Y * Bowl.Body.Size.Y));

    public Vector2 OpeningRadius => new(Profile.MouthRadius.X * Bowl.Body.Size.X, Profile.MouthRadius.Y * Bowl.Body.Size.Y);

    public float RearRimY => OpeningCenter.Y - OpeningRadius.Y;

    public float FrontRimY => OpeningCenter.Y + OpeningRadius.Y;

    // Content geometry expressed in the bowl's own normalized profile space.
    public Vector2 LocalCenter => new(
        (Content.Body.Center.X - BowlOrigin.X) / Bowl.Body.Size.X,
        (Content.Body.Center.Y - BowlOrigin.Y) / Bowl.Body.Size.Y);

    public Vector2 LocalSize => new(Content.Body.Size.X / Bowl.Body.Size.X, Content.Body.Size.Y / Bowl.Body.Size.Y);

    public float ContentBaseY => Content.Body.Center.Y + (Content.Body.Size.Y / 2);

    // Share of the content hidden behind the near wall. Published so tests can assert the
    // fruit is genuinely seated rather than balanced on the rim or buried out of sight.
    public float OccludedFraction
    {
        get
        {
            if (Content.Body.Size.Y <= 0f)
            {
                return 0f;
            }

            return Math.Clamp((ContentBaseY - FrontRimY) / Content.Body.Size.Y, 0f, 1f);
        }
    }

    public float VisibleFraction => 1f - OccludedFraction;

    // How much of the opening the content spans horizontally (0 = empty, 1 = rim to rim).
    public float OpeningCoverage => OpeningRadius.X <= 0f ? 0f : Content.Body.Size.X / (OpeningRadius.X * 2);

    public string SubjectToken =>
        Content.Subject.ToString().ToLowerInvariant() + "-in-" + Bowl.Glaze.ToString().ToLowerInvariant() + "-bowl";
}
