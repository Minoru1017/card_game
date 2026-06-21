#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 批次刷新 Resources Library 資產（Audio / UI Font / UI Sprite / Card Art）。
/// 選單：Tools/Resources/Refresh All Libraries
/// 命令列：Unity -batchmode -quit -projectPath ... -executeMethod UnityLibraryRefreshBatch.RunFromCommandLine -libraryRefresh all
/// </summary>
public static class UnityLibraryRefreshBatch
{
    private const string LogPrefix = "UnityLibraryRefresh";

    [MenuItem("Tools/Resources/Refresh All Libraries")]
    public static void RefreshAllFromMenu()
    {
        RefreshAll(logEachStep: true);
        EditorUtility.DisplayDialog("Library Refresh", "已執行全部 Library 刷新，詳見 Console。", "OK");
    }

    [MenuItem("Tools/Resources/Refresh Audio Library")]
    public static void RefreshAudioFromMenu() => RefreshAudio(log: true);

    [MenuItem("Tools/Resources/Refresh UI Font Library")]
    public static void RefreshUiFontFromMenu() => RefreshUiFont(log: true);

    [MenuItem("Tools/Resources/Refresh UI Sprite Library")]
    public static void RefreshUiSpriteFromMenu() => RefreshUiSprite(log: true);

    [MenuItem("Tools/Resources/Refresh Card Art Pipeline")]
    public static void RefreshCardArtFromMenu() => RefreshCardArtPipeline(log: true);

    /// <summary>Batch-mode entry for PowerShell / CI.</summary>
    public static void RunFromCommandLine()
    {
        string target = GetCommandLineArg("-libraryRefresh") ?? "all";
        bool ok = RunTarget(target, log: true);
        EditorApplication.Exit(ok ? 0 : 1);
    }

    public static bool RunTarget(string target, bool log)
    {
        switch ((target ?? "all").Trim().ToLowerInvariant())
        {
            case "all":
                RefreshAll(logEachStep: log);
                return true;
            case "audio":
                RefreshAudio(log);
                return true;
            case "ui-font":
            case "font":
                RefreshUiFont(log);
                return true;
            case "ui-sprite":
            case "sprite":
                RefreshUiSprite(log);
                return true;
            case "card-art":
            case "cardart":
                RefreshCardArtPipeline(log);
                return true;
            case "fonts-full":
            case "cjk":
                RefreshFontsFull(log);
                return true;
            default:
                UnityEngine.Debug.LogError($"{LogPrefix}: unknown -libraryRefresh target '{target}'. " +
                    "Use: all | audio | ui-font | ui-sprite | card-art | fonts-full");
                return false;
        }
    }

    private static void RefreshAll(bool logEachStep)
    {
        RefreshAudio(logEachStep);
        RefreshUiFont(logEachStep);
        RefreshUiSprite(logEachStep);
        RefreshCardArtPipeline(logEachStep);
        AssetDatabase.SaveAssets();
        if (logEachStep)
            UnityEngine.Debug.Log($"{LogPrefix}: Refresh All complete.");
    }

    private static void RefreshAudio(bool log)
    {
        EditorApplication.ExecuteMenuItem("Tools/Audio/Create or Refresh Audio Library");
        if (log) UnityEngine.Debug.Log($"{LogPrefix}: AudioLibrary refreshed.");
    }

    private static void RefreshUiFont(bool log)
    {
        EditorApplication.ExecuteMenuItem("Tools/UI/Create or Refresh UI Font Library");
        if (log) UnityEngine.Debug.Log($"{LogPrefix}: UiFontLibrary refreshed.");
    }

    private static void RefreshUiSprite(bool log)
    {
        EditorApplication.ExecuteMenuItem("Tools/UI/Create or Refresh UI Sprite Library");
        if (log) UnityEngine.Debug.Log($"{LogPrefix}: UiSpriteLibrary refreshed.");
    }

    private static void RefreshCardArtPipeline(bool log)
    {
        EditorApplication.ExecuteMenuItem("Tools/Card Art/Rescan UI Images And Rebind");
        EditorApplication.ExecuteMenuItem("Tools/Card Art/Create or Refresh Card Art Library");
        if (log) UnityEngine.Debug.Log($"{LogPrefix}: Card art rescanned + CardArtLibrary refreshed.");
    }

    private static void RefreshFontsFull(bool log)
    {
        EditorApplication.ExecuteMenuItem("Tools/UI/Configure Noto CJK Font (Dynamic)");
        RefreshUiFont(log: false);
        if (log) UnityEngine.Debug.Log($"{LogPrefix}: Noto CJK configured + UiFontLibrary refreshed.");
    }

    private static string GetCommandLineArg(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }

        return null;
    }
}
#endif
