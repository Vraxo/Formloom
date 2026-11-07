using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Randomness;

namespace Formloom.Core;

// The second flower the grammar supports, built from the vase's own normalized mouth.
//
// A daisy is not a wide tulip: its head is an open, radial arrangement of white petals around a
// golden disk, so it gets its own authored profile rather than being forced into the tulip's
// cupped-petal shapes. Every length is derived either from the usable headroom above the rim or
// from the mouth radius, so the plant scales with the vessel body and no canvas pixel offset is
// hard-coded. The petal count is fixed: variation may change the shape of a petal, never how
// many there are.
public static class DaisyGeometry
{
    // Free space kept above the bloom. The bloom top is placed relative to this margin, so the
    // flower can never reach the canvas edge however the vase body is scaled.
    public const float TopMargin = 0.036f;

    // Twelve petals are enough to read as a daisy at still-life size without turning into a
    // gear: the authored per-petal jitter below breaks the mechanical symmetry.
    public const int PetalCount = 12;

    private const float HeadroomShare = 0.34f;
    private const float StemSlenderness = 0.085f;
    private const float PetalHalfWidth = 0.28f;
    private const float LeafHalfWidth = 0.30f;
    private const float FlattenShare = 0.86f;
    private const float DiskShare = 0.34f;
    private const int StemSamples = 64;

    // Petal-local profile: x across the petal, y from the rounded tip (0) to the narrow root
    // that tucks under the disk (1). The outline is widest a third of the way out, so each
    // petal is tapered at both ends instead of being an oval spoke.
    private static readonly Vector2[] PetalShape =
    [
        new(0.50f, 0.00f),
        new(0.35f, 0.07f),
        new(0.25f, 0.25f),
        new(0.22f, 0.52f),
        new(0.29f, 0.79f),
        new(0.42f, 0.96f),
        new(0.50f, 1.00f),
        new(0.58f, 0.96f),
        new(0.71f, 0.79f),
        new(0.78f, 0.52f),
        new(0.75f, 0.25f),
        new(0.65f, 0.07f),
    ];

    // Leaf-local profile: the base is (0.5, 1), the tip (0.5, 0), and the spatulate blade is
    // widest past the middle, which is what separates a daisy leaf from the tulip's lance.
    private static readonly Vector2[] LeafBlade =
    [
        new(0.50f, 1.00f),
        new(0.33f, 0.72f),
        new(0.24f, 0.48f),
        new(0.24f, 0.26f),
        new(0.36f, 0.07f),
        new(0.50f, 0.00f),
        new(0.64f, 0.07f),
        new(0.76f, 0.26f),
        new(0.76f, 0.48f),
        new(0.67f, 0.72f),
    ];

    public static DaisyPlant Create(CeramicProfile profile, CeramicDescription body, ulong seed, double variation)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(body);
        var size = body.Size;
        var origin = new Vector2(body.Center.X - (size.X / 2), body.Center.Y - (size.Y / 2));
        var mouth = origin + (profile.MouthCenter * size);
        var mouthRadius = profile.MouthRadius * size;
        var rimTop = mouth.Y - mouthRadius.Y;

        // The usable headroom starts above the rim's own thickness, so the plant clears the
        // lip rather than growing out of the middle of it.
        var rimCrest = rimTop - (profile.RimThickness * size.Y);
        var headroom = MathF.Max(rimCrest - TopMargin, 0.02f);
        var amount = (float)variation;
        var stemRandom = Pcg32.ForStream(seed, "daisy.stem");
        var leafRandom = Pcg32.ForStream(seed, "daisy.leaves");
        var petalRandom = Pcg32.ForStream(seed, "daisy.petals");
        var diskRandom = Pcg32.ForStream(seed, "daisy.disk");

        // The head is sized first, then the stem is given exactly the height that leaves the
        // bloom under the top margin. A flat daisy head is wider than a tulip bloom but much
        // shorter, so it must not eat the whole headroom.
        var headRadius = MathF.Min(mouthRadius.X * 1.55f, headroom * HeadroomShare) *
            (1 + Jitter(petalRandom, 0.04f, amount));
        var stemVisible = headroom - ((headRadius * FlattenShare) + (headRadius * 0.10f));
        var sway = mouthRadius.X * Jitter(stemRandom, 0.24f, amount);
        var stemTop = new Vector2(mouth.X + sway, rimTop - stemVisible);
        var stemBase = new Vector2(mouth.X, mouth.Y + (mouthRadius.Y * 0.35f));
        var thickness = mouthRadius.X * StemSlenderness;
        var rise = stemBase.Y - stemTop.Y;
        var stemSegment = new CubicSegment(
            stemBase,
            new Vector2(stemBase.X + (sway * 0.22f), stemBase.Y - (rise * 0.30f)),
            new Vector2(stemTop.X - (sway * 0.50f), stemTop.Y + (rise * 0.36f)),
            stemTop);
        var stem = ImmutableArray.Create(stemSegment);

