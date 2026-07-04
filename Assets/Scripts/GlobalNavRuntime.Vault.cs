using UnityEngine;

public partial class GlobalNavRuntime : MonoBehaviour
{
    private void ToggleValuablesVaultPanel()
    {
        EnsureValuablesVaultOverlay();
        if (valuablesVaultOverlay == null) return;
        if (valuablesVaultOverlay.IsOpen)
            valuablesVaultOverlay.Close();
        else
            OpenValuablesVaultOverlay();
    }

    private bool OpenValuablesVaultOverlay()
    {
        EnsureValuablesVaultOverlay();
        if (valuablesVaultOverlay == null) return false;
        if (playerInfoOverlayRoot != null) playerInfoOverlayRoot.SetActive(false);
        valuablesVaultOverlay.Open();
        return true;
    }

    private void CloseValuablesVaultOverlay()
    {
        valuablesVaultOverlay?.Close();
        if (ValuablesVaultState.HasPendingChanges)
            PlayerSaveCoordinator.FlushDebouncedThenSavePlayerData();
    }

    private void EnsureValuablesVaultOverlay()
    {
        if (view == null || view.rootCanvas == null) return;
        if (valuablesVaultOverlay == null)
        {
            valuablesVaultOverlay = new GlobalNavValuablesVaultOverlay(
                ValuablesVaultFonts.ApplyTo,
                CreatePlayerInfoStyleCloseButton);
        }

        valuablesVaultOverlay.EnsureBuilt(view.rootCanvas.transform);
    }
}
