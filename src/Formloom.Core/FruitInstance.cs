namespace Formloom.Core;

public sealed record FruitInstance(FruitKind Subject, AppleDescription Body, ulong Seed, int Depth)
{
    public FruitColor Color => Subject == FruitKind.Orange ? FruitColor.Orange : (FruitColor)Body.Color;
}
