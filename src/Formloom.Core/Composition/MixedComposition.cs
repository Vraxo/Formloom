using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Core;
using Formloom.Core.Scenes;

namespace Formloom.Core.Composition;

// Mixed and ceramic-only arrangements. Fruit-only scenes keep using FruitComposition so
// their established placements and random-stream consumption stay untouched.
public static class MixedComposition
{
    public static ImmutableArray<SceneEntry> Arrange(ImmutableArray<SceneEntry> entries, ulong seed, double variation)
    {
        if (entries.IsDefault || entries.Length is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(entries));
        }

        if (!double.IsFinite(variation) || variation is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(variation));
        }

        if (entries.Length == 1)
        {
            return entries;
        }

        var random = Pcg32.ForStream(seed, "scene.composition");
        int[] slots = entries.Length == 2 ? [0, 1] : [0, 1, 2];
        for (var index = slots.Length - 1; index > 0; index--)
        {
            var other = random.NextInt(0, index + 1);
            (slots[index], slots[other]) = (slots[other], slots[index]);
        }

        var result = ImmutableArray.CreateBuilder<SceneEntry>(entries.Length);
        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            var slot = slots[index];
            var back = entries.Length == 3 && slot == 1;
            var x = entries.Length == 2 ? 0.33f + (slot * 0.34f) : 0.24f + (slot * 0.26f);
            x += (float)((random.NextDouble() - 0.5) * 0.010 * variation);
            var factor = back ? 0.52f : entries.Length == 2 ? 0.72f : 0.60f;
            var size = entry.Size * factor;
            var bottom = back ? 0.735f : 0.79f + (float)((random.NextDouble() - 0.5) * 0.010 * variation);
            var center = new Vector2(
                x - (entry.HorizontalShift * size.X),
                bottom - (entry.GroundOffset * size.Y));
            result.Add(entry.WithCenter(center, size, back ? 0 : slot + 1));
        }

        return result.MoveToImmutable();
    }
}
