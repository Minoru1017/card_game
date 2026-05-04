using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// TMP_InputField 的 <see cref="TMP_InputField.OnUpdateSelected"/> 多半只在 KeyDown 路徑呼叫
/// <c>UpdateLabel</c>；注音等 IME 在組字過程中可能沒有對應事件，導致框內不顯示組字，直到送出漢字。
/// 在偵測到組字字串時多呼叫一次 <c>UpdateLabel</c> 即可與內建行為相容。
/// 另：部分桌面環境 <c>TouchScreenKeyboard.isSupported</c> 為 true 時，TMP 不會對 BaseInput 設 IME，
/// 於 <see cref="OnSelect"/> 補上 <c>imeCompositionMode</c>。
/// </summary>
public class TmpInputFieldImeRedraw : TMP_InputField
{
    public override void OnSelect(BaseEventData eventData)
    {
        EnsureImeCompositionOnInputModule();
        base.OnSelect(eventData);
    }

    public override void OnUpdateSelected(BaseEventData eventData)
    {
        base.OnUpdateSelected(eventData);
        if (!isFocused || readOnly) return;
        if (!HasActiveImeComposition()) return;
        UpdateLabel();
    }

    static void EnsureImeCompositionOnInputModule()
    {
        if (EventSystem.current == null) return;
        if (EventSystem.current.currentInputModule is StandaloneInputModule sim && sim.input != null)
            sim.input.imeCompositionMode = IMECompositionMode.On;
    }

    static bool HasActiveImeComposition()
    {
        if (EventSystem.current != null &&
            EventSystem.current.currentInputModule is StandaloneInputModule sim &&
            sim.input != null &&
            !string.IsNullOrEmpty(sim.input.compositionString))
            return true;
        return !string.IsNullOrEmpty(Input.compositionString);
    }
}
