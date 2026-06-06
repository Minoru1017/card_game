using TMPro;
using UnityEngine;

/// <summary>
/// TMP 字型註冊表（直接引用，取代 Resources.FindObjectsOfTypeAll 掃描與字串式 Resources.Load）。
/// D 類：CJK 主字型（Noto Sans TC）、預設 UI 字型（LiberationSans）。
///
/// 取得方式：UiFontLibrary.Instance（一次性從 Resources/UiFontLibrary.asset 載入並快取）。
/// 直接引用可確保 CJK 字型一定被打包並載入，免去掃描在某些場景找不到的脆弱性。
/// </summary>
[CreateAssetMenu(fileName = "UiFontLibrary", menuName = "Card Game/UI Font Library", order = 3)]
public sealed class UiFontLibrary : ScriptableObject
{
    public const string ResourcesPath = "UiFontLibrary";

    [Header("CJK 主字型（Noto Sans TC，支援繁中標點）")]
    [SerializeField] private TMP_FontAsset cjkFont;

    [Header("預設 UI 字型（LiberationSans 等拉丁字型）")]
    [SerializeField] private TMP_FontAsset defaultUiFont;

    public TMP_FontAsset CjkFont => cjkFont;
    public TMP_FontAsset DefaultUiFont => defaultUiFont;

    private static UiFontLibrary instance;
    private static bool instanceLoaded;

    /// <summary>一次性載入並快取的單例；找不到資產時回傳 null（呼叫端應退回舊解析方式）。</summary>
    public static UiFontLibrary Instance
    {
        get
        {
            if (!instanceLoaded)
            {
                instance = Resources.Load<UiFontLibrary>(ResourcesPath);
                instanceLoaded = true;
                if (instance == null)
                {
                    Debug.LogWarning(
                        $"UiFontLibrary: 找不到 Resources/{ResourcesPath}.asset，" +
                        "請執行 Tools/UI/Create or Refresh UI Font Library；暫時回退舊字型解析方式。");
                }
            }
            return instance;
        }
    }

#if UNITY_EDITOR
    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetFonts(TMP_FontAsset cjk, TMP_FontAsset defaultUi)
    {
        cjkFont = cjk;
        defaultUiFont = defaultUi;
    }
#endif
}