        // Exactly two leaves at different heights, splayed to either side so the head never
        // hides them and neither leaf crosses the stem.
        var leafLength = MathF.Min(stemVisible * 0.62f, mouthRadius.X * 2.1f);
        var leaves = ImmutableArray.Create(
            CreateLeaf(
                stemSegment,
                rimTop - (stemVisible * 0.34f),
                -(1.16f + Jitter(leafRandom, 0.14f, amount)),
                leafLength * (1 + Jitter(leafRandom, 0.06f, amount)),
                leafRandom),
            CreateLeaf(
                stemSegment,
                rimTop - (stemVisible * 0.72f),
                1.08f + Jitter(leafRandom, 0.14f, amount),
                leafLength * (1 + Jitter(leafRandom, 0.06f, amount)),
                leafRandom));

        var center = new Vector2(stemTop.X, stemTop.Y - (headRadius * 0.10f));
        var diskX = headRadius * DiskShare * (1 + Jitter(diskRandom, 0.03f, amount));
        var diskY = diskX * FlattenShare;
        var petals = CreatePetals(center, headRadius, diskX, petalRandom, amount);
        return new(
            stemBase,
            stemTop,
            thickness,
            stem,
            leaves,
            new DaisyBloom(center, headRadius, headRadius * FlattenShare, petals, new DaisyDisk(center, diskX, diskY)));
    }

    // Bounds measured from the drawn geometry: every stem, leaf and petal contour, the disk
    // ellipse, and half the stem stroke.
    public static SceneBounds Measure(DaisyPlant plant)
    {
        ArgumentNullException.ThrowIfNull(plant);
        var extent = new ProfileExtent(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
        extent = Union(extent, CurveBuilder.Extent(plant.Stem));
        foreach (var leaf in plant.Leaves)
        {
            extent = Union(extent, CurveBuilder.Extent(leaf.Outline));
        }

        foreach (var petal in plant.Bloom.Petals)
        {
            extent = Union(extent, CurveBuilder.Extent(petal.Outline));
        }

        extent = Union(
            extent,
            new ProfileExtent(
                plant.Bloom.Disk.Center.X - plant.Bloom.Disk.RadiusX,
                plant.Bloom.Disk.Center.Y - plant.Bloom.Disk.RadiusY,
                plant.Bloom.Disk.Center.X + plant.Bloom.Disk.RadiusX,
                plant.Bloom.Disk.Center.Y + plant.Bloom.Disk.RadiusY));
        var pad = MathF.Max(plant.StemThickness / 2, 0.002f);
        return new(extent.Left - pad, extent.Top - pad, extent.Right + pad, extent.Bottom + pad);
    }

    // Shortest distance from a point to the sampled stem curve. Tests use it to prove the
    // leaves are attached rather than floating beside the stem.
    public static float DistanceToStem(DaisyPlant plant, Vector2 point)
    {
        ArgumentNullException.ThrowIfNull(plant);
        var best = float.MaxValue;
        foreach (var segment in plant.Stem)
        {
            for (var step = 0; step <= StemSamples; step++)
            {
                var sample = segment.Evaluate(step / (float)StemSamples);
                best = MathF.Min(best, Vector2.Distance(sample, point));
            }
        }

        return best;
    }

    // The petals are authored around the disk in one pass and then ordered back to front, so
    // the upper half of the head sits behind the lower half and the disk closes the middle.
    private static ImmutableArray<DaisyPetal> CreatePetals(
        Vector2 center,
        float headRadius,
        float diskX,
        Pcg32 random,
        float amount)
    {
        var built = ImmutableArray.CreateBuilder<DaisyPetal>(PetalCount);
        var turn = MathF.Tau / PetalCount;
        for (var index = 0; index < PetalCount; index++)
        {
            var angle = (index * turn) + Jitter(random, turn * 0.18f, amount);
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var normal = new Vector2(-direction.Y, direction.X);
            var length = headRadius * (1 + Jitter(random, 0.07f, amount));
            var halfWidth = headRadius * 0.30f * (1 + Jitter(random, 0.06f, amount));
            var root = center + (direction * diskX * 0.55f);
            var points = new Vector2[PetalShape.Length];
            for (var step = 0; step < PetalShape.Length; step++)
            {
                var along = (1 - PetalShape[step].Y) * length;
                var across = ((PetalShape[step].X - 0.5f) / PetalHalfWidth) * halfWidth;
                var point = root + (direction * along) + (normal * across);
                points[step] = Flatten(center, point);
            }

            // The upper half of the head is the far half: darker, and drawn first.
            var tip = Flatten(center, root + (direction * length));
            var tone = direction.Y < 0 ? 0.62f + Jitter(random, 0.06f, 1f) : 0.90f + Jitter(random, 0.08f, 1f);
            built.Add(new DaisyPetal(
                CurveBuilder.ClosedCubic(points),
                angle,
                Math.Clamp(tone, 0f, 1f),
                Flatten(center, root),
                tip));
        }

        var ordered = ImmutableArray.CreateBuilder<DaisyPetal>(PetalCount);
        foreach (var petal in built.OrderBy(petal => MathF.Sin(petal.Angle)))
        {
            ordered.Add(petal);
        }

        return ordered.MoveToImmutable();
    }

    private static DaisyLeaf CreateLeaf(CubicSegment stem, float attachmentY, float angle, float length, Pcg32 random)
    {
        var attachment = PointNearY(stem, attachmentY);
        var direction = new Vector2(MathF.Sin(angle), -MathF.Cos(angle));
        var normal = new Vector2(-direction.Y, direction.X);
        var bow = length * Jitter(random, 0.14f, 1f);
        var bladeWidth = length * 0.30f;
        var points = new Vector2[LeafBlade.Length];
        for (var index = 0; index < LeafBlade.Length; index++)
        {
            points[index] = MapBlade(LeafBlade[index], attachment, direction, normal, length, bladeWidth, bow);
        }

        var veinStart = SpinePoint(attachment, direction, normal, length, bow, 0f);
        var veinMiddle = SpinePoint(attachment, direction, normal, length, bow, 0.40f);
        var veinEnd = SpinePoint(attachment, direction, normal, length, bow, 0.80f);
        var vein = new CubicSegment(
            veinStart,
            veinStart + ((veinMiddle - veinStart) * 1.4f),
            veinEnd - ((veinEnd - veinMiddle) * 1.4f),
            veinEnd);
        return new(
            attachment,
            SpinePoint(attachment, direction, normal, length, bow, 1f),
            bladeWidth,
            CurveBuilder.ClosedCubic(points),
            ImmutableArray.Create(vein));
    }

    private static Vector2 MapBlade(
        Vector2 point,
        Vector2 attachment,
        Vector2 direction,
        Vector2 normal,
        float length,
        float width,
        float bow)
    {
        var along = (1 - point.Y) * length;
        var across = (((point.X - 0.5f) / LeafHalfWidth) * (width * 0.5f)) + (bow * MathF.Sin(MathF.PI * (1 - point.Y)));
        return attachment + (direction * along) + (normal * across);
    }

    private static Vector2 SpinePoint(
        Vector2 attachment,
        Vector2 direction,
        Vector2 normal,
        float length,
        float bow,
        float t) =>
        attachment + (direction * (t * length)) + (normal * (bow * MathF.Sin(MathF.PI * t)));

    // The head is a flat disc seen slightly from the front, so every authored point is squashed
    // towards the centre vertically. One helper so the petals, their ends and the disk agree.
    private static Vector2 Flatten(Vector2 center, Vector2 point) =>
        new(point.X, center.Y + ((point.Y - center.Y) * FlattenShare));

    private static Vector2 PointNearY(CubicSegment stem, float y)
    {
        var best = stem.Start;
        var bestDistance = float.MaxValue;
        for (var step = 0; step <= StemSamples; step++)
        {
            var sample = stem.Evaluate(step / (float)StemSamples);
            var distance = MathF.Abs(sample.Y - y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = sample;
            }
        }

        return best;
    }

    private static ProfileExtent Union(ProfileExtent extent, ProfileExtent other) => new(
        MathF.Min(extent.Left, other.Left),
        MathF.Min(extent.Top, other.Top),
        MathF.Max(extent.Right, other.Right),
        MathF.Max(extent.Bottom, other.Bottom));

    private static float Jitter(Pcg32 random, float range, float amount) =>
        (((float)random.NextDouble() * 2) - 1) * range * amount;
}
