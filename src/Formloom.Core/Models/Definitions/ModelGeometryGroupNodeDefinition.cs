using System.Collections.Immutable;

namespace Formloom.Core.Models.Definitions;

public sealed record ModelGeometryGroupNodeDefinition(
    string Id,
    ImmutableArray<string> Parts) : ModelNodeDefinition(Id);