using UnityEngine;

public static class GlobalNavBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitGlobalNav()
    {
        GlobalNavRuntime.EnsureInitialized();
    }
}
