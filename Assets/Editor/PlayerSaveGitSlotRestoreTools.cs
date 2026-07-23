using UnityEditor;

/// <summary>
/// Legacy placeholder — Git 槽位還原已併入 <see cref="PlayerSaveRestoreWindow"/>。
/// 保留此檔以避免 Unity 因遺失 .meta 對應腳本而無法編譯。
/// </summary>
public static class PlayerSaveGitSlotRestoreTools
{
    [MenuItem("Card Game/Player Save/Restore Minoru1017 (Slot 3) From Git HEAD…", false, 55)]
    private static void OpenRestoreWindow() => PlayerSaveRestoreWindow.ShowWindow();
}
