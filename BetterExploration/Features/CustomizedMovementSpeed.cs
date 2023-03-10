using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterExploration.Features
{
    [UsedImplicitly]
    public class CustomizedMovementSpeed
    {
        [HarmonyPatch(typeof(GlobalSettings), "GetMovementSpeedMod")]
        public class GlobalSettingsGetMovementSpeedModPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref float __result)
            {
                if (!Options.IsCustomizedMovementSpeedEnabled.Value)
                {
                    return;
                }

                __result *= Options.SpeedMultiplier.Value;
            }
        }
    }
}
