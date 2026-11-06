using System.Numerics;

namespace Formloom.Core;

// A closed ring behind the cup body. The inner ellipse is a genuine hole, so the
// center stays see-through while the ring joins the body at both attachments.
public sealed record CeramicHandle(
    Vector2 Center,
    Vector2 OuterRadius,
    Vector2 InnerRadius,
    Vector2 TopAttachment,
    Vector2 BottomAttachment)
{
    public Vector2 MidRadius => (OuterRadius + InnerRadius) / 2;

    public float Thickness => OuterRadius.X - InnerRadius.X;
}
