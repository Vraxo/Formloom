namespace Formloom.Core;

public static class SceneValidator
{
    public static string? Validate(StillLifeScene scene)
    {
        if (scene.Width is < 64 or > 4096 || scene.Height is < 64 or > 4096 ||
            !double.IsFinite(scene.Variation) || scene.Variation is < 0 or > 1 ||
            !double.IsFinite(scene.TableHorizon) || scene.TableHorizon is < 0 or > 1 ||
            !double.IsFinite(scene.BackgroundWarmth) || scene.BackgroundWarmth is < 0 or > 1 ||
            !double.IsFinite(scene.GrainStrength) || scene.GrainStrength is < 0 or > 1 ||
            !float.IsFinite(scene.LightDirection.X) || !float.IsFinite(scene.LightDirection.Y))
        {
            return "Scene dimensions, variation and lighting must be finite and supported.";
        }

        if (scene.Instances is { IsDefault: true } || scene.Fruits.Length is < 1 or > 3)
        {
            return "A scene must contain one, two or three fruits.";
        }

        foreach (var fruit in scene.Fruits)
        {
            if (fruit is null || fruit.Body is null)
            {
                return "Every scene instance must describe a fruit body.";
            }

            var body = fruit.Body;
            if (!Enum.IsDefined(fruit.Subject) || !Enum.IsDefined(body.Color) ||
                !float.IsFinite(body.Center.X) || !float.IsFinite(body.Center.Y) ||
                !float.IsFinite(body.Size.X) || !float.IsFinite(body.Size.Y) ||
                body.Size.X is < 0.1f or > 0.6f || body.Size.Y is < 0.1f or > 0.65f ||
                !double.IsFinite(body.Asymmetry) || Math.Abs(body.Asymmetry) > 0.1 ||
                !double.IsFinite(body.StemLean) || Math.Abs(body.StemLean) > 0.5 ||
                !double.IsFinite(body.LeafAngle) || body.LeafAngle is < -0.8 or > 0 ||
                !double.IsFinite(body.SurfaceVariation) || body.SurfaceVariation is < 0 or > 1 ||
                fruit.Depth is < 0 or > 3)
            {
                return "Fruit transforms and appearance must be finite and within supported bounds.";
            }

            if (fruit.Subject == FruitKind.Pear && body.Color == AppleColor.Red)
            {
                return "Pears must be green or yellow.";
            }

            if (body.Center.Y + (body.Size.Y / 2) <= scene.TableHorizon)
            {
                return "Every fruit must rest below the table horizon.";
            }

            var bounds = FruitBounds.For(fruit);
            var margin = scene.Fruits.Length == 1 ? 0 : 0.03f;
            if (bounds.Left < margin || bounds.Top < margin || bounds.Right > 1 - margin || bounds.Bottom > 0.94f)
            {
                return "Complete fruit bounds must remain inside the safe canvas margins.";
            }
        }

        return null;
    }
}
