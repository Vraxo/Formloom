namespace Formloom.Core.Daisies;

// The daisy's fixed appearance. It is a closed value of its own rather than a member of
// TulipColor, so a white flower can never appear by widening the tulip palette and a tulip
// can never be asked for in a colour the grammar does not support.
public enum DaisyAppearance
{
    WhiteWithGoldenDisk,
}
