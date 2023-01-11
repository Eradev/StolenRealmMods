namespace DataDumper
{
    class DifficultyModifiers
    {
        public DifficultyModifiers(DifficultySetting setting)
        {
            LootModifier = setting.LootModifierPerc;
        }

        public float LootModifier;
    }
}
