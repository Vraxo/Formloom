namespace Formloom.Core;

// A daisy-in-vase prompt. The vase is the container and the daisy is its content, so the roles
// are part of the type; the appearance is the daisy's own closed value and never a TulipColor.
public sealed record VaseDaisyPrompt(CeramicPrompt Vase, DaisyAppearance Appearance);
