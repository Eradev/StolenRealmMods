using Burst2Flame;
using HarmonyLib;

namespace eradev.stolenrealm.BetterTooltips
{
    public static class ActionStatusInfoExtensions
    {
        public static ActionStatusInfo Clone(this ActionStatusInfo self)
        {
            return (ActionStatusInfo)AccessTools.Method(typeof(ActionStatusInfo), "MemberwiseClone").Invoke(self, null);
        }
    }
}
