namespace Formloom.Core;

public readonly record struct CeramicBounds(float Left, float Top, float Right, float Bottom)
{
    public static CeramicBounds For(CeramicInstance ceramic)
    {
        ArgumentNullException.ThrowIfNull(ceramic);
        var profile = CeramicForms.For(ceramic.Kind);
        var extent = CurveBuilder.Extent(profile.Outline);
        var left = Math.Min(extent.Left, profile.MouthCenter.X - profile.MouthRadius.X);
        var top = Math.Min(extent.Top, profile.MouthCenter.Y - profile.MouthRadius.Y);
        var right = Math.Max(extent.Right, profile.MouthCenter.X + profile.MouthRadius.X);
        var bottom = Math.Max(extent.Bottom, profile.MouthCenter.Y + profile.MouthRadius.Y);
        if (profile.Handle is { } handle)
        {
            left = Math.Min(left, handle.Center.X - handle.OuterRadius.X);
            top = Math.Min(top, handle.Center.Y - handle.OuterRadius.Y);
            right = Math.Max(right, handle.Center.X + handle.OuterRadius.X);
            bottom = Math.Max(bottom, handle.Center.Y + handle.OuterRadius.Y);
        }

        var body = ceramic.Body;
        var originX = body.Center.X - (body.Size.X / 2);
        var originY = body.Center.Y - (body.Size.Y / 2);
        return new(
            originX + (left * body.Size.X),
            originY + (top * body.Size.Y),
            originX + (right * body.Size.X),
            originY + (bottom * body.Size.Y));
    }
}
