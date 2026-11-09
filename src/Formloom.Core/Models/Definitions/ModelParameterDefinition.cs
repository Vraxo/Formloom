using System.Collections.Immutable;

namespace Formloom.Core.Models.Definitions;

public sealed record ModelParameterDefinition(
    ModelParameterType Type,
    ModelNumberExpression? DefaultValue,
    ModelNumberExpression? Minimum,
    ModelNumberExpression? Maximum,
    string? DefaultChoice,
    ImmutableArray<string> Values);