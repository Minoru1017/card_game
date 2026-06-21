using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class FightingBirdGameSceneController
{
    // ----------------------------------------------------------------- input

    private void RegisterInput(BirdGesture gesture)
    {
        if (!inputWindowOpen || inputLocked || currentBeatDsp <= 0d) return;

        float timingErrorNow = Mathf.Abs((float)(AudioSettings.dspTime - currentBeatDsp));
        if (timingErrorNow > goodWindow) return;

        pendingInput = gesture;
        pendingInputDsp = AudioSettings.dspTime;
        inputLocked = true;
        FlashButton(gesture);
    }

    private void HandleKeyboard()
    {
        if (!inputWindowOpen || inputLocked) return;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) RegisterInput(BirdGesture.Peck);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) RegisterInput(BirdGesture.Wing);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) RegisterInput(BirdGesture.Nest);
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Space))
            RegisterInput(BirdGesture.Pass);
    }

    private void SetButtonsInteractable(bool value)
    {
        foreach (KeyValuePair<BirdGesture, Image> pair in buttonImages)
        {
            if (pair.Value == null) continue;
            Button btn = pair.Value.GetComponent<Button>();
            if (btn != null) btn.interactable = value;
            Color c = pair.Value.color;
            c.a = value ? 1f : 0.45f;
            pair.Value.color = c;
        }
    }
}
