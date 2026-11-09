using System.Collections.Immutable;
using System.Numerics;

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

        var flower = scene.VaseFlower is not null || scene.VaseDaisy is not null ||
            scene.VaseTulipDaisy is not null || scene.VaseRose is not null;
        if (flower && (containment is not null || scene.CupContents is not null))
        {
            return "A scene cannot hold a flower and another contents relationship at the same time.";
        }

        var vaseFlowerRelationships = (scene.VaseFlower is null ? 0 : 1) +
            (scene.VaseDaisy is null ? 0 : 1) +
            (scene.VaseTulipDaisy is null ? 0 : 1) +
            (scene.VaseRose is null ? 0 : 1);
        if (vaseFlowerRelationships > 1)
        {
            return "A scene cannot hold two flower arrangements at the same time.";
        }

        if (scene.CupContents is { } contents)
        {
            return ValidateCupContents(objects, contents);
        }

        if (scene.VaseTulipDaisy is { } pair)
        {
            return ValidateVaseTulipDaisy(objects, pair);
        }

        if (scene.VaseFlower is { } tulip)
        {
            return ValidateVaseFlower(objects, tulip);
        }

        if (scene.VaseDaisy is { } daisy)
        {
            return ValidateVaseDaisy(objects, daisy);
        }

        if (scene.VaseRose is { } rose)
        {
            return ValidateVaseRose(objects, rose);
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

        if (flower.Flower != FlowerKind.Tulip)
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

    // The daisy is contents of the scene's one vase, so the vase must be exactly the ceramic
    // instance the scene already holds, and the plant must hang from that vase's own opening.
    // The petal count is fixed by the authored profile: variation may reshape a petal but may
    // never change how many there are.
    private static string? ValidateVaseDaisy(ImmutableArray<SceneEntry> objects, VaseDaisy daisy)
    {
        if (daisy.Vase.Kind != CeramicKind.Vase)
        {
            return "Only a vase can hold a flower.";
        }

        if (!Enum.IsDefined(daisy.Vase.Glaze))
        {
            return "A vase must have a blue or cream glaze.";
        }

        if (daisy.Flower != FlowerKind.Daisy)
        {
            return "Only a daisy is supported.";
        }

        if (!Enum.IsDefined(daisy.Appearance))
        {
            return "A daisy must have white petals and a golden centre.";
        }

        if (objects.Length != 1 || objects[0].Ceramic != daisy.Vase)
        {
            return "A flower scene must contain exactly that vase and no other object.";
        }

        var plant = daisy.Plant;
        if (plant is null || plant.Leaves.IsDefault || plant.Bloom is null || plant.Bloom.Petals.IsDefault)
        {
            return "A vase daisy must describe a stem, two leaves and a bloom.";
        }

        if (plant.Stem.IsDefaultOrEmpty || plant.Leaves.Length != 2 || plant.Bloom.Petals.Length != DaisyGeometry.PetalCount)
        {
            return "A vase daisy must describe a stem, exactly two leaves and one bloom of twelve petals.";
        }

        if (!float.IsFinite(plant.StemThickness) || plant.StemThickness <= 0 ||
            !float.IsFinite(plant.Bloom.RadiusX) || plant.Bloom.RadiusX <= 0 ||
            !float.IsFinite(plant.Bloom.RadiusY) || plant.Bloom.RadiusY <= 0 ||
            !float.IsFinite(plant.Bloom.Disk.RadiusX) || plant.Bloom.Disk.RadiusX <= 0 ||
            !float.IsFinite(plant.Bloom.Disk.RadiusY) || plant.Bloom.Disk.RadiusY <= 0)
        {
            return "Daisy geometry must be finite and positive.";
        }

        var bounds = plant.Bounds;
        if (!float.IsFinite(bounds.Left) || !float.IsFinite(bounds.Top) ||
            !float.IsFinite(bounds.Right) || !float.IsFinite(bounds.Bottom))
        {
            return "Daisy geometry must be finite and positive.";
        }

        var opening = daisy.OpeningCenter;
        var radius = daisy.OpeningRadius;
        var offsetX = (plant.StemBase.X - opening.X) / radius.X;
        var offsetY = (plant.StemBase.Y - opening.Y) / radius.Y;
        var reach = (offsetX * offsetX) + (offsetY * offsetY);
        if (reach > 0.95f || plant.StemBase.Y <= daisy.MouthTop)
        {
            return "A stem must enter through the vase opening rather than the exterior wall.";
        }

        if (plant.StemTop.Y >= daisy.MouthTop - 0.004f)
        {
            return "A stem must rise clear of the vase rim.";
        }

        if (plant.Bloom.Top >= daisy.MouthTop - 0.01f || plant.Bloom.Bottom >= daisy.MouthTop - 0.004f)
        {
            return "A bloom must sit above the vase opening, clear of the rim.";
        }

        foreach (var leaf in plant.Leaves)
        {
            if (!float.IsFinite(leaf.Width) || leaf.Width <= 0 || leaf.Outline.IsDefault || leaf.Vein.IsDefault)
            {
                return "Every leaf must describe a finite blade and vein.";
            }

            if (leaf.Attachment.Y >= daisy.MouthTop - 0.004f || leaf.Tip.Y >= daisy.MouthTop - 0.004f)
            {
                return "Every leaf must stay above the vase opening.";
            }
        }

        if (bounds.Top < DaisyGeometry.TopMargin - 0.012f || bounds.Left < 0.02f || bounds.Right > 0.98f ||
            bounds.Bottom > plant.StemBase.Y + (plant.StemThickness / 2) + 0.001f)
        {
            return "Flower bounds must remain inside the safe canvas margins.";
        }

        if (plant.Bloom.RadiusX * 2 > radius.X * 3.6f || plant.Bloom.RadiusY > 0.16f ||
            plant.StemThickness > radius.X * 0.3f)
        {
            return "A bloom and stem must stay proportionate to the vase opening.";
        }

        return null;
    }

    // A rose is the one typed content of the scene's existing vase. Its fixed layer counts,
    // stem attachment, leaf geometry and bounds are validated before the renderer sees it.
    private static string? ValidateVaseRose(ImmutableArray<SceneEntry> objects, VaseRose rose)
    {
        if (rose.Vase is null || rose.Vase.Kind != CeramicKind.Vase)
        {
            return "Only a vase can hold a rose.";
        }

        if (!Enum.IsDefined(rose.Vase.Glaze))
        {
            return "A vase must have a blue or cream glaze.";
        }

        if (rose.Flower != FlowerKind.Rose)
        {
            return "Only a rose is supported by this arrangement.";
        }

        if (!Enum.IsDefined(rose.Color))
        {
            return "A rose must be red or pink.";
        }

        if (objects.Length != 1 || !ReferenceEquals(objects[0].Ceramic, rose.Vase))
        {
            return "A rose scene must contain exactly its shared vase and no other object.";
        }

        var plant = rose.Plant;
        if (plant is null || plant.Stem.IsDefault || plant.Leaves.IsDefault ||
            plant.Bloom is null || plant.Bloom.Petals.IsDefault)
        {
            return "A vase rose must describe one stem, two leaves and one layered bloom.";
        }

        if (plant.Stem.Length != 1 || plant.Leaves.Length != 2 || plant.Bloom.Petals.Length != RoseGeometry.PetalCount)
        {
            return "A vase rose must have one continuous stem, exactly two leaves and thirteen petals.";
        }

        if (!IsFinite(plant.StemBase) || !IsFinite(plant.StemTop) ||
            !float.IsFinite(plant.StemThickness) || plant.StemThickness <= 0 ||
            !IsFinite(plant.Bloom.Center) || !float.IsFinite(plant.Bloom.RadiusX) || plant.Bloom.RadiusX <= 0 ||
            !float.IsFinite(plant.Bloom.RadiusY) || plant.Bloom.RadiusY <= 0 ||
            !AreFinite(plant.Stem))
        {
            return "Rose geometry must be finite and positive.";
        }

        var stem = plant.Stem[0];
        if (stem.Start != plant.StemBase || stem.End != plant.StemTop)
        {
            return "The rose stem must connect its typed base and bloom attachment.";
        }

        for (var index = 0; index < plant.Bloom.Petals.Length; index++)
        {
            var petal = plant.Bloom.Petals[index];
            var expectedLayer = index < RoseGeometry.OuterPetalCount
                ? RosePetalLayer.Outer
                : index < RoseGeometry.OuterPetalCount + RoseGeometry.MiddlePetalCount
                    ? RosePetalLayer.Middle
                    : RosePetalLayer.Inner;
            if (petal is null || petal.Layer != expectedLayer || !Enum.IsDefined(petal.Layer) ||
                !IsFinite(petal.Root) || !IsFinite(petal.Tip) || !float.IsFinite(petal.Angle) ||
                !float.IsFinite(petal.Tone) || petal.Tone is < 0 or > 1 ||
                !AreFinite(petal.Outline) || petal.Outline.Length < 3 || !HasNonDegenerateExtent(petal.Outline) ||
                !AreFinite(petal.Fold) || petal.Fold.Length != 1)
            {
                return "Rose petals must keep their finite outer, middle and inner layer structure.";
            }
        }

        foreach (var leaf in plant.Leaves)
        {
            if (leaf is null || !IsFinite(leaf.Attachment) || !IsFinite(leaf.Tip) ||
                !float.IsFinite(leaf.Width) || leaf.Width <= 0 ||
                !AreFinite(leaf.Outline) || leaf.Outline.Length < 3 || !HasNonDegenerateExtent(leaf.Outline) ||
                !AreFinite(leaf.Vein) || leaf.Vein.IsDefaultOrEmpty)
            {
                return "Every rose leaf must describe a finite blade and vein.";
            }

            if (leaf.Tip.Y >= leaf.Attachment.Y || leaf.Attachment.Y >= rose.MouthTop - 0.004f ||
                leaf.Tip.Y >= rose.MouthTop - 0.004f || RoseGeometry.DistanceToStem(plant, leaf.Attachment) > 0.002f)
            {
                return "Both rose leaves must point upward and remain attached above the vase opening.";
            }
        }

        if (plant.Leaves[0].Tip.X >= plant.Leaves[0].Attachment.X ||
            plant.Leaves[1].Tip.X <= plant.Leaves[1].Attachment.X ||
            plant.Leaves[0].Tip.X >= plant.Leaves[1].Tip.X ||
            Vector2.Distance(plant.Leaves[0].Attachment, plant.Leaves[1].Attachment) <= 0.01f)
        {
            return "The two rose leaves must remain separate on opposite sides of the stem.";
        }

        var opening = rose.OpeningCenter;
        var radius = rose.OpeningRadius;
        var offsetX = (plant.StemBase.X - opening.X) / radius.X;
        var offsetY = (plant.StemBase.Y - opening.Y) / radius.Y;
        var reach = (offsetX * offsetX) + (offsetY * offsetY);
        if (reach >= 0.95f || plant.StemBase.Y <= rose.MouthTop)
        {
            return "The rose stem must enter through the vase opening rather than the exterior wall.";
        }

        if (plant.StemTop.Y >= rose.MouthTop - 0.004f)
        {
            return "The rose stem must rise clear of the vase rim.";
        }

        if (plant.StemTop.X != plant.Bloom.Center.X ||
            plant.StemTop.Y <= plant.Bloom.Top || plant.StemTop.Y >= plant.Bloom.Bottom ||
            plant.Bloom.Top >= rose.MouthTop - 0.01f || plant.Bloom.Bottom >= rose.MouthTop - 0.004f)
        {
            return "The rose bloom must attach to the stem and sit above the opening, clear of the rim.";
        }

        var bounds = plant.Bounds;
        if (!float.IsFinite(bounds.Left) || !float.IsFinite(bounds.Top) ||
            !float.IsFinite(bounds.Right) || !float.IsFinite(bounds.Bottom))
        {
            return "Rose geometry must be finite and positive.";
        }

        if (bounds.Top < RoseGeometry.TopMargin - 0.012f || bounds.Left < 0.02f || bounds.Right > 0.98f ||
            bounds.Bottom > 0.94f || bounds.Bottom > plant.StemBase.Y + (plant.StemThickness / 2) + 0.001f)
        {
            return "Rose bounds must remain inside the safe canvas margins.";
        }

        if (plant.Bloom.Width > radius.X * 2.6f || plant.Bloom.Height > 0.16f ||
            plant.StemThickness > radius.X * 0.3f)
        {
            return "The rose bloom and stem must stay proportionate to the vase opening.";
        }

        return null;
    }

    private static bool IsFinite(Vector2 point) => float.IsFinite(point.X) && float.IsFinite(point.Y);

    private static bool AreFinite(ImmutableArray<CubicSegment> segments) =>
        !segments.IsDefaultOrEmpty && segments.All(segment =>
            segment is not null && IsFinite(segment.Start) && IsFinite(segment.Control1) &&
            IsFinite(segment.Control2) && IsFinite(segment.End));

    private static bool HasNonDegenerateExtent(ImmutableArray<CubicSegment> segments)
    {
        var extent = CurveBuilder.Extent(segments);
        return extent.Right - extent.Left > 1e-5f && extent.Bottom - extent.Top > 1e-5f;
    }

    // The mixed contents are one fixed tulip and one fixed daisy sharing the exact vase entry.
    // Validate both species through their standalone rules first, then validate the pair-only
    // opening fit and left/right separation constraints.
    private static string? ValidateVaseTulipDaisy(ImmutableArray<SceneEntry> objects, VaseTulipDaisy pair)
    {
        if (pair.Vase is null || pair.Vase.Kind != CeramicKind.Vase)
        {
            return "A mixed flower arrangement must reference one vase.";
        }

        if (!Enum.IsDefined(pair.Vase.Glaze))
        {
            return "A vase must have a blue or cream glaze.";
        }

        if (!Enum.IsDefined(pair.TulipColor))
        {
            return "A mixed arrangement's tulip must be red or yellow.";
        }

        if (pair.DaisyAppearance != DaisyAppearance.WhiteWithGoldenDisk)
        {
            return "A mixed arrangement's daisy must have white petals and a golden centre.";
        }

        if (objects.Length != 1 || !ReferenceEquals(objects[0].Ceramic, pair.Vase))
        {
            return "A mixed flower arrangement must contain exactly its shared vase and no other object.";
        }

        var tulipError = ValidateVaseFlower(
            objects,
            new VaseFlower(pair.Vase, FlowerKind.Tulip, pair.TulipColor, pair.Tulip));
        if (tulipError is not null)
        {
            return "The tulip in the mixed arrangement is invalid: " + tulipError;
        }

        var daisyError = ValidateVaseDaisy(
            objects,
            new VaseDaisy(pair.Vase, pair.DaisyAppearance, pair.Daisy));
        if (daisyError is not null)
        {
            return "The daisy in the mixed arrangement is invalid: " + daisyError;
        }

        var opening = VaseMouth.Center(pair.Vase, VaseMouth.Profile(pair.Vase));
        var radius = VaseMouth.Radius(pair.Vase, VaseMouth.Profile(pair.Vase));
        var minimumStemGap = ((pair.Tulip.StemThickness + pair.Daisy.StemThickness) / 2) + 0.001f;
        if (pair.Tulip.StemBase.X >= opening.X || pair.Daisy.StemBase.X <= opening.X ||
            pair.Daisy.StemBase.X - pair.Tulip.StemBase.X <= minimumStemGap)
        {
            return "The mixed arrangement's stem bases must stay distinct and left-to-right inside the vase opening.";
        }

        var tulipStem = CurveBuilder.Extent(pair.Tulip.Stem);
        var daisyStem = CurveBuilder.Extent(pair.Daisy.Stem);
        if (daisyStem.Left - tulipStem.Right <= minimumStemGap)
        {
            return "The mixed arrangement's tulip and daisy stems must stay distinct and left-to-right.";
        }

        var tulipBloom = MeasureTulipBloom(pair.Tulip);
        var daisyBloom = MeasureDaisyBloom(pair.Daisy);
        if (daisyBloom.Left - tulipBloom.Right < 0.003f)
        {
            return "The mixed arrangement's tulip and daisy blooms must stay distinct and separated.";
        }

        foreach (var stemBase in new[] { pair.Tulip.StemBase, pair.Daisy.StemBase })
        {
            var x = MathF.Abs(stemBase.X - opening.X) + (minimumStemGap * 0.5f);
            var y = stemBase.Y - opening.Y;
            var reach = ((x / radius.X) * (x / radius.X)) + ((y / radius.Y) * (y / radius.Y));
            if (reach >= 0.95f)
            {
                return "Both mixed-arrangement stems must fit inside the vase's rim-inset opening.";
            }
        }

        return null;
    }

    private static ProfileExtent MeasureTulipBloom(TulipPlant plant)
    {
        var extent = new ProfileExtent(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
        foreach (var petal in plant.Bloom.Petals)
        {
            extent = Union(extent, CurveBuilder.Extent(petal.Outline));
        }

        return extent;
    }

    private static ProfileExtent MeasureDaisyBloom(DaisyPlant plant)
    {
        var extent = new ProfileExtent(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
        foreach (var petal in plant.Bloom.Petals)
        {
            extent = Union(extent, CurveBuilder.Extent(petal.Outline));
        }

        var disk = plant.Bloom.Disk;
        return Union(extent, new ProfileExtent(
            disk.Center.X - disk.RadiusX,
            disk.Center.Y - disk.RadiusY,
            disk.Center.X + disk.RadiusX,
            disk.Center.Y + disk.RadiusY));
    }

    private static ProfileExtent Union(ProfileExtent first, ProfileExtent second) => new(
        MathF.Min(first.Left, second.Left),
        MathF.Min(first.Top, second.Top),
        MathF.Max(first.Right, second.Right),
        MathF.Max(first.Bottom, second.Bottom));

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
