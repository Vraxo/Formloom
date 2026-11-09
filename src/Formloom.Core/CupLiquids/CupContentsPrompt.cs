using Formloom.Core.Ceramics;

namespace Formloom.Core.CupLiquids;

// A filled-cup prompt. The cup is the container and the beverage is its contents, so the
// roles are part of the type and cannot be mistaken for two neighbouring objects.
public sealed record CupContentsPrompt(CeramicPrompt Cup, BeverageKind Beverage);
