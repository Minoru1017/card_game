using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainPlotSceneController : MonoBehaviour
{
    [System.Serializable]
    public class PlotStep
    {
        [Header("Story Content")]
        public string speakerName;
        [TextArea(2, 6)] public string dialogueText;
        public Sprite backgroundSprite;
        public Sprite characterASprite;
        public Sprite characterBSprite;
        public Sprite characterCSprite;

        [Header("Player Choices")]
        public string choice1Text = "選擇一";
        public string choice2Text = "選擇二";
        public string choice3Text = "選擇三";

        [Header("Choice Next Step Index (-1 = hide)")]
        public int choice1Next = -1;
        public int choice2Next = -1;
        public int choice3Next = -1;
    }

    [Header("Scene UI Refs (auto-bind by name if empty)")]
    [SerializeField] private TMP_Text dialogueTextTmp;
    [SerializeField] private TMP_Text speakerNameTmp;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image characterAImage;
    [SerializeField] private Image characterBImage;
    [SerializeField] private Image characterCImage;
    [SerializeField] private Button choice1Button;
    [SerializeField] private Button choice2Button;
    [SerializeField] private Button choice3Button;
    [SerializeField] private TMP_Text choice1TextTmp;
    [SerializeField] private TMP_Text choice2TextTmp;
    [SerializeField] private TMP_Text choice3TextTmp;

    [Header("Script Data")]
    [SerializeField] private List<PlotStep> steps = new List<PlotStep>();
    [SerializeField] private int startStepIndex;

    private int currentStepIndex = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawnForMainPlot()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Main Plot") return;
        if (Object.FindFirstObjectByType<MainPlotSceneController>() != null) return;

        GameObject host = new GameObject("MainPlotSceneController");
        host.AddComponent<MainPlotSceneController>();
    }

    private void Awake()
    {
        AutoBindUiIfMissing();
        WireChoiceButtons();
    }

    private void Start()
    {
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning("MainPlotSceneController: no steps configured.");
            return;
        }

        ShowStep(Mathf.Clamp(startStepIndex, 0, steps.Count - 1));
    }

    private void AutoBindUiIfMissing()
    {
        if (dialogueTextTmp == null) dialogueTextTmp = FindTmpByName("劇本文字");
        if (speakerNameTmp == null) speakerNameTmp = FindTmpByName("角色名稱");
        if (backgroundImage == null) backgroundImage = FindImageByName("文字背板");
        if (characterAImage == null) characterAImage = FindImageByName("角色A");
        if (characterBImage == null) characterBImage = FindImageByName("角色B");
        if (characterCImage == null) characterCImage = FindImageByName("角色C");
        if (choice1Button == null) choice1Button = FindButtonByName("玩家選擇按鈕1");
        if (choice2Button == null) choice2Button = FindButtonByName("玩家選擇按鈕2");
        if (choice3Button == null) choice3Button = FindButtonByName("玩家選擇按鈕3");

        if (choice1TextTmp == null && choice1Button != null)
            choice1TextTmp = choice1Button.GetComponentInChildren<TMP_Text>(true);
        if (choice2TextTmp == null && choice2Button != null)
            choice2TextTmp = choice2Button.GetComponentInChildren<TMP_Text>(true);
        if (choice3TextTmp == null && choice3Button != null)
            choice3TextTmp = choice3Button.GetComponentInChildren<TMP_Text>(true);
    }

    private static TMP_Text FindTmpByName(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.GetComponent<TMP_Text>() : null;
    }

    private static Image FindImageByName(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.GetComponent<Image>() : null;
    }

    private static Button FindButtonByName(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private void WireChoiceButtons()
    {
        if (choice1Button != null)
        {
            choice1Button.onClick.RemoveAllListeners();
            choice1Button.onClick.AddListener(() => OnChoiceClicked(0));
        }
        if (choice2Button != null)
        {
            choice2Button.onClick.RemoveAllListeners();
            choice2Button.onClick.AddListener(() => OnChoiceClicked(1));
        }
        if (choice3Button != null)
        {
            choice3Button.onClick.RemoveAllListeners();
            choice3Button.onClick.AddListener(() => OnChoiceClicked(2));
        }
    }

    private void OnChoiceClicked(int choiceIndex)
    {
        if (currentStepIndex < 0 || currentStepIndex >= steps.Count) return;

        PlotStep step = steps[currentStepIndex];
        int nextIndex = -1;
        switch (choiceIndex)
        {
            case 0: nextIndex = step.choice1Next; break;
            case 1: nextIndex = step.choice2Next; break;
            case 2: nextIndex = step.choice3Next; break;
        }

        if (nextIndex < 0 || nextIndex >= steps.Count)
        {
            HideAllChoiceButtons();
            return;
        }

        ShowStep(nextIndex);
    }

    public void ShowStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= steps.Count) return;

        currentStepIndex = stepIndex;
        PlotStep step = steps[stepIndex];

        if (speakerNameTmp != null) speakerNameTmp.text = step.speakerName ?? string.Empty;
        if (dialogueTextTmp != null) dialogueTextTmp.text = step.dialogueText ?? string.Empty;

        ApplyImageSprite(backgroundImage, step.backgroundSprite);
        ApplyImageSprite(characterAImage, step.characterASprite);
        ApplyImageSprite(characterBImage, step.characterBSprite);
        ApplyImageSprite(characterCImage, step.characterCSprite);

        SetupChoiceButton(choice1Button, choice1TextTmp, step.choice1Text, step.choice1Next);
        SetupChoiceButton(choice2Button, choice2TextTmp, step.choice2Text, step.choice2Next);
        SetupChoiceButton(choice3Button, choice3TextTmp, step.choice3Text, step.choice3Next);
    }

    private static void ApplyImageSprite(Image image, Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private static void SetupChoiceButton(Button button, TMP_Text textTmp, string label, int nextIndex)
    {
        if (button == null) return;
        bool visible = !string.IsNullOrWhiteSpace(label) && nextIndex >= 0;
        button.gameObject.SetActive(visible);
        if (visible && textTmp != null) textTmp.text = label;
    }

    private void HideAllChoiceButtons()
    {
        if (choice1Button != null) choice1Button.gameObject.SetActive(false);
        if (choice2Button != null) choice2Button.gameObject.SetActive(false);
        if (choice3Button != null) choice3Button.gameObject.SetActive(false);
    }
}
