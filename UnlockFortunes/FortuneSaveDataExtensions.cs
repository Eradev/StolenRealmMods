using System.Linq;
using Burst2Flame;

namespace eradev.stolenrealm.UnlockFortunes
{
    public static class FortuneSaveDataExtensions
    {
        public static string GetName(this FortuneSaveData fortuneSaveData)
        {
            return Game.Instance.Fortunes.SingleOrDefault(y => y.Guid.ToString() == fortuneSaveData.Guid)?.Name;
        }

        public static string GetLocalizedName(this FortuneSaveData fortuneSaveData)
        {
            return OptionsManager.Localize(GetName(fortuneSaveData));
        }
    }
}
