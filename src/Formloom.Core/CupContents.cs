namespace Formloom.Core;

// The one cup-content relationship the grammar supports: a single existing cup holding a
// single still beverage. It refers to the cup instance that already exists in the scene
// rather than duplicating it, so the cup keeps its kind, glaze, body, depth and handle,
// and the drink never becomes a second independently positioned scene object.
public sealed record CupContents(CeramicInstance Cup, BeverageKind Beverage)
{
    public CeramicProfile Profile => CeramicForms.For(Cup.Kind);

    public CupLiquidSurface Surface => CupLiquidSurface.For(Profile);

    public string SubjectToken =>
        Beverage.ToString().ToLowerInvariant() + "-in-" + Cup.Glaze.ToString().ToLowerInvariant() + "-cup";
}
