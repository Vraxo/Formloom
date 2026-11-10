using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Models.Catalog;
using Formloom.Core.Models.Definitions;

namespace Formloom.Core.Models.Evaluation;

internal static class ModelNumberExpressionEvaluator
{
    public static bool TryEvaluate(
        ModelNumberExpression? expression,
        Func<string, double?> resolveParameter,
        ImmutableDictionary<string, ModelMountFrameValues> frames,
        IReadOnlyDictionary<string, ModelNodeEvaluationOutput> nodeOutputs,
        ModelDefinitionCatalogOptions options,
        out double value,
        out string error)
    {
        var remaining = options.MaximumExpressionNodes;
        return TryEvaluateNode(
            expression,
            resolveParameter,
            frames,
            nodeOutputs,
            options.MaximumExpressionDepth,
            1,
            ref remaining,
            out value,
            out error);
    }

    private static bool TryEvaluateNode(
        ModelNumberExpression? expression,
        Func<string, double?> resolveParameter,
        ImmutableDictionary<string, ModelMountFrameValues> frames,
        IReadOnlyDictionary<string, ModelNodeEvaluationOutput> nodeOutputs,
        int maximumDepth,
        int depth,
        ref int remaining,
        out double value,
        out string error)
    {
        value = 0;
        error = string.Empty;
        if (expression is null)
        {
            error = "A numeric expression is required.";
            return false;
        }

        remaining--;
        if (remaining < 0 || depth > maximumDepth)
        {
            error = "The numeric expression exceeds its configured resource limits.";
            return false;
        }

        switch (expression)
        {
            case ModelLiteralExpression literal:
                value = literal.Value;
                break;
            case ModelReferenceExpression reference:
                return TryResolveReference(reference.Path, resolveParameter, frames, nodeOutputs, out value, out error);
            case ModelArithmeticExpression arithmetic:
                return TryEvaluateArithmetic(
                    arithmetic,
                    resolveParameter,
                    frames,
                    nodeOutputs,
                    maximumDepth,
                    depth,
                    ref remaining,
                    out value,
                    out error);
            case ModelClampExpression clamp:
                if (!TryEvaluateNode(clamp.Value, resolveParameter, frames, nodeOutputs, maximumDepth, depth + 1, ref remaining, out var current, out error) ||
                    !TryEvaluateNode(clamp.Minimum, resolveParameter, frames, nodeOutputs, maximumDepth, depth + 1, ref remaining, out var minimum, out error) ||
                    !TryEvaluateNode(clamp.Maximum, resolveParameter, frames, nodeOutputs, maximumDepth, depth + 1, ref remaining, out var maximum, out error))
                {
                    return false;
                }

                if (minimum > maximum)
                {
                    error = "Clamp minimum cannot exceed its maximum.";
                    return false;
                }

                value = Math.Clamp(current, minimum, maximum);
                break;
            default:
                error = $"Expression type '{expression.GetType().Name}' is not supported.";
                return false;
        }

        if (!double.IsFinite(value))
        {
            error = "The numeric expression produced a non-finite value.";
            return false;
        }

        return true;
    }

    private static bool TryEvaluateArithmetic(
        ModelArithmeticExpression arithmetic,
        Func<string, double?> resolveParameter,
        ImmutableDictionary<string, ModelMountFrameValues> frames,
        IReadOnlyDictionary<string, ModelNodeEvaluationOutput> nodeOutputs,
        int maximumDepth,
        int depth,
        ref int remaining,
        out double value,
        out string error)
    {
        value = 0;
        error = string.Empty;
        if (arithmetic.Operands.IsDefaultOrEmpty || arithmetic.Operands.Length < 2 ||
            ((arithmetic.Operator is ModelArithmeticOperator.Subtract or ModelArithmeticOperator.Divide) &&
             arithmetic.Operands.Length != 2))
        {
            error = "The arithmetic expression has an invalid operand count.";
            return false;
        }

        var operands = new double[arithmetic.Operands.Length];
        for (var index = 0; index < arithmetic.Operands.Length; index++)
        {
            if (!TryEvaluateNode(
                arithmetic.Operands[index],
                resolveParameter,
                frames,
                nodeOutputs,
                maximumDepth,
                depth + 1,
                ref remaining,
                out operands[index],
                out error))
            {
                return false;
            }
        }

        switch (arithmetic.Operator)
        {
            case ModelArithmeticOperator.Add:
                value = operands.Sum();
                break;
            case ModelArithmeticOperator.Subtract:
                value = operands[0] - operands[1];
                break;
            case ModelArithmeticOperator.Multiply:
                value = operands.Aggregate(1d, (current, operand) => current * operand);
                break;
            case ModelArithmeticOperator.Divide:
                if (operands[1] == 0)
                {
                    error = "Division by zero is not permitted.";
                    return false;
                }

                value = operands[0] / operands[1];
                break;
            case ModelArithmeticOperator.Minimum:
                value = operands.Min();
                break;
            case ModelArithmeticOperator.Maximum:
                value = operands.Max();
                break;
            default:
                error = $"Arithmetic operator '{arithmetic.Operator}' is not supported.";
                return false;
        }

        if (!double.IsFinite(value))
        {
            error = "The arithmetic expression produced a non-finite value.";
            return false;
        }

        return true;
    }

    private static bool TryResolveReference(
        string reference,
        Func<string, double?> resolveParameter,
        ImmutableDictionary<string, ModelMountFrameValues> frames,
        IReadOnlyDictionary<string, ModelNodeEvaluationOutput> nodeOutputs,
        out double value,
        out string error)
    {
        value = 0;
        error = string.Empty;
        var parts = reference.Split('.', StringSplitOptions.None);
        if (parts.Length == 2 && parts[0] == "parameters")
        {
            var parameter = resolveParameter(parts[1]);
            if (parameter is null)
            {
                error = $"Numeric parameter '{parts[1]}' could not be resolved.";
                return false;
            }

            value = parameter.Value;
            return true;
        }

        if (parts.Length == 3 && parts[0] == "frame" &&
            frames.TryGetValue(parts[1], out var frame))
        {
            value = parts[2] switch
            {
                "radiusX" => frame.RadiusX,
                "radiusY" => frame.RadiusY,
                "mouthTop" => frame.MouthTop,
                "rimThickness" => frame.RimThickness,
                "headroom" => frame.Headroom,
                _ => double.NaN,
            };
            if (double.IsFinite(value))
            {
                return true;
            }
        }
        else if (parts.Length == 4 && parts[0] == "frame" && parts[2] == "openingCenter" &&
            frames.TryGetValue(parts[1], out frame))
        {
            value = parts[3] switch
            {
                "x" => frame.OpeningCenter.X,
                "y" => frame.OpeningCenter.Y,
                _ => double.NaN,
            };
            if (double.IsFinite(value))
            {
                return true;
            }
        }

        var nodeParts = reference.Split(':', StringSplitOptions.None);
        if (nodeParts.Length == 4 && nodeParts[0] == "node" &&
            nodeOutputs.TryGetValue(nodeParts[1], out var node) &&
            node.Landmarks.TryGetValue(nodeParts[2], out var landmark))
        {
            value = nodeParts[3] switch
            {
                "x" => landmark.X,
                "y" => landmark.Y,
                _ => double.NaN,
            };
            if (double.IsFinite(value))
            {
                return true;
            }
        }

        error = $"Reference '{reference}' could not be resolved in the current evaluation context.";
        return false;
    }
}
