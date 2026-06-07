using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public event Action TimerEnded;

    [SerializeField] private TMP_Text timerText;
    [SerializeField, Min(1f)] private float warningSeconds = 15f;
    [SerializeField, Min(0.1f)] private float pulseStartSeconds = 5f;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 3f;
    [SerializeField, Range(0f, 1f)] private float pulseMinAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float pulseMaxAlpha = 1f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;

    private Coroutine _timerRoutine;
    private float _remainingSeconds;
    private bool _isRunning;
    private float _baseFontSize;
    private bool _hasBaseFontSize;

    private void Awake()
    {
        if (timerText != null)
        {
            _baseFontSize = timerText.fontSize;
            _hasBaseFontSize = true;
        }
        SetVisible(false);
    }

    public void Begin(float durationSeconds)
    {
        StopTimer();
        _remainingSeconds = Mathf.Max(0f, durationSeconds);
        _isRunning = true;
        SetVisible(true);
        UpdateText();
        _timerRoutine = StartCoroutine(RunTimer());
    }

    public void StopTimer()
    {
        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        _isRunning = false;
    }

    public void Hide()
    {
        StopTimer();
        SetVisible(false);
    }

    private IEnumerator RunTimer()
    {
        while (_isRunning && _remainingSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(1f);
            if (!_isRunning)
                yield break;

            _remainingSeconds = Mathf.Max(0f, _remainingSeconds - 1f);
            UpdateText();
        }

        _timerRoutine = null;
        _isRunning = false;
        TimerEnded?.Invoke();
    }

    private void UpdateText()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(_remainingSeconds);
        timerText.text = FormatTime(seconds);

        if (_remainingSeconds <= pulseStartSeconds)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f;
            float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, pulse);
            Color color = warningColor;
            color.a = alpha;
            timerText.color = color;

            if (_hasBaseFontSize)
            {
                float scale = Mathf.Lerp(0.96f, 1.08f, pulse);
                timerText.fontSize = _baseFontSize * scale;
            }
        }
        else if (_remainingSeconds <= warningSeconds)
        {
            timerText.color = warningColor;

            if (_hasBaseFontSize)
                timerText.fontSize = _baseFontSize;
        }
        else
        {
            timerText.color = normalColor;

            if (_hasBaseFontSize)
                timerText.fontSize = _baseFontSize;
        }
    }

    private string FormatTime(int totalSeconds)
    {
        totalSeconds = Mathf.Max(0, totalSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    private void SetVisible(bool visible)
    {
        if (timerText != null)
            timerText.gameObject.SetActive(visible);
    }
}
