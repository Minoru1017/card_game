using UnityEngine;

public partial class DeckManager{
    private sealed class DeckListView
    {
        private readonly DeckManager _owner;

        public DeckListView(DeckManager owner)
        {
            _owner = owner;
        }

        public void EnsureDeckUIRefs()
        {
            if (_owner.libraryPanel == null)
            {
                _owner.TryResolveLibraryPanelByName();
                _owner.TryResolveLibraryPanelUnderCanvas();
            }

            if (_owner.deckPanel == null)
            {
                _owner.TryResolveDeckPanelByName();
                _owner.TryResolveDeckPanelUnderCanvas();
            }

            if (DeckManager.IsBuildbeckSceneActive() && (_owner.libraryPanel == null || _owner.deckPanel == null))
            {
                BuildbeckSceneAutoScaffold.EnsureScaffoldNow();
                if (_owner.libraryPanel == null)
                {
                    _owner.TryResolveLibraryPanelByName();
                    _owner.TryResolveLibraryPanelUnderCanvas();
                }
                if (_owner.deckPanel == null)
                {
                    _owner.TryResolveDeckPanelByName();
                    _owner.TryResolveDeckPanelUnderCanvas();
                }
            }

            if (_owner.libraryPanel != null) DeckManager.EnsureHierarchyActive(_owner.libraryPanel);
            if (_owner.deckPanel != null) DeckManager.EnsureHierarchyActive(_owner.deckPanel);
            if (_owner.libraryPanel != null)
            {
                Canvas libCanvas = _owner.libraryPanel.GetComponentInParent<Canvas>(true);
                if (libCanvas != null)
                {
                    libCanvas.enabled = true;
                    libCanvas.gameObject.SetActive(true);
                }
            }
            if (_owner.saveDeckButton != null)
            {
                DeckManager.EnsureHierarchyActive(_owner.saveDeckButton.transform);
                Canvas saveCanvas = _owner.saveDeckButton.GetComponentInParent<Canvas>(true);
                if (saveCanvas != null)
                {
                    saveCanvas.enabled = true;
                    saveCanvas.gameObject.SetActive(true);
                }
            }

            if (_owner.disbandDeckButton != null)
            {
                DeckManager.EnsureHierarchyActive(_owner.disbandDeckButton.transform);
                Canvas disbandCanvas = _owner.disbandDeckButton.GetComponentInParent<Canvas>(true);
                if (disbandCanvas != null)
                {
                    disbandCanvas.enabled = true;
                    disbandCanvas.gameObject.SetActive(true);
                }
            }

            if (_owner.editDeckNameButton != null)
            {
                DeckManager.EnsureHierarchyActive(_owner.editDeckNameButton.transform);
                Canvas editCanvas = _owner.editDeckNameButton.GetComponentInParent<Canvas>(true);
                if (editCanvas != null)
                {
                    editCanvas.enabled = true;
                    editCanvas.gameObject.SetActive(true);
                }
            }

            if (_owner.deckPanel != null)
            {
                Canvas c = _owner.deckPanel.GetComponentInParent<Canvas>(true);
                if (c != null)
                {
                    c.enabled = true;
                    c.gameObject.SetActive(true);
                }
            }

            if (_owner.librarycardPrefab == null)
            {
                GameObject fromLib = _owner.FindCardTemplateInPanel(_owner.libraryPanel);
                if (fromLib != null) _owner.librarycardPrefab = fromLib;
            }
            if (_owner.deckCardPrefab == null)
            {
                GameObject fromDeck = _owner.FindCardTemplateInPanel(_owner.deckPanel);
                if (fromDeck != null) _owner.deckCardPrefab = fromDeck;
            }

            if (_owner.deckCardPrefab == null && _owner.defaultDeckCardPrefab != null) _owner.deckCardPrefab = _owner.defaultDeckCardPrefab;
            if (_owner.librarycardPrefab == null && _owner.defaultLibraryCardPrefab != null) _owner.librarycardPrefab = _owner.defaultLibraryCardPrefab;
            if (_owner.librarycardPrefab == null && _owner.runtimeLibraryTemplate != null) _owner.librarycardPrefab = _owner.runtimeLibraryTemplate;
            if (_owner.deckCardPrefab == null && _owner.runtimeDeckTemplate != null) _owner.deckCardPrefab = _owner.runtimeDeckTemplate;
            if (_owner.librarycardPrefab == null) _owner.librarycardPrefab = _owner.deckCardPrefab;
            if (_owner.deckCardPrefab == null) _owner.deckCardPrefab = _owner.librarycardPrefab;

            _owner.ApplyNewLayoutRuntimeDefaults();
            _owner.ApplyDeckParentLayoutOffsets();
            _owner.EnsureDeckPanelSingleColumnLayout();
            _owner.ApplyDeckListContentScale();
            _owner.EnsureDeckArcPresenter();
            if (DeckManager.IsBuildbeckSceneActive())
            {
                _owner.EnsureDisbandDeckButtonDrawOrder(null);
                BuildbeckLayoutAutoBinder.TryBindCurrentDeckNameDisplay(_owner);
                _owner.RefreshCurrentDeckDisplayName();
                BuildbeckLayoutAutoBinder.TryWireBackButtonToPersistent();
                BuildbeckLayoutAutoBinder.BringBuildbeckReturnButtonToFront();
                _owner.HideBuildbeckLibraryDeckGenCanvasPrototypeForRuntime();
                _owner.HideBuildbeckDeckStripStackCanvasPrototypeForRuntime();
            }
        }

        public void RefreshScrollablePanels()
        {
            _owner.RefreshScrollablePanelHeightsLibraryThenDeck();
        }

        public void ForceRebuildPanelsLayout(bool includeDeck = true)
        {
            _owner.ForceRebuildLibraryPanelLayout();
            if (!includeDeck) return;

            _owner.ForceRebuildDeckPanelLayoutAfterNormalize();
            Canvas.ForceUpdateCanvases();
        }

        public void ForcePanelsScrollToTop()
        {
            _owner.ForcePanelScrollToTop(_owner.libraryPanel);
            _owner.ForcePanelScrollToTop(_owner.deckPanel);
        }
    }
}
