using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardCounter : MonoBehaviour
{
    public TextMeshProUGUI counterText;
    private int count;

    public void SetCounter(int value)   // 直接設定
    {
        count = value;
        OnCounterChange();
    }

    public void AddCounter(int delta)   // +1 / -1
    {
        count += delta;
        OnCounterChange();
    }

    private void OnCounterChange()
    {
        if (counterText != null)
            counterText.text = count.ToString();
    }
}
