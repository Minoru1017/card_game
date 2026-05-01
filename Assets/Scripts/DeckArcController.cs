using UnityEngine;

public partial class DeckManager
{
    private sealed class DeckArcController
    {
        private readonly DeckManager _owner;

        public DeckArcController(DeckManager owner)
        {
            _owner = owner;
        }

        public void LateUpdateArc()
        {
            if (!DeckManager.NewLayoutEnableDeckArc || _owner.deckPanel == null) return;
            if (_owner._deckCardRemoveAnimationActive) return;
            if (_owner.deckArcHorizontalSmoothTime <= 0f) return;
            _owner.RequestDeckArcLayout(_owner.deckPanel as RectTransform, false);
        }
    }
}
