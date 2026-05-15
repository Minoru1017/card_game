using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 點擊後先播放「泡泡」式縮放（放大再縮回），結束後才執行回呼（例如換場）。
/// </summary>
[RequireComponent(typeof(Button))]
public class ReturnButtonClickFeedback : MonoBehaviour
{
    [SerializeField] private float expandScale = 1.18f;
    [SerializeField] private float expandDuration = 0.12f;
    [SerializeField] private float contractDuration = 0.16f;

    private Button _button;
    private UnityAction _onComplete;
    private UnityAction _clickHandler;
    private Coroutine _co;
    private bool _busy;
    private Vector3 _baseLocalScale;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _baseLocalScale = transform.localScale;
    }

    public void Configure(UnityAction onCompleteAfterAnimation)
    {
        if (_button == null) _button = GetComponent<Button>();
        _onComplete = onCompleteAfterAnimation;
        _baseLocalScale = transform.localScale;

        if (_clickHandler != null) _button.onClick.RemoveListener(_clickHandler);
        _clickHandler = OnClicked;
        _button.onClick.AddListener(_clickHandler);
    }

    private void OnClicked()
    {
        if (_busy) return;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoBubbleThenInvoke());
    }

    private IEnumerator CoBubbleThenInvoke()
    {
        _busy = true;
        Vector3 from = _baseLocalScale;
        Vector3 peak = from * expandScale;

        float t = 0f;
        while (t < expandDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / expandDuration);
            float s = Mathf.SmoothStep(0f, 1f, k);
            transform.localScale = Vector3.LerpUnclamped(from, peak, s);
            yield return null;
        }

        transform.localScale = peak;
        t = 0f;
        while (t < contractDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / contractDuration);
            float s = Mathf.SmoothStep(0f, 1f, k);
            transform.localScale = Vector3.LerpUnclamped(peak, from, s);
            yield return null;
        }

        transform.localScale = from;
        _busy = false;
        _co = null;

        UnityAction cb = _onComplete;
        if (cb != null) cb.Invoke();
    }
}
