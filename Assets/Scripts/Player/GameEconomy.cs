using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameEconomy : MonoBehaviour
{
    public static GameEconomy Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text _papersText;
    [SerializeField] private TMP_Text _timerText;

    [Header("Game Settings")]
    [SerializeField] private int _totalPapersToFind = 9;

    private int _collectedPapers;
    private float _elapsedTime;
    private bool _isTimerRunning = true;
    private float _bestTime;

    private HashSet<int> _readPaperIDs = new HashSet<int>();
    private const string BestTimeSaveKey = "PlayerBestTime";
    private const float NoBestTimeValue = 999999f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadGameData();
    }

    void Start()
    {
        UpdatePapersUI();
    }

    void Update()
    {
        if (_isTimerRunning && Time.timeScale > 0f)
        {
            _elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void CollectPaper(GameObject paperObject)
    {
        if (paperObject == null) return;

        int paperID = paperObject.GetInstanceID();

        if (_readPaperIDs.Add(paperID))
        {
            _collectedPapers++;
            UpdatePapersUI();
        }
    }

    public void StopTimerAndCheckRecord()
    {
        _isTimerRunning = false;

        if (_elapsedTime < _bestTime)
        {
            _bestTime = _elapsedTime;
            PlayerPrefs.SetFloat(BestTimeSaveKey, _bestTime);
            PlayerPrefs.Save();
        }
    }

    private void UpdatePapersUI()
    {
        if (_papersText != null)
            _papersText.text = $"Papers: {_collectedPapers} / {_totalPapersToFind}";
    }

    private void UpdateTimerUI()
    {
        if (_timerText != null)
            _timerText.text = $"Time: {FormatTime(_elapsedTime)}";
    }

    public string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds >= NoBestTimeValue - 1f) return "--:--";

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void LoadGameData()
    {
        _bestTime = PlayerPrefs.GetFloat(BestTimeSaveKey, NoBestTimeValue);
    }

    public int CollectedPapers => _collectedPapers;
    public int TotalPapers => _totalPapersToFind;
    public float ElapsedTime => _elapsedTime;
    public float BestTime => _bestTime;

    [ContextMenu("Reset Best Time Record")]
    public void ResetData()
    {
        PlayerPrefs.DeleteKey(BestTimeSaveKey);
        _bestTime = NoBestTimeValue;
        _elapsedTime = 0f;
        UpdatePapersUI();
    }
}
