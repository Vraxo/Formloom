using System.Numerics;
using Formloom.Core.Randomness;

namespace Formloom.Core;

// Places a fruit inside a bowl. Everything is derived from the bowl's own transform, so
// the result is bowl-local and carries no scene-wide search: the content is scaled as a
// share of the bowl height, seated just below the near rim, and kept inside the inner
// width by a bounded horizontal drift.
public static class BowlContainmentPlacement
{
    // Dedicated stream: the containment layout never consumes the composition streams,
    // so single-object, group and mixed scenes are unaffected.
    public const string Stream = "containment.placement";

    public const string ContentStream = "containment.content";

    // Seed for the content, independent of the bowl's glaze so switching the bowl between
    // blue and cream reproduces the same fruit shape and surface detail.
    public static ulong ContentSeed(ulong seed)
    {
        var random = Pcg32.ForStream(seed, ContentStream);
        return ((ulong)random.NextUInt32() << 32) | random.NextUInt32();
    }

    public static BowlContainment Place(CeramicInstance bowl, FruitInstance content, ulong seed, double variation)
    {
        ArgumentNullException.ThrowIfNull(bowl);
        ArgumentNullException.ThrowIfNull(content);
        if (!double.IsFinite(variation) || variation is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(variation));
        }

        var random = Pcg32.ForStream(seed, Stream);
        var (heightShare, seat) = Fit(content.Subject);
        var container = BowlContainment.Create(bowl, content);

        // Three draws are always taken, so the layout stays deterministic even at
        // variation 0 where the offsets all collapse to zero.
        var heightScale = 1 + (Drift(random) * 0.04 * variation);
        var drift = (float)(Drift(random) * variation * 0.06 * container.OpeningRadius.X);
        var seatShift = (float)(Drift(random) * variation * 0.012 * bowl.Body.Size.Y);

        var aspect = content.Body.Size.X <= 0 ? 1f : Math.Max(0.4f, content.Body.Size.Y / content.Body.Size.X);
        var height = (float)(bowl.Body.Size.Y * heightShare * heightScale);
        var width = height / aspect;
        var baseY = container.FrontRimY + (seat * height) + seatShift;
        var center = new Vector2(container.OpeningCenter.X + drift, baseY - (height / 2));
        return new(container.Bowl, content with { Body = content.Body with { Center = center, Size = new Vector2(width, height) } });
    }

    // Height of the content as a share of the bowl's height, plus how much of the content
    // sinks behind the near wall. Pears stand taller and slimmer; oranges are rounder and
    // sit a little deeper.
    private static (float Height, float Seat) Fit(FruitKind subject) => subject switch
    {
        FruitKind.Pear => (0.74f, 0.32f),
        FruitKind.Orange => (0.60f, 0.40f),
        _ => (0.64f, 0.34f),
    };

    private static double Drift(Pcg32 random) => (random.NextDouble() * 2) - 1;
}
