using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DeckManager
{
    private sealed class DeckDataController
    {
        private readonly DeckManager _owner;

        public DeckDataController(DeckManager owner)
        {
            _owner = owner;
        }

        public void ClearPanels()
        {
            ClearPanelChildren(_owner.libraryPanel);
            ClearPanelChildren(_owner.deckPanel);

            _owner.libraryDic.Clear();
            _owner.deckDic.Clear();
        }

        /// <summary>
        /// Removes card instances only. Keeps subtrees that contain save/disband toolbar buttons under the grid.
        /// </summary>
        private void ClearPanelChildren(Transform panel)
        {
            if (panel == null) return;

            var remove = new List<Transform>(panel.childCount);
            foreach (Transform t in panel)
            {
                if (ShouldKeepChildBecauseOfProtectedDeckToolbarButton(t)) continue;
                remove.Add(t);
            }

            for (int i = 0; i < remove.Count; i++)
            {
                if (remove[i] != null)
                    Object.Destroy(remove[i].gameObject);
            }
        }

        private bool ShouldKeepChildBecauseOfProtectedDeckToolbarButton(Transform directChild)
        {
            if (directChild == null) return false;
            if (SubtreeContainsButton(directChild, _owner.saveDeckButton)) return true;
            if (SubtreeContainsButton(directChild, _owner.disbandDeckButton)) return true;
            if (SubtreeContainsButton(directChild, _owner.editDeckNameButton)) return true;
            return false;
        }

        private static bool SubtreeContainsButton(Transform directChild, Button btn)
        {
            if (btn == null) return false;
            Transform s = btn.transform;
            return s == directChild || s.IsChildOf(directChild);
        }

        public void UpdateLibrary()
        {
            _owner.EnsureCoreRefs();
            _owner.EnsureDeckUIRefs();
            if (_owner.PlayerData == null) return;
            foreach (var kv in _owner.PlayerData.playerCollection)
            {
                if (kv.Value > 0)
                    _owner.CreateCard(kv.Key, CardState.Library);
            }
        }

        public void UpdateDeck()
        {
            _owner.EnsureCoreRefs();
            _owner.EnsureDeckUIRefs();
            if (_owner.PlayerData == null) return;
            foreach (var kv in _owner.PlayerData.GetDeckMap(_owner.PlayerData.selectedDeckSlot))
            {
                if (kv.Value > 0)
                    _owner.CreateCard(kv.Key, CardState.Deck);
            }
        }

        public void UpdataCard(CardState state, int id)
        {
            bool deferDeckForceRebuild = false;
            if (state == CardState.Deck)
            {
                if (!_owner.deckDic.ContainsKey(id))
                    return;

                _owner.PlayerData.AddSelectedDeckCount(id, -1);
                _owner.PlayerData.AddCollection(id, 1);

                if (_owner.PlayerData.GetSelectedDeckCount(id) <= 0)
                {
                    GameObject removingObj = _owner.deckDic[id];
                    bool bottomListRow = _owner.ComputeDeckCardIsBottomListRow(removingObj);
                    _owner.deckDic.Remove(id);
                    _owner.StartCoroutine(_owner.AnimateDeckCardRemove(removingObj, bottomListRow));
                    deferDeckForceRebuild = true;
                }
                else
                {
                    int deckCount = _owner.PlayerData.GetSelectedDeckCount(id);
                    GameObject deckGo = _owner.deckDic[id];
                    CardCounter dcc = deckGo.GetComponent<CardCounter>();
                    if (dcc != null) dcc.SetCounter(deckCount);
                    _owner.SyncBuildbeckDeckStripMtOiPrimaryTexts(deckGo);
                    _owner.ApplyDeckStripStackPresentation(deckGo, deckCount);
                }

                if (_owner.libraryDic.ContainsKey(id))
                {
                    int libCount = _owner.PlayerData.GetCollectionCount(id);
                    CardCounter libCc = _owner.libraryDic[id].GetComponent<CardCounter>();
                    if (libCc != null) libCc.SetCounter(libCount);
                    _owner.ApplyLibraryDeckGenStackPresentation(_owner.libraryDic[id], libCount);
                }
                else
                    _owner.CreateCard(id, CardState.Library);
            }
            else if (state == CardState.Library)
            {
                if (!_owner.libraryDic.ContainsKey(id))
                    return;

                if (_owner.PlayerData.GetSelectedDeckTotalCount() >= _owner.maxDeckCards)
                {
                    _owner.ShowDeckHint("牌組上限為30張牌");
                    return;
                }

                _owner.PlayerData.AddSelectedDeckCount(id, 1);
                _owner.PlayerData.AddCollection(id, -1);

                if (_owner.deckDic.ContainsKey(id))
                {
                    int deckCount = _owner.PlayerData.GetSelectedDeckCount(id);
                    GameObject deckGo = _owner.deckDic[id];
                    CardCounter dcc = deckGo.GetComponent<CardCounter>();
                    if (dcc != null) dcc.SetCounter(deckCount);
                    _owner.SyncBuildbeckDeckStripMtOiPrimaryTexts(deckGo);
                    _owner.ApplyDeckStripStackPresentation(deckGo, deckCount);
                }
                else
                    _owner.CreateCard(id, CardState.Deck);

                if (_owner.PlayerData.GetCollectionCount(id) <= 0)
                {
                    Object.Destroy(_owner.libraryDic[id]);
                    _owner.libraryDic.Remove(id);
                }
                else
                {
                    int libCount = _owner.PlayerData.GetCollectionCount(id);
                    CardCounter libCc = _owner.libraryDic[id].GetComponent<CardCounter>();
                    if (libCc != null) libCc.SetCounter(libCount);
                    _owner.ApplyLibraryDeckGenStackPresentation(_owner.libraryDic[id], libCount);
                }
            }
            _owner.PlayerData.SavePlayerData();
            _owner.RefreshScrollablePanels();
            _owner.ForceRebuildPanelsLayout(!deferDeckForceRebuild);
        }
    }
}
