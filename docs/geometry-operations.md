# Reusable procedural geometry operations

> **Status:** Initial C# capability inventory for Phase 11. These algorithms are ordinary Core code, not a second model-definition language. Their boundary is governed by [ADR-0001: Keep Formloom a hybrid procedural engine](adr/0001-hybrid-procedural-engine.md).

## Design rule

A reusable algorithm belongs in C#. Model files may choose its inputs and compose its results, but must not reimplement its mathematics as a long graph of low-level JSON expressions. The methods in this document are model-independent: none accepts a model ID, species-specific record, renderer type, or JSON DTO.

The first operations were selected by inspecting `RoseGeometry`: its broad/curling petals and serrated leaves share an oriented profile-mapping formula; its layered blooms use radial repetition and per-element variation; and its leaves attach to a continuous cubic stem. The legacy rose implementation is unchanged at this stage. These utilities have not yet been wired into an interpreter or used to regenerate the rose.

## Profile mapping

**Implementation:** `Formloom.Core.Geometry.ProfileMapper`

`Map(profile, origin, direction, options)` maps normalized 2D profile points into an oriented local basis.

- The direction is normalized by the mapper; its perpendicular normal defines the transverse axis.
- `Length` and `RootOffset` control longitudinal placement.
- `Width` and `TransverseNormalization` control transverse scale.
- `Bend` adds a sinusoidal transverse offset along the normalized longitudinal coordinate.
- `ReverseLongitudinal` flips the profile's Y coordinate before mapping.
- `ScaleX` and `ScaleY` apply the final component-wise scale, useful for elliptical scene-space mapping.
- Inputs must be finite; direction must be non-zero; width and length must be non-negative; transverse normalization must be positive.

The formula is:

```text
t = reverse ? (1 - y) : y
along = rootOffset + t * length
across = ((x - 0.5) / transverseNormalization) * (width / 2)
       + bend * sin(pi * t)
mapped = origin + (direction * along + normal * across) * (scaleX, scaleY)
```

This formula covers the shared mathematics in the current rose petal and leaf profile transforms without naming either part type. Callers choose the options appropriate to their profile.

## Radial array generation

**Implementation:** `Formloom.Core.Geometry.RadialArrayGenerator`

`Generate(count, startAngle, phase, variation, amount, seed, stream)` returns elements in index order. Base angles are evenly spaced by `Tau / count`, with phase applied before per-element angle variation.

Each element gets an independent PCG stream derived from the seed and `stream/index`. It returns the final angle, length and width multipliers, bend offset, and tone offset. Adding another element does not consume random values from existing elements; changing the count does change the base angular spacing, as expected for a radial arrangement.

Variation ranges are non-negative maximum absolute offsets. `amount` is limited to 0..1. Length and width variation ranges cannot exceed 1, preventing their multipliers from becoming negative. The caller supplies a stable operation/layer stream name. Runtime definition validation remains responsible for applying the catalog's configured repetition limits before calling the operation.

This is a generic arrangement primitive. It does not know what an element represents or how its geometry is constructed.

## Cubic curve projection

**Implementation:** `Formloom.Core.Geometry.CubicCurveProjector`

`ProjectNearest(segments, target, samplesPerSegment)` finds an approximate nearest point across an ordered set of cubic Bézier segments and returns the winning segment index, local parameter, point, normalized tangent, and squared distance.

The algorithm samples each segment to find a candidate interval, then performs a fixed number of golden-section refinement iterations inside that interval. Squared-distance comparisons use double precision so a large orthogonal offset does not erase small coordinate differences through float rounding. It is deterministic and bounded by the supplied sample count. Ties preserve the earlier segment. The tangent is zero only when the curve derivative is degenerate at the projected point.

This is appropriate for deterministic geometric attachments such as the current leaf-to-stem projection. It is a numeric projection, not an analytic solution to every possible cubic distance polynomial.

## Existing primitives reused

- `CubicSegment` is the existing cubic Bézier representation and point evaluator.
- `CurveBuilder.ClosedCubic` converts a mapped closed contour into smooth cubic segments.
- `Pcg32.ForStream` provides stable named random streams.

These are sufficient primitives for now. A separate abstraction for each trivial constructor or array/list operation would add indirection without adding a reusable capability.

## Explicitly not implemented here

- Model-definition expression evaluation or node dispatch. A future interpreter resolves typed values and calls these C# operations.
- Rose-specific petal/leaf construction, fixed 5/5/3 counts, profiles, palette, vein/fold paths, or vase-relative proportions.
- Generic fill, stroke, gradient, clipping, or Skia paint creation. These belong to a separately specified material/rendering contract.
- A universal procedural scripting language or arbitrary user-defined operators.
- Changes to `RoseGeometry`, rose validation, prompt handling, scene generation, or rendering.

The next step is to express the remaining rose-specific facts as external data, identify any missing reusable algorithms from concrete rose requirements, and build the interpreter around this small C# capability set. Do not expand the operation set speculatively.
