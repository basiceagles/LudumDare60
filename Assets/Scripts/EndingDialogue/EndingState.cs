
using System.Collections.Generic;
using UnityEngine;

// Runtime-only record of how the player finished each story call.
//
// Nothing is written to the ending assets, so the choices live only for the
// current session: stopping play mode, closing the game or reloading the
// domain throws them away and the assets keep their Inspector values.
public static class EndingState
{
    private static readonly Dictionary<DialogueSO, bool> decisions =
        new Dictionary<DialogueSO, bool>();

    // Runs on every play session, even with domain reload turned off.
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void Reset()
    {
        decisions.Clear();
    }

    public static void Set(DialogueSO ending, bool goodEnding)
    {
        if (ending == null)
            return;

        decisions[ending] = goodEnding;
    }

    public static bool TryGet(DialogueSO ending, out bool goodEnding)
    {
        goodEnding = false;

        if (ending == null)
            return false;

        return decisions.TryGetValue(ending, out goodEnding);
    }

    public static void Clear()
    {
        decisions.Clear();
    }
}
