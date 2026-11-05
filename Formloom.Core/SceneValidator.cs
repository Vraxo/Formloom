namespace Formloom.Core;

public static class SceneValidator
{
    public static string? Validate(StillLifeScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (scene.Width is < 64 or > 4096 || scene.Height is < 64 or > 4096 ||
            !double.IsFinite(scene.Variation) || scene.Variation is < 0 or > 1 ||
            !double.IsFinite(scene.TableHorizon) || scene.TableHorizon is < 0 or > 1 ||
            !double.IsFinite(scene.BackgroundWarmth) || scene.BackgroundWarmth is < 0 or > 1 ||
            !double.IsFinite(scene.GrainStrength) || scene.GrainStrength is < 0 or > 1 ||
            !float.IsFinite(scene.LightDirection.X) || !float.IsFinite(scene.LightDirection.Y))
        {
            return "Scene dimensions, variation and lighting must be finite and supported.";
        }

        if (scene.Instances is { IsDefault: true } || scene.Entries is { IsDefault: true })
        {
            return "A scene must contain one, two or three objects.";
        }

        var objects = scene.Objects;
        if (objects.Length is < 1 or > 3)
        {
            return "A scene must contain one, two or three objects.";
        }

        var margin = objects.Length == 1 ? 0 : 0.03f;
        foreach (var entry in objects)
        {
            if (entry.Fruit is { } fruit)
            {
                var error = ValidateFruit(scene, fruit);
                if (error is not null)
                {
                    return error;
                }
            }
            else if (entry.Ceramic is { } ceramic)
            {
                var error = ValidateCeramic(scene, ceramic);
                if (error is not null)
                {
                    return error;
                }
            }
            else
            {
                return "Every scene instance must describe a fruit or a ceramic vessel.";
            }

            var bounds = entry.Bounds;
            if (bounds.Left < margin || bounds.Top < margin || bounds.Right > 1 - margin || bounds.Bottom > 0.94f)
            {
                return "Complete object bounds must remain inside the safe canvas margins.";
            }
        }

        return null;
    }

    private static string? ValidateFruit(StillLifeScene scene, FruitInstance fruit)
    {
        if (fruit.Body is null)
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
            return "Every object must rest below the table horizon.";
        }

        return null;
    }

    private static string? ValidateCeramic(StillLifeScene scene, CeramicInstance ceramic)
    {
        if (ceramic.Body is null)
        {
            return "Every ceramic instance must describe a vessel body.";
        }

        var body = ceramic.Body;
        if (!Enum.IsDefined(ceramic.Kind) || !Enum.IsDefined(ceramic.Glaze) ||
            !float.IsFinite(body.Center.X) || !float.IsFinite(body.Center.Y) ||
            !float.IsFinite(body.Size.X) || !float.IsFinite(body.Size.Y) ||
            body.Size.X is < 0.1f or > 0.7f || body.Size.Y is < 0.1f or > 0.8f ||
            !double.IsFinite(body.SurfaceVariation) || body.SurfaceVariation is < 0 or > 1 ||
            ceramic.Depth is < 0 or > 3)
        {
            return "Ceramic transforms and appearance must be finite and within supported bounds.";
        }

        var bounds = CeramicBounds.For(ceramic);
        if (bounds.Bottom <= scene.TableHorizon)
        {
            return "Every object must rest below the table horizon.";
        }

        return null;
    }
}
