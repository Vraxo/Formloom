using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;
using Formloom.Core.Daisies;
using Formloom.Core.Tulips;

namespace Formloom.Core.VaseTulipDaisies;

// A single, species-specific layout for one tulip and one daisy in one vase.
// The only shared operation is transforming the existing normalized geometry; each plant keeps
// its own generator, materials, and named geometry streams.
internal static class VaseTulipDaisyGeometry
{
    private const float PlantScale = 0.82f;
    private const float BaseOffsetShare = 0.22f;
    private const float BaseOffsetJitter = 0.025f;
    private const float HeadOffsetShare = 1.0f;
    private const float HeadOffsetJitter = 0.08f;

    public static (TulipPlant Tulip, DaisyPlant Daisy) Place(
        TulipPlant tulip,
        DaisyPlant daisy,
        Vector2 openingCenter,
        Vector2 openingRadius,
        ulong seed,
        double variation)
    {
        var layoutRandom = Pcg32.ForStream(seed, "vase.tulip-daisy.layout");
        var amount = (float)variation;
        var baseOffset = openingRadius.X * (BaseOffsetShare + Jitter(layoutRandom, BaseOffsetJitter, amount));
        var headOffset = openingRadius.X * (HeadOffsetShare + Jitter(layoutRandom, HeadOffsetJitter, amount));
        var tulipBase = new Vector2(openingCenter.X - baseOffset, tulip.StemBase.Y);
        var daisyBase = new Vector2(openingCenter.X + baseOffset, daisy.StemBase.Y);
        var tulipHeadDrift = -headOffset + baseOffset - ((tulip.StemTop.X - tulip.StemBase.X) * PlantScale);
        var daisyHeadDrift = headOffset - baseOffset - ((daisy.StemTop.X - daisy.StemBase.X) * PlantScale);

        return (
            PlaceTulip(tulip, tulipBase, tulipHeadDrift),
            PlaceDaisy(daisy, daisyBase, daisyHeadDrift));
    }

    private static TulipPlant PlaceTulip(TulipPlant plant, Vector2 targetBase, float headDrift)
    {
        Vector2 Transform(Vector2 point) => TransformPoint(point, plant.StemBase, plant.StemTop, targetBase, headDrift);
        Vector2 TransformHead(Vector2 point)
        {
            var targetTop = Transform(plant.StemTop);
            return targetTop + ((point - plant.StemTop) * PlantScale);
        }

        var stem = TransformSegments(plant.Stem, Transform);
        var leaves = plant.Leaves
            .Select(leaf => leaf with
            {
                Attachment = Transform(leaf.Attachment),
                Tip = Transform(leaf.Tip),
                Width = leaf.Width * PlantScale,
                Outline = TransformSegments(leaf.Outline, Transform),
                Vein = TransformSegments(leaf.Vein, Transform),
            })
            .ToImmutableArray();
        var petals = plant.Bloom.Petals
            .Select(petal => petal with { Outline = TransformSegments(petal.Outline, TransformHead) })
            .ToImmutableArray();
        var bloom = plant.Bloom with
        {
            Base = TransformHead(plant.Bloom.Base),
            Width = plant.Bloom.Width * PlantScale,
            Height = plant.Bloom.Height * PlantScale,
            Petals = petals,
        };

        return plant with
        {
            StemBase = Transform(plant.StemBase),
            StemTop = Transform(plant.StemTop),
            StemThickness = plant.StemThickness * PlantScale,
            Stem = stem,
            Leaves = leaves,
            Bloom = bloom,
        };
    }

    private static DaisyPlant PlaceDaisy(DaisyPlant plant, Vector2 targetBase, float headDrift)
    {
        Vector2 Transform(Vector2 point) => TransformPoint(point, plant.StemBase, plant.StemTop, targetBase, headDrift);
        Vector2 TransformHead(Vector2 point)
        {
            var targetTop = Transform(plant.StemTop);
            return targetTop + ((point - plant.StemTop) * PlantScale);
        }

        var stem = TransformSegments(plant.Stem, Transform);
        var leaves = plant.Leaves
            .Select(leaf => leaf with
            {
                Attachment = Transform(leaf.Attachment),
                Tip = Transform(leaf.Tip),
                Width = leaf.Width * PlantScale,
                Outline = TransformSegments(leaf.Outline, Transform),
                Vein = TransformSegments(leaf.Vein, Transform),
            })
            .ToImmutableArray();
        var petals = plant.Bloom.Petals
            .Select(petal => petal with
            {
                Outline = TransformSegments(petal.Outline, TransformHead),
                Root = TransformHead(petal.Root),
                Tip = TransformHead(petal.Tip),
            })
            .ToImmutableArray();
        var bloom = plant.Bloom with
        {
            Center = TransformHead(plant.Bloom.Center),
            RadiusX = plant.Bloom.RadiusX * PlantScale,
            RadiusY = plant.Bloom.RadiusY * PlantScale,
            Petals = petals,
            Disk = plant.Bloom.Disk with
            {
                Center = TransformHead(plant.Bloom.Disk.Center),
                RadiusX = plant.Bloom.Disk.RadiusX * PlantScale,
                RadiusY = plant.Bloom.Disk.RadiusY * PlantScale,
            },
        };

        return plant with
        {
            StemBase = Transform(plant.StemBase),
            StemTop = Transform(plant.StemTop),
            StemThickness = plant.StemThickness * PlantScale,
            Stem = stem,
            Leaves = leaves,
            Bloom = bloom,
        };
    }

    // Preserve the source plants' vertical rim clearance: stems and leaves keep their original
    // height, while TransformHead uniformly scales each bloom around its transformed stem top.
    private static Vector2 TransformPoint(
        Vector2 point,
        Vector2 originalBase,
        Vector2 originalTop,
        Vector2 targetBase,
        float headDrift)
    {
        var rise = originalBase.Y - originalTop.Y;
        var progress = rise > 0
            ? Math.Clamp((originalBase.Y - point.Y) / rise, 0f, 1f)
            : 0f;
        var x = targetBase.X + ((point.X - originalBase.X) * PlantScale) + (headDrift * progress);
        var y = targetBase.Y + (point.Y - originalBase.Y);
        return new Vector2(x, y);
    }

    private static ImmutableArray<CubicSegment> TransformSegments(
        ImmutableArray<CubicSegment> segments,
        Func<Vector2, Vector2> transform) =>
        segments
            .Select(segment => new CubicSegment(
                transform(segment.Start),
                transform(segment.Control1),
                transform(segment.Control2),
                transform(segment.End)))
            .ToImmutableArray();

    private static float Jitter(Pcg32 random, float range, float amount) =>
        (((float)random.NextDouble() * 2) - 1) * range * amount;
}
