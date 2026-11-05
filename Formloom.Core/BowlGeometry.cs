using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core;

// Profile-space geometry the renderer needs to split a bowl into a rear stage
// (body, cavity) and a front stage (near wall and lip) so a contained fruit can be
// drawn between them. Everything stays in the normalized vessel space.
public static class BowlGeometry
{
    private const float Kappa = 0.5522847f;

    public static Vector2 LeftRim(CeramicProfile profile) =>
        new(profile.MouthCenter.X - profile.MouthRadius.X, profile.MouthCenter.Y);

    public static Vector2 RightRim(CeramicProfile profile) =>
        new(profile.MouthCenter.X + profile.MouthRadius.X, profile.MouthCenter.Y);

    // Number of leading outline segments that form the near/front wall: from the left
    // rim, around the outside, up to the right rim. The remaining segments are the
    // far rim, which must stay behind a contained fruit.
    public static int FrontChainLength(CeramicProfile profile)
    {
        var right = RightRim(profile);
        for (var index = 0; index < profile.Outline.Length; index++)
        {
            var end = profile.Outline[index].End;
            if (Math.Abs(end.X - right.X) < 0.02f && Math.Abs(end.Y - right.Y) < 0.02f)
            {
                return index + 1;
            }
        }

        throw new InvalidOperationException("The vessel outline does not reach the right rim.");
    }

    // The near-wall band: the opening's lower arc followed by the body's front chain
    // traversed backwards. Filling this over a contained fruit hides its lower part.
    public static ImmutableArray<CubicSegment> FrontWall(CeramicProfile profile)
    {
        var front = FrontChainLength(profile);
        var segments = ImmutableArray.CreateBuilder<CubicSegment>(front + 2);
        var center = profile.MouthCenter;
        var radius = profile.MouthRadius;
        var left = LeftRim(profile);
        var bottom = new Vector2(center.X, center.Y + radius.Y);
        var right = RightRim(profile);
        segments.Add(new(left, new(center.X - radius.X, center.Y + (Kappa * radius.Y)), new(center.X - (Kappa * radius.X), center.Y + radius.Y), bottom));
        segments.Add(new(bottom, new(center.X + (Kappa * radius.X), center.Y + radius.Y), new(center.X + radius.X, center.Y + (Kappa * radius.Y)), right));
        for (var index = front - 1; index >= 0; index--)
        {
            var segment = profile.Outline[index];
            segments.Add(new(segment.End, segment.Control2, segment.Control1, segment.Start));
        }

        return segments.MoveToImmutable();
    }

    // Closed four-quadrant approximation of the opening, for clipping interior work.
    public static ImmutableArray<CubicSegment> Opening(CeramicProfile profile)
    {
        var center = profile.MouthCenter;
        var radius = profile.MouthRadius;
        var top = new Vector2(center.X, center.Y - radius.Y);
        var right = RightRim(profile);
        var bottom = new Vector2(center.X, center.Y + radius.Y);
        var left = LeftRim(profile);
        return
        [
            new(left, new(center.X - radius.X, center.Y - (Kappa * radius.Y)), new(center.X - (Kappa * radius.X), center.Y - radius.Y), top),
            new(top, new(center.X + (Kappa * radius.X), center.Y - radius.Y), new(center.X + radius.X, center.Y - (Kappa * radius.Y)), right),
            new(right, new(center.X + radius.X, center.Y + (Kappa * radius.Y)), new(center.X + (Kappa * radius.X), center.Y + radius.Y), bottom),
            new(bottom, new(center.X - (Kappa * radius.X), center.Y + radius.Y), new(center.X - radius.X, center.Y + (Kappa * radius.Y)), left),
        ];
    }
}
