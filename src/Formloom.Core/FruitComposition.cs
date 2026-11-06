using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Randomness;

namespace Formloom.Core;

public static class FruitComposition
{
    public static ImmutableArray<FruitInstance> Arrange(ImmutableArray<FruitInstance> fruits, ulong seed, double variation)
    {
        if (fruits.IsDefault || fruits.Length is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(fruits));
        }

        if (!double.IsFinite(variation) || variation is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(variation));
        }

        if (fruits.Length == 1)
        {
            return fruits;
        }

        var random = Pcg32.ForStream(seed, "scene.composition");
        int[] slots = fruits.Length == 2 ? [0, 1] : [0, 1, 2];
        for (var index = slots.Length - 1; index > 0; index--)
        {
            var other = random.NextInt(0, index + 1);
            (slots[index], slots[other]) = (slots[other], slots[index]);
        }

        var result = ImmutableArray.CreateBuilder<FruitInstance>(fruits.Length);
        for (var index = 0; index < fruits.Length; index++)
        {
            var fruit = fruits[index];
            var slot = slots[index];
            var back = fruits.Length == 3 && slot == 1;
            var x = fruits.Length == 2 ? 0.33f + (slot * 0.34f) : 0.25f + (slot * 0.25f);
            x += (float)((random.NextDouble() - 0.5) * 0.012 * variation);
            var factor = back ? 0.61f : (fruits.Length == 2 ? 0.79f : 0.69f);
            var size = fruit.Body.Size * factor;
            var bottom = back ? 0.735f : 0.785f + (float)((random.NextDouble() - 0.5) * 0.012 * variation);
            result.Add(fruit with
            {
                Body = fruit.Body with { Center = new Vector2(x, bottom - (size.Y / 2)), Size = size },
                Depth = back ? 0 : slot + 1,
            });
        }

        return result.MoveToImmutable();
    }
}
