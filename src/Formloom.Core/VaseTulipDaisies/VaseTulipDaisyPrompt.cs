using Formloom.Core.Ceramics;
using Formloom.Core.Daisies;
using Formloom.Core.Tulips;

namespace Formloom.Core.VaseTulipDaisies;

// A fixed red/yellow tulip and white/golden daisy sharing one existing vase.
// The species and container roles are explicit; this is not an arbitrary flower collection.
public sealed record VaseTulipDaisyPrompt(
    CeramicPrompt Vase,
    TulipColor TulipColor,
    DaisyAppearance DaisyAppearance);
