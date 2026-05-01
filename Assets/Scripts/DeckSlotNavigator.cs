public partial class DeckManager
{
    private sealed class DeckSlotNavigator
    {
        private readonly DeckManager _owner;

        public DeckSlotNavigator(DeckManager owner)
        {
            _owner = owner;
        }

        public void UpdateFrame()
        {
            if (DeckManager.IsBuildbeckSceneActive())
            {
                _owner.BindExternalSlotButtonsIfNeeded();
                _owner.TryApplyDeckLayoutLiveTuning();
            }
            else if (_owner.deckSlotGuideDotsRoot != null)
            {
                _owner.CleanupDeckSlotGuideDots();
            }
        }
    }
}
