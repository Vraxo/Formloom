using Formloom.Core.Ceramics;

namespace Formloom.Core.Roses;

/// <summary>A strict rose-in-vase prompt, with the rose and vessel roles kept explicit.</summary>
public sealed record VaseRosePrompt(CeramicPrompt Vase, RoseColor Color);
