namespace Kosynierzy
{
    /// <summary>One place that wipes everything a run remembers (checkpoints, photos, cards, loadout, inventory).</summary>
    public static class GameReset
    {
        public static bool HasSave => Checkpoints.SavedStep >= 0 || WeaponCatalog.LoadoutChosen;

        public static void Wipe()
        {
            Checkpoints.NewGame();
            GameProgress.NewGame();
            WeaponCatalog.NewGame();
            Inventory.NewGame(false);
            MissionFail.Clear();
        }
    }
}
