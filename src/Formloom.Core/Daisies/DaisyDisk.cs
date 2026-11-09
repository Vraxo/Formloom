using System.Numerics;

namespace Formloom.Core.Daisies;

// The golden centre of the bloom. It is drawn over the petal roots, so the petals visibly
// radiate from behind it instead of stopping short of it.
public sealed record DaisyDisk(Vector2 Center, float RadiusX, float RadiusY);
