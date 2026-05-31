using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    private void TryPlayOpeningRollPresentation()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (TutorialPlotBattleTransition.IsPlaying) return;
        if (battleManager == null || openingRollPanel == null) return;
        int version = battleManager.GetOpeningRollVersion();
        if (version <= 0 || version == lastOpeningRollVersion) return;
        lastOpeningRollVersion = version;

        if (openingRollRoutine != null) StopCoroutine(openingRollRoutine);
        openingRollRoutine = StartCoroutine(PlayOpeningRollPresentation());
    }

    private IEnumerator PlayOpeningRollPresentation()
    {
        if (openingRollPanel == null || openingRollGroup == null || openingRollDiceText == null || openingRollFirstText == null)
            yield break;

        int playerDice = battleManager.GetOpeningPlayerDice();
        int enemyDice = battleManager.GetOpeningEnemyDice();
        bool playerFirst = battleManager.IsOpeningPlayerFirst();

        openingRollDiceText.text =
            "Player " + playerDice +
            "    VS    " +
            enemyDice + " Enemy";
        openingRollFirstText.text = playerFirst ? "Player goes first" : "Enemy goes first";
        SetDicePips(openingPlayerDicePips, playerDice);
        SetDicePips(openingEnemyDicePips, enemyDice);
        openingRollPanel.gameObject.SetActive(true);
        openingRollPanel.transform.SetAsLastSibling();
        openingRollPanel.localScale = Vector3.one * 0.82f;
        openingRollGroup.alpha = 0f;
        PlayUIClip(openingRollSfx, 0.9f);

        float t = 0f;
        const float popDur = 0.22f;
        while (t < popDur && openingRollPanel != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / popDur);
            float eased = p * p * (3f - 2f * p);
            openingRollGroup.alpha = eased;
            openingRollPanel.localScale = Vector3.Lerp(Vector3.one * 0.82f, Vector3.one * 1.06f, eased);
            yield return null;
        }

        if (openingRollPanel != null) openingRollPanel.localScale = Vector3.one;
        if (openingRollGroup != null) openingRollGroup.alpha = 1f;

        const float fadeOutDur = 0.35f;
        float configured = battleManager != null ? battleManager.GetOpeningPresentationSeconds() : 3f;
        float holdAfterPop = Mathf.Max(0.04f, configured - popDur - fadeOutDur);
        yield return new WaitForSecondsRealtime(holdAfterPop);

        t = 0f;
        while (t < fadeOutDur && openingRollGroup != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeOutDur);
            openingRollGroup.alpha = 1f - p;
            yield return null;
        }

        if (openingRollGroup != null) openingRollGroup.alpha = 0f;
        if (openingRollPanel != null) openingRollPanel.gameObject.SetActive(false);
        openingRollRoutine = null;
        ApplyBattleTurnBannerStackOrder();
    }

    private Image[] CreateDicePipGrid(Transform parent, string name, Vector2 anchoredPos)
    {
        GameObject diceObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        diceObj.transform.SetParent(parent, false);
        RectTransform diceRt = diceObj.GetComponent<RectTransform>();
        diceRt.anchorMin = new Vector2(0.5f, 0.5f);
        diceRt.anchorMax = new Vector2(0.5f, 0.5f);
        diceRt.pivot = new Vector2(0.5f, 0.5f);
        diceRt.anchoredPosition = anchoredPos;
        diceRt.sizeDelta = new Vector2(44f, 44f);
        Image bg = diceObj.GetComponent<Image>();
        bg.sprite = GetUnitWhiteSprite();
        bg.color = BattleFxColors.DiceFace;

        Image[] pips = new Image[9];
        int idx = 0;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                GameObject pipObj = new GameObject("Pip_" + idx, typeof(RectTransform), typeof(Image));
                pipObj.transform.SetParent(diceObj.transform, false);
                RectTransform pipRt = pipObj.GetComponent<RectTransform>();
                pipRt.anchorMin = new Vector2(0.5f, 0.5f);
                pipRt.anchorMax = new Vector2(0.5f, 0.5f);
                pipRt.pivot = new Vector2(0.5f, 0.5f);
                pipRt.sizeDelta = new Vector2(7f, 7f);
                float x = (col - 1) * 13f;
                float y = (1 - row) * 13f;
                pipRt.anchoredPosition = new Vector2(x, y);
                Image pip = pipObj.GetComponent<Image>();
                pip.sprite = GetUnitWhiteSprite();
                pip.color = BattleFxColors.DicePipOn;
                pip.enabled = false;
                pips[idx] = pip;
                idx++;
            }
        }
        return pips;
    }

    private void SetDicePips(Image[] pips, int value)
    {
        if (pips == null || pips.Length != 9) return;
        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] != null) pips[i].enabled = false;
        }

        EnablePip(pips, 4); // center for odd defaults as needed
        if (value == 1)
        {
            return;
        }

        // corners: 0=TL,2=TR,6=BL,8=BR; mids:1=TM,3=ML,5=MR,7=BM
        if (value >= 2)
        {
            EnablePip(pips, 0);
            EnablePip(pips, 8);
        }
        if (value >= 4)
        {
            EnablePip(pips, 2);
            EnablePip(pips, 6);
        }
        if (value == 3 || value == 5)
        {
            EnablePip(pips, 4);
        }
        if (value == 6)
        {
            EnablePip(pips, 3);
            EnablePip(pips, 5);
        }
        if (value == 2 || value == 4 || value == 6)
        {
            if (pips[4] != null) pips[4].enabled = false;
        }
    }

    private void EnablePip(Image[] pips, int index)
    {
        if (index < 0 || index >= pips.Length) return;
        if (pips[index] == null) return;
        pips[index].enabled = true;
        pips[index].color = BattleFxColors.DicePipOn;
    }

    private AudioClip CreateProceduralDiceRollClip()
    {
        const int sampleRate = 22050;
        const float duration = 0.28f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float noise = Random.Range(-1f, 1f);
            float tone = Mathf.Sin(2f * Mathf.PI * 940f * t) * 0.25f;
            float env = Mathf.Clamp01(1f - (t / duration));
            env *= env;
            samples[i] = (noise * 0.4f + tone) * env * 0.7f;
        }

        AudioClip clip = AudioClip.Create("DiceRollProcedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

}
