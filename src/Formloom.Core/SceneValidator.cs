using System.Collections.Immutable;

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

        var containment = scene.Containment;
        var margin = objects.Length == 1 ? 0 : 0.03f;
        foreach (var entry in objects)
        {
            if (entry.Fruit is { } fruit)
            {
                // Contained fruit is held by the bowl, not by the table, so the horizon
                // rule belongs to the vessel that actually touches the surface.
                var contained = containment is not null && fruit == containment.Content;
                var error = ValidateFruit(scene, fruit, restsOnTable: !contained);
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

        if (containment is not null && scene.CupContents is not null)
        {
            return "A scene cannot be both a filled cup and a fruit inside a bowl.";
        }

        if (scene.VaseFlower is not null && (containment is not null || scene.CupContents is not null))
        {
            return "A scene cannot hold a flower and another contents relationship at the same time.";
        }

        if (scene.CupContents is { } contents)
        {
            return ValidateCupContents(objects, contents);
        }

        if (scene.VaseFlower is { } flower)
        {
            return ValidateVaseFlower(objects, flower);
        }

        return containment is null ? null : ValidateContainment(objects, containment);
    }

    // The tulip is contents of the scene's one vase, so the vase must be exactly the ceramic
    // instance the scene already holds, and the plant must hang from that vase's own mouth.
    private static string? ValidateVaseFlower(ImmutableArray<SceneEntry> objects, VaseFlower flower)
    {
        if (flower.Vase.Kind != CeramicKind.Vase)
        {
            return "Only a vase can hold a flower.";
        }

        if (!Enum.IsDefined(flower.Vase.Glaze))
        {
            return "A vase must have a blue or cream glaze.";
        }

        if (!Enum.IsDefined(flower.Flower))
        {
            return "Only a tulip is supported.";
        }

        if (!Enum.IsDefined(flower.Color))
        {
            return "A tulip must be red or yellow.";
        }

        if (objects.Length != 1 || objects[0].Ceramic != flower.Vase)
        {
            return "A flower scene must contain exactly that vase and no other object.";
        }

        var plant = flower.Plant;
        if (plant is null || plant.Leaves.IsDefault || plant.Bloom is null || plant.Bloom.Petals.IsDefault)
        {
            return "A vase flower must describe a stem, two leaves and a bloom.";
        }

        if (plant.Stem.IsDefaultOrEmpty || plant.Leaves.Length != 2 || plant.Bloom.Petals.Length is < 3 or > 5)
        {
            return "A vase flower must describe a stem, exactly two leaves and one bloom of three to five petals.";
        }

        if (!float.IsFinite(plant.StemThickness) || plant.StemThickness <= 0 ||
            !float.IsFinite(plant.Bloom.Width) || plant.Bloom.Width <= 0 ||
            !float.IsFinite(plant.Bloom.Height) || plant.Bloom.Height <= 0)
        {
            return "Flower geometry must be finite and positive.";
        }

        var bounds = plant.Bounds;
        if (!float.IsFinite(bounds.Left) || !float.IsFinite(bounds.Top) || !float.IsFinite(bounds.Right) || !float.IsFinite(bounds.Bottom))
        {
            return "Flower geometry must be finite and positive.";
        }

        var opening = flower.OpeningCenter;
        var radius = flower.OpeningRadius;
        var offsetX = (plant.StemBase.X - opening.X) / radius.X;
        var offsetY = (plant.StemBase.Y - opening.Y) / radius.Y;
        var reach = (offsetX * offsetX) + (offsetY * offsetY);
        if (reach > 0.95f || plant.StemBase.Y <= flower.MouthTop)
        {
            return "A stem must enter through the vase opening rather than the exterior wall.";
        }

        if (plant.StemTop.Y >= flower.MouthTop - 0.004f)
        {
            return "A stem must rise clear of the vase rim.";
        }

        if (plant.Bloom.Top >= flower.MouthTop - 0.01f || plant.Bloom.Base.Y >= flower.MouthTop - 0.004f)
        {
            return "A bloom must sit above the vase opening, clear of the rim.";
        }

        foreach (var leaf in plant.Leaves)
        {
            if (!float.IsFinite(leaf.Width) || leaf.Width <= 0 || leaf.Outline.IsDefault || leaf.Vein.IsDefault)
            {
                return "Every leaf must describe a finite blade and vein.";
            }

            if (leaf.Attachment.Y >= flower.MouthTop - 0.004f || leaf.Tip.Y >= flower.MouthTop - 0.004f)
            {
                return "Every leaf must stay above the vase opening.";
            }
        }

        if (bounds.Top < TulipGeometry.TopMargin - 0.012f || bounds.Left < 0.02f || bounds.Right > 0.98f ||
            bounds.Bottom > plant.StemBase.Y + (plant.StemThickness / 2) + 0.001f)
        {
            return "Flower bounds must remain inside the safe canvas margins.";
        }

        if (plant.Bloom.Width > radius.X * 2.5f || plant.Bloom.Height > 0.16f || plant.StemThickness > radius.X * 0.3f)
        {
            return "A bloom and stem must stay proportionate to the vase opening.";
        }

        return null;
    }

    // The drink is contents of the scene's one cup, so the cup must be exactly the ceramic
    // instance the scene already holds and nothing else may sit in the scene.
    private static string? ValidateCupContents(ImmutableArray<SceneEntry> objects, CupContents contents)
    {
        if (contents.Cup.Kind != CeramicKind.Cup)
        {
            return "Only a cup can hold a beverage.";
        }

        if (!Enum.IsDefined(contents.Beverage))
        {
            return "A cup can only hold coffee or tea.";
        }

        if (objects.Length != 1 || objects[0].Ceramic != contents.Cup)
        {
            return "A filled cup scene must contain exactly that cup and no other object.";
        }

        var profile = contents.Profile;
        var surface = contents.Surface;
        var mouth = profile.MouthCenter;
        var radius = profile.MouthRadius;
        if (!float.IsFinite(surface.Center.X) || !float.IsFinite(surface.Center.Y) ||
            !float.IsFinite(surface.Radius.X) || !float.IsFinite(surface.Radius.Y) ||
            !float.IsFinite(surface.ClipRadius.X) || !float.IsFinite(surface.ClipRadius.Y) ||
            surface.Radius.X <= 0 || surface.Radius.Y <= 0 || surface.ClipRadius.X <= 0 || surface.ClipRadius.Y <= 0)
        {
            return "A liquid surface must have finite, positive bounds.";
        }

        var margin = profile.RimThickness;
        if (surface.ClipRadius.X >= radius.X - (margin * 0.5f) || surface.ClipRadius.Y >= radius.Y - (margin * 0.5f))
        {
            return "A liquid surface must stay inside the inner mouth, clear of the rim.";
        }

        if (surface.Top <= (mouth.Y - radius.Y) + (margin * 0.5f))
        {
            return "A liquid surface must sit below the lip, leaving the far wall visible.";
        }

        if (surface.Bottom >= mouth.Y + radius.Y)
        {
            return "A liquid surface must stay inside the cup opening.";
        }

        return null;
    }

    private static string? ValidateContainment(ImmutableArray<SceneEntry> objects, BowlContainment containment)
    {
        if (containment.Bowl.Kind != CeramicKind.Bowl)
        {
            return "Only a bowl can contain fruit.";
        }

        var hasBowl = false;
        var hasContent = false;
        foreach (var entry in objects)
        {
            hasBowl |= entry.Ceramic == containment.Bowl;
            hasContent |= entry.Fruit == containment.Content;
        }

        if (objects.Length != 2 || !hasBowl || !hasContent)
        {
            return "A containment scene must contain exactly one bowl and the fruit inside it.";
        }

        if (containment.Content.Depth != containment.Bowl.Depth + 1)
        {
            return "A contained fruit must sit directly inside its bowl.";
        }

        if (containment.OccludedFraction is < 0.15f or > 0.55f)
        {
            return "A contained fruit must be seated with its base behind the near rim.";
        }

        if (containment.OpeningCoverage is < 0.25f or > 0.8f)
        {
            return "A contained fruit must fit inside the bowl's opening.";
        }

        if (Math.Abs(containment.Content.Body.Center.X - containment.OpeningCenter.X) + (containment.Content.Body.Size.X / 2) > containment.OpeningRadius.X)
        {
            return "A contained fruit must keep a horizontal margin inside the bowl's inner width.";
        }

        return null;
    }

    private static string? ValidateFruit(StillLifeScene scene, FruitInstance fruit, bool restsOnTable = true)
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

        if (restsOnTable && body.Center.Y + (body.Size.Y / 2) <= scene.TableHorizon)
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
