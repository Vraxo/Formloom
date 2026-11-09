namespace Formloom.Core.Models.Definitions;

public sealed record ModelClampExpression(
    ModelNumberExpression Value,
    ModelNumberExpression Minimum,
    ModelNumberExpression Maximum) : ModelNumberExpression;