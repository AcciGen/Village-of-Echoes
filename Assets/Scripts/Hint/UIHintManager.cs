using System.Collections;
using TMPro;
using UnityEngine;

public class UIHintManager : MonoBehaviour
{
    public static UIHintManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private CanvasGroup _hintCanvasGroup;
    [SerializeField] private TMP_Text _hintText;

    [Header("Default Settings")]
    [SerializeField] private float _defaultFadeDuration = 0.3f;
    [SerializeField] private float _defaultDisplayDuration = 3.0f;

    private Coroutine _activeHintCoroutine;

    private const float MinAlpha = 0f;
    private const float MaxAlpha = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (_hintCanvasGroup != null)
        {
            _hintCanvasGroup.alpha = MinAlpha;
        }
    }

    public void ShowHint(string message)
    {
        ShowHintDynamic(message, _defaultDisplayDuration);
    }

    void ShowHintDynamic(string message, float displayDuration)
    {
        if (_hintCanvasGroup == null || _hintText == null) return;

        if (_activeHintCoroutine != null)
        {
            StopCoroutine(_activeHintCoroutine);
        }

        _activeHintCoroutine = StartCoroutine(FadeHintRoutine(message, displayDuration));
    }

    IEnumerator FadeHintRoutine(string message, float displayDuration)
    {
        _hintText.text = message;

        float timer = 0f;
        while (timer < _defaultFadeDuration)
        {
            timer += Time.deltaTime;
            _hintCanvasGroup.alpha = Mathf.Lerp(MinAlpha, MaxAlpha, timer / _defaultFadeDuration);
            yield return null;
        }
        _hintCanvasGroup.alpha = MaxAlpha;

        yield return new WaitForSeconds(displayDuration);

        timer = 0f;
        while (timer < _defaultFadeDuration)
        {
            timer += Time.deltaTime;
            _hintCanvasGroup.alpha = Mathf.Lerp(MaxAlpha, MinAlpha, timer / _defaultFadeDuration);
            yield return null;
        }
        _hintCanvasGroup.alpha = MinAlpha;

        _activeHintCoroutine = null;
    }
}
