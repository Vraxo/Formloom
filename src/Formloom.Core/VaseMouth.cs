using System.Numerics;

namespace Formloom.Core;

// The opening of the vase instance a flower stands in, derived from that vase's own body size
// and profile. Both flower contents read the vessel through these helpers, so the opening is
// computed in exactly one place and no canvas pixel offset is hard-coded anywhere.
internal static class VaseMouth
{
    public static CeramicProfile Profile(CeramicInstance vase) => CeramicForms.For(vase.Kind);

    public static Vector2 Center(CeramicInstance vase, CeramicProfile profile) => new(
        vase.Body.Center.X - (vase.Body.Size.X / 2) + (profile.MouthCenter.X * vase.Body.Size.X),
        vase.Body.Center.Y - (vase.Body.Size.Y / 2) + (profile.MouthCenter.Y * vase.Body.Size.Y));

    public static Vector2 Radius(CeramicInstance vase, CeramicProfile profile) => new(
        profile.MouthRadius.X * vase.Body.Size.X,
        profile.MouthRadius.Y * vase.Body.Size.Y);
}
