using GameCore.HotUpdate;
using UnityEngine;

namespace BetterCrafting
{
    // Skips the game's "not enough materials" text only for a click that the mod fills, and only in the
    // frame of that click: when the game's RA_RecipeClick throws, the Postfix does not clear the flag.
    internal static class AnyStorageOnPopText
    {
        public static bool Prefix(string Text)
        {
            if (!AnyStorageFill.ModFills || Time.frameCount != AnyStorageFill.ModFillsFrame) return true;
            return Text != ConstantTextTools.ToConstantTextOrEmpty(AnyStorageFill.ShortageTextKey);
        }
    }
}
