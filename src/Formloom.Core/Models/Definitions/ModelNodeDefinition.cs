using System.Text.Json.Serialization;

namespace Formloom.Core.Models.Definitions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "op")]
[JsonDerivedType(typeof(ModelCubicPathNodeDefinition), "path.cubic")]
[JsonDerivedType(typeof(ModelCurveProjectNodeDefinition), "curve.project")]
[JsonDerivedType(typeof(ModelCurveSampleNodeDefinition), "curve.sample")]
[JsonDerivedType(typeof(ModelProfileMapNodeDefinition), "profile.map")]
[JsonDerivedType(typeof(ModelRadialArrayNodeDefinition), "array.radial")]
[JsonDerivedType(typeof(ModelGeometryGroupNodeDefinition), "geometry.group")]
public abstract record ModelNodeDefinition(string Id);