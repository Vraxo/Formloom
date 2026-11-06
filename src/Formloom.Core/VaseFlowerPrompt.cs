namespace Formloom.Core;

// A tulip-in-vase prompt. The vase is the container and the flower is its content, so the
// roles are part of the type and cannot be mistaken for two neighbouring objects.
public sealed record VaseFlowerPrompt(CeramicPrompt Vase, FlowerKind Flower, TulipColor Color);
