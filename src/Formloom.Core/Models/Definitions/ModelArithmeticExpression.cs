using System.Collections.Immutable;

namespace Formloom.Core.Models.Definitions;

public sealed record ModelArithmeticExpression(
    ModelArithmeticOperator Operator,
    ImmutableArray<ModelNumberExpression> Operands) : ModelNumberExpression;