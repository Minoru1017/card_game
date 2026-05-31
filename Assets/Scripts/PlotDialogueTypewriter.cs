using System;
using TMPro;
using UnityEngine;

/// <summary>劇情對話逐字顯示（支援 TMP Rich Text）。</summary>
public sealed class PlotDialogueTypewriter
{
    private const int ShowAllCharacters = 100000;

    private TMP_Text _text;
    private int _totalCharacters;
    private int _revealedCharacters;
    private float _charactersPerSecond;
    private float _revealAccumulator;
    private bool _active;
    private Action _onTypingStarted;
    private Action _onTypingEnded;

    public bool IsActive => _active;
    public bool IsComplete => !_active;

    public void Begin(
        TMP_Text text,
        string richDialogue,
        float charactersPerSecond,
        Action onTypingStarted = null,
        Action onTypingEnded = null)
    {
        _onTypingStarted = onTypingStarted;
        _onTypingEnded = onTypingEnded;
        _text = text;
        _charactersPerSecond = Mathf.Max(1f, charactersPerSecond);
        _active = false;
        _totalCharacters = 0;
        _revealedCharacters = 0;

        if (_text == null)
            return;

        _text.richText = true;
        _text.text = richDialogue ?? string.Empty;
        _text.ForceMeshUpdate();

        _totalCharacters = _text.textInfo != null ? _text.textInfo.characterCount : 0;
        if (_totalCharacters <= 0)
        {
            _text.maxVisibleCharacters = ShowAllCharacters;
            _onTypingStarted = null;
            _onTypingEnded = null;
            return;
        }

        _revealedCharacters = 0;
        _revealAccumulator = 0f;
        _text.maxVisibleCharacters = 0;
        _active = true;
        _onTypingStarted?.Invoke();
    }

    public void Tick(float deltaTime)
    {
        if (!_active || _text == null)
            return;

        _revealAccumulator += _charactersPerSecond * deltaTime;
        int targetVisible = Mathf.Min(_totalCharacters, Mathf.FloorToInt(_revealAccumulator));
        if (targetVisible <= _revealedCharacters)
            return;

        _revealedCharacters = targetVisible;
        _text.maxVisibleCharacters = _revealedCharacters;

        if (_revealedCharacters >= _totalCharacters)
            Complete();
    }

    public void Complete()
    {
        if (_text != null)
            _text.maxVisibleCharacters = ShowAllCharacters;

        _revealedCharacters = _totalCharacters;
        EndTypingSession();
    }

    public void Stop() => EndTypingSession();

    private void EndTypingSession()
    {
        bool wasActive = _active;
        _active = false;
        if (!wasActive)
            return;

        Action ended = _onTypingEnded;
        _onTypingStarted = null;
        _onTypingEnded = null;
        ended?.Invoke();
    }
}
