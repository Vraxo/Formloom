using Formloom.Core.Ceramics;
using Formloom.Core.Fruits;

namespace Formloom.Core.BowlContainments;

// A fruit-in-bowl prompt. The fruit is the content and the bowl is the container;
// the roles are part of the type so they cannot be mistaken for a list of neighbours.
public sealed record ContainmentPrompt(FruitPrompt Content, CeramicPrompt Bowl);
