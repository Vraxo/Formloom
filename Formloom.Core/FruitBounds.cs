namespace Formloom.Core;

public readonly record struct FruitBounds(float Left, float Top, float Right, float Bottom)
{
    public static FruitBounds For(FruitInstance fruit)
    {
        var body = fruit.Body;
        var left = body.Center.X - (body.Size.X / 2);
        var top = body.Center.Y - (body.Size.Y / 2);
        return new(
            left - (body.Size.X * 0.08f),
            top - (body.Size.Y * (fruit.Subject == FruitKind.Orange ? 0.02f : 0.42f)),
            left + (body.Size.X * 1.1f),
            top + (body.Size.Y * 1.05f));
    }
}
