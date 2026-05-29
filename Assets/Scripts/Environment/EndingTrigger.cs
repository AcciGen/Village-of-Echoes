using System.Collections;
using TMPro;
using UHFPS.Runtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndingTrigger : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private CanvasGroup _endingPanelGroup;
    [SerializeField] private Image _fadeImage;
    [SerializeField] private TextMeshProUGUI _endingTitleText;
    [SerializeField] private TextMeshProUGUI _statText;

    [Header("Animation Settings")]
    [SerializeField] private float _fadeSpeed = 1f;
    [SerializeField] private float _targetTextScale = 1.2f;
    [SerializeField] private float _delayBeforeMenuReturn = 10f;

    [Header("Audio References")]
    [SerializeField] private AudioSource _carSound;
    [SerializeField] private AudioClip _carAudioClip;

    private Collider _triggerCollider;
    private bool _isEnding = false;

    private const float _MinProgress = 0f;
    private const float _MaxProgress = 1f;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider>();
        if (_triggerCollider != null)
            _triggerCollider.enabled = false;
    }

    public void EnableEndingTrigger()
    {
        if (_triggerCollider != null)
            _triggerCollider.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_isEnding)
        {
            _isEnding = true;

            if (other.TryGetComponent(out CharacterController characterController))
                characterController.enabled = false;

            StartCoroutine(PlayEnding());
        }
    }

    IEnumerator PlayEnding()
    {
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.StopTimerAndCheckRecord();

            if (_statText != null)
            {
                string currentTimeStr = GameEconomy.Instance.FormatTime(GameEconomy.Instance.ElapsedTime);
                string bestTimeStr = GameEconomy.Instance.FormatTime(GameEconomy.Instance.BestTime);

                _statText.text = $"Papers Found: {GameEconomy.Instance.CollectedPapers} / {GameEconomy.Instance.TotalPapers}\n" +
                                 $"Your Time: {currentTimeStr}\n" +
                                 $"Best Time: {bestTimeStr}";
            }
        }

        if (_carSound != null && _carAudioClip != null)
        {
            _carSound.clip = _carAudioClip;
            _carSound.Play();
        }

        float fadeProgress = _MinProgress;
        while (fadeProgress < _MaxProgress)
        {
            fadeProgress += Time.deltaTime * _fadeSpeed;
            if (_endingPanelGroup != null)
                _endingPanelGroup.alpha = fadeProgress;
            yield return null;
        }

        if (_endingTitleText != null)
        {
            float scaleProgress = _MinProgress;
            Vector3 targetScale = new Vector3(_targetTextScale, _targetTextScale, _targetTextScale);

            while (scaleProgress < 1f)
            {
                scaleProgress += Time.deltaTime * _fadeSpeed;
                _endingTitleText.transform.localScale = Vector3.Lerp(Vector3.one, targetScale, scaleProgress);
                yield return null;
            }
        }

        yield return new WaitForSeconds(_delayBeforeMenuReturn);

        SceneManager.LoadScene(0);
    }
}
