using System.Collections.Immutable;
using System.Numerics;

namespace Formloom.Core.Models.Geometry;

public static class ModelGeometryValidator
{
    private const float ContinuityTolerance = 0.00001f;

    public static ImmutableArray<ModelGeometryDiagnostic> Validate(ModelGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var diagnostics = ImmutableArray.CreateBuilder<ModelGeometryDiagnostic>();
        if (string.IsNullOrWhiteSpace(geometry.ModelId))
        {
            diagnostics.Add(new("geometry.model-id-required", null, "A model ID is required."));
        }

        if (!Enum.IsDefined(geometry.CoordinateSpace))
        {
            diagnostics.Add(new("geometry.coordinate-space-unsupported", null, "The coordinate space is unsupported."));
        }

        ValidateMaterialGradients(geometry, diagnostics);

        if (geometry.Paths.IsDefaultOrEmpty)
        {
            diagnostics.Add(new("geometry.paths-required", null, "At least one geometry path is required."));
            return diagnostics.ToImmutable();
        }

        var pathIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in geometry.Paths)
        {
            if (path is null)
            {
                diagnostics.Add(new("geometry.path-null", null, "A geometry path cannot be null."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(path.Id))
            {
                diagnostics.Add(new("geometry.path-id-required", null, "A path ID is required."));
            }
            else if (!pathIds.Add(path.Id))
            {
                diagnostics.Add(new("geometry.path-id-duplicate", path.Id, "Path IDs must be unique."));
            }

            if (string.IsNullOrWhiteSpace(path.SourceNodeId))
            {
                diagnostics.Add(new("geometry.source-node-required", path.Id, "A source node ID is required."));
            }

            if (path.MaterialSlot is not null && string.IsNullOrWhiteSpace(path.MaterialSlot))
            {
                diagnostics.Add(new("geometry.material-slot-invalid", path.Id, "A material slot must be null or non-empty."));
            }

            if (path.MaterialTone is { } tone && (!float.IsFinite(tone) || tone is < 0 or > 1))
            {
                diagnostics.Add(new("geometry.material-tone-invalid", path.Id, "Material tone must be finite and in the range 0..1."));
            }

            if (path.StrokeWidth is { } strokeWidth)
            {
                if (!float.IsFinite(strokeWidth) || strokeWidth <= 0)
                {
                    diagnostics.Add(new("geometry.stroke-width-invalid", path.Id, "Stroke width must be finite and greater than zero."));
                }

                if (string.IsNullOrWhiteSpace(path.MaterialSlot))
                {
                    diagnostics.Add(new("geometry.stroke-material-slot-required", path.Id, "A stroked path requires a material slot."));
                }
            }

            if (path.Landmarks.IsDefault)
            {
                diagnostics.Add(new("geometry.landmarks-uninitialized", path.Id, "Path landmarks must be initialized."));
            }
            else
            {
                var landmarkNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var landmark in path.Landmarks)
                {
                    if (landmark is null || string.IsNullOrWhiteSpace(landmark.Name) ||
                        !landmarkNames.Add(landmark.Name) || !IsFinite(landmark.Position))
                    {
                        diagnostics.Add(new("geometry.landmark-invalid", path.Id, "Landmarks must have unique non-empty names and finite coordinates."));
                    }
                }
            }

            if (path.Segments.IsDefaultOrEmpty)
            {
                diagnostics.Add(new("geometry.segments-required", path.Id, "A path must contain at least one cubic segment."));
                continue;
            }

            ValidateSegments(path, diagnostics);
        }

        return diagnostics.ToImmutable();
    }

    private static void ValidateSegments(
        ModelGeometryPath path,
        ImmutableArray<ModelGeometryDiagnostic>.Builder diagnostics)
    {
        for (var index = 0; index < path.Segments.Length; index++)
        {
            var segment = path.Segments[index];
            if (segment is null)
            {
                diagnostics.Add(new("geometry.segment-null", path.Id, $"Segment {index} is null."));
                continue;
            }

            if (!IsFinite(segment.Start) || !IsFinite(segment.Control1) ||
                !IsFinite(segment.Control2) || !IsFinite(segment.End))
            {
                diagnostics.Add(new("geometry.segment-non-finite", path.Id, $"Segment {index} contains a non-finite coordinate."));
            }

            if (index > 0 && path.Segments[index - 1] is { } previous &&
                Vector2.Distance(previous.End, segment.Start) > ContinuityTolerance)
            {
                diagnostics.Add(new("geometry.segment-disconnected", path.Id, $"Segment {index} does not meet the previous segment."));
            }
        }

        if (path.Closed && path.Segments[^1] is { } last && path.Segments[0] is { } first &&
            Vector2.Distance(last.End, first.Start) > ContinuityTolerance)
        {
            diagnostics.Add(new("geometry.closed-path-open", path.Id, "A closed path's final endpoint must meet its first start point."));
        }
    }

    private static void ValidateMaterialGradients(
        ModelGeometry geometry,
        ImmutableArray<ModelGeometryDiagnostic>.Builder diagnostics)
    {
        if (geometry.MaterialGradients is null)
        {
            diagnostics.Add(new("geometry.material-gradients-uninitialized", null, "Material gradients must be initialized."));
            return;
        }

        foreach (var (slot, gradient) in geometry.MaterialGradients)
        {
            if (string.IsNullOrWhiteSpace(slot) || gradient is null)
            {
                diagnostics.Add(new("geometry.material-gradient-invalid", null, "A material gradient requires a material slot and gradient definition."));
                continue;
            }

            if (!IsFinite(gradient.Start) || !IsFinite(gradient.End) ||
                gradient.Start.X is < 0 or > 1 || gradient.Start.Y is < 0 or > 1 ||
                gradient.End.X is < 0 or > 1 || gradient.End.Y is < 0 or > 1 ||
                gradient.Start == gradient.End)
            {
                diagnostics.Add(new("geometry.material-gradient-endpoints-invalid", slot, "Gradient endpoints must be distinct finite coordinates in the normalized 0..1 range."));
            }

            if (gradient.Stops.IsDefaultOrEmpty || gradient.Stops.Length < 2)
            {
                diagnostics.Add(new("geometry.material-gradient-stops-invalid", slot, "A linear gradient requires at least two color stops."));
                continue;
            }

            var previousOffset = -1f;
            foreach (var stop in gradient.Stops)
            {
                if (stop is null || !float.IsFinite(stop.Offset) ||
                    stop.Offset is < 0 or > 1 || stop.Offset <= previousOffset ||
                    string.IsNullOrWhiteSpace(stop.Token) ||
                    geometry.MaterialTokens is null ||
                    !geometry.MaterialTokens.ContainsKey(stop.Token))
                {
                    diagnostics.Add(new("geometry.material-gradient-stop-invalid", slot, "Gradient stops require strictly increasing offsets in the normalized 0..1 range and existing material color tokens."));
                    break;
                }

                previousOffset = stop.Offset;
            }
        }
    }

    private static bool IsFinite(Vector2 point) => float.IsFinite(point.X) && float.IsFinite(point.Y);
}
