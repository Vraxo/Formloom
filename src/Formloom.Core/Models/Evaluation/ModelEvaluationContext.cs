using System.Collections.Immutable;

namespace Formloom.Core.Models.Evaluation;

public sealed record ModelEvaluationContext(
    ulong Seed,
    float VariationAmount,
    ImmutableDictionary<string, double> NumberParameters,
    ImmutableDictionary<string, string> ChoiceParameters,
    ImmutableDictionary<string, ModelMountFrameValues> Frames)
{
    public static ModelEvaluationContext Empty(ulong seed = 0, float variationAmount = 0) =>
        new(
            seed,
            variationAmount,
            ImmutableDictionary<string, double>.Empty,
            ImmutableDictionary<string, string>.Empty,
            ImmutableDictionary<string, ModelMountFrameValues>.Empty);
}
