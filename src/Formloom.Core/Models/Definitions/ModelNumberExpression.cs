using System.Text.Json.Serialization;

namespace Formloom.Core.Models.Definitions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ModelLiteralExpression), "literal")]
[JsonDerivedType(typeof(ModelReferenceExpression), "reference")]
[JsonDerivedType(typeof(ModelArithmeticExpression), "arithmetic")]
[JsonDerivedType(typeof(ModelClampExpression), "clamp")]
public abstract record ModelNumberExpression;