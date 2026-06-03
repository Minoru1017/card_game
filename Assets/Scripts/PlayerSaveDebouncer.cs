using System.Collections;
using UnityEngine;

/// <summary>Buildbeck 牌組編輯等高頻操作：合併短時間內多次存檔請求。</summary>
public sealed class PlayerSaveDebouncer : MonoBehaviour
{
    public const float DefaultDelaySeconds = 0.75f;

    private static PlayerSaveDebouncer instance;
    private Coroutine pendingSave;
    private PlayerData pendingTarget;

    public static void RequestDebouncedSave(PlayerData target = null, float delaySeconds = DefaultDelaySeconds)
    {
        PlayerData resolved = target != null ? target : PlayerData.ResolveCanonical();
        if (resolved == null)
            return;

        EnsureInstance(resolved);
        if (instance == null)
        {
            resolved.SavePlayerData();
            return;
        }

        instance.Schedule(resolved, delaySeconds);
    }

    public static bool HasPendingDebouncedSave =>
        instance != null && instance.pendingSave != null;

    public static void CancelPending()
    {
        if (instance == null)
            return;

        if (instance.pendingSave != null)
        {
            instance.StopCoroutine(instance.pendingSave);
            instance.pendingSave = null;
        }

        instance.pendingTarget = null;
    }

    private static void EnsureInstance(PlayerData anchor)
    {
        if (instance != null)
            return;

        if (anchor == null)
            return;

        instance = anchor.GetComponent<PlayerSaveDebouncer>();
        if (instance == null)
            instance = anchor.gameObject.AddComponent<PlayerSaveDebouncer>();
    }

    private void Schedule(PlayerData target, float delaySeconds)
    {
        pendingTarget = target;
        if (pendingSave != null)
            StopCoroutine(pendingSave);
        pendingSave = StartCoroutine(CoSaveAfterDelay(delaySeconds));
    }

    private IEnumerator CoSaveAfterDelay(float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, delaySeconds));
        pendingSave = null;
        PlayerData pd = pendingTarget != null ? pendingTarget : PlayerData.ResolveCanonical();
        pendingTarget = null;
        if (pd != null)
            pd.SavePlayerData();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
