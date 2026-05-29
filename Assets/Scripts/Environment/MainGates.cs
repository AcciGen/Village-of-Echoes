using UnityEngine;

public class MainGates : MonoBehaviour
{
    [Header("Gate Settings")]
    [SerializeField] private Transform _gateTransform;
    [SerializeField] private float _openedHeight = 5.5f;
    [SerializeField] private float _closedHeight = 2.9f;
    [SerializeField] private float _speed = 1f;
    [SerializeField] private bool _shouldOpen = true;

    [Header("Audio References")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _moveSound;

    void Update()
    {
        if (_gateTransform == null) return;

        float targetY = _shouldOpen ? _openedHeight : _closedHeight;
        float currentY = _gateTransform.localPosition.y;

        if (!Mathf.Approximately(currentY, targetY))
        {
            MoveGate(targetY);
            HandleSound();
        }
        else
        {
            StopSoundIfDone();
        }
    }

    void MoveGate(float targetY)
    {
        float newY = Mathf.MoveTowards(_gateTransform.localPosition.y, targetY, _speed * Time.deltaTime);

        Vector3 currentPos = _gateTransform.localPosition;
        _gateTransform.localPosition = new Vector3(currentPos.x, newY, currentPos.z);
    }

    void HandleSound()
    {
        if (_audioSource == null || _moveSound == null) return;

        if (!_audioSource.isPlaying)
        {
            _audioSource.clip = _moveSound;
            _audioSource.loop = true;
            _audioSource.Play();
        }
    }

    void StopSoundIfDone()
    {
        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();
    }

    public void OpenGate() => _shouldOpen = true;
    public void CloseGate() => _shouldOpen = false;
}
