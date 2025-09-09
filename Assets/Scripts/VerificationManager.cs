// Name: Snow Cai
// Email: snowc@unr.edu

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class VerificationManager : MonoBehaviour
{
    [Header("Refs")]
    public GameManager game;
    public TMP_Text movesText;
    public Slider suspicionBar;
    public TMP_Text suspicionText;
    [SerializeField] float endPanelDelay = 0.8f; 
    bool deferFailUntilSolved = false;

    [Header("Rules")]
    [Range(0f, 1f)] public float hintPenalty = 0.1f;
    public bool failOnSolve = true;   // pressing Solve is “suspicious”

    // state
    private int moves = 0;
    private float suspicion = 0f;

    [Header("End UI")]
    public GameObject endPanel;
    public TMP_Text endTitleText;
    public TMP_Text endPercentText;
    public Button endRestartButton;
    public Button endQuitButton;
    public Button endCloseButton;

    [Header("Info UI")]
    public GameObject infoPanel;
    public Button infoButton;
    public Button infoCloseButton;
    public Button infoEndButton;

    void Start()
    {
        if (endPanel != null) endPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);

        // subscribe to events from GameManager
        game.onPlayerMove += OnPlayerMove;
        game.onPlayerSolved += OnPlayerSolved;
        game.onPlayerPressedSolve += OnPlayerPressedSolve;
        game.onPlayerHint += OnPlayerHint;

        ResetSession();
    }

    private void Update()
    {
        if (infoButton != null)
        {
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(() =>
            {
                ShowInfoPanel();
            });
        }
    }

    public void ResetSession()
    {
        moves = 0;
        suspicion = 0f;
        UpdateHUD();
        if (endPanel != null) endPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    void UpdateHUD()
    {
        if (movesText != null) movesText.text = $"Moves: {moves}";
        if (suspicionBar != null) suspicionBar.value = suspicion;
        if (suspicionText != null)
        {
            if (suspicion <= 0f) suspicionText.text = "Suspicion: None";
            else if (suspicion < 0.5f) suspicionText.text = "Suspicion: Low";
            else if (suspicion < 1f) suspicionText.text = "Suspicion: High";
            else suspicionText.text = "Suspicious!";
        }
    }

    void OnPlayerMove()
    {
        moves++;
        UpdateHUD();
    }

    void OnPlayerSolved()
    {
        // Decide verdict/percent at the moment of solve
        int percent = Mathf.Clamp(Mathf.RoundToInt((1f - suspicion) * 100f), 0, 100);
        string verdict;

        if (suspicion >= 1f || deferFailUntilSolved)
            verdict = "You are Definitely NOT Human!";
        else
            verdict = VerdictForPercent(percent);

        StartCoroutine(ShowEndPanelAfterDelay(verdict, percent, endPanelDelay));
    }

    IEnumerator ShowEndPanelAfterDelay(string verdict, int percent, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        ShowEndPanel(verdict, percent);
        deferFailUntilSolved = false; // clear for next round
    }

    void OnPlayerPressedSolve()
    {
        if (failOnSolve)
        {
            // Don’t show the panel yet; let the AI animate first.
            suspicion = 1f;
            UpdateHUD();
            deferFailUntilSolved = true;
        }
        else
        {
            suspicion = Mathf.Clamp01(suspicion + 0.5f);
            UpdateHUD();
        }
    }

    void OnPlayerHint()
    {
        suspicion = Mathf.Clamp01(suspicion + hintPenalty);
        UpdateHUD();

        // If you also want to wait until the puzzle is solved before showing failure:
        if (suspicion >= 1f)
            deferFailUntilSolved = true; // panel will appear after solve
    }

    int HumanPercent() =>
        Mathf.Clamp(Mathf.RoundToInt((1f - suspicion) * 100f), 0, 100);

    string VerdictForPercent(int p)
    {
        if (p >= 90) return "You are Definitely Human!";
        if (p >= 70) return "You are Probably Human?";
        if (p >= 50) return "You Might be Human?";
        if (p >= 30) return "You are Probably NOT Human";
        return "You are Definitely NOT Human!";
    }

    void ShowEndPanel(string verdict, int percent)
    {
        if (endPanel != null) endPanel.SetActive(true);
        if (endTitleText != null) endTitleText.text = verdict;
        if (endPercentText != null) endPercentText.text = $"You are {percent}% human";

        // audio stuff
        if (verdict.Contains("Human"))
            AudioManager.Instance?.PlayWin();

        if (endRestartButton != null)
        {
            endRestartButton.onClick.RemoveAllListeners();
            endRestartButton.onClick.AddListener(() =>
            {
                endPanel.SetActive(false);
                game.NewGame();     // or game.RestartGame() if you added one
                ResetSession();
            });
        }

        if (endQuitButton != null)
        {
            endQuitButton.onClick.RemoveAllListeners();
            endQuitButton.onClick.AddListener(() =>
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                                Application.Quit();
                #endif
            });
        }

        if (endCloseButton != null)
        {
            endCloseButton.onClick.RemoveAllListeners();
            endCloseButton.onClick.AddListener(() =>
            {
                endPanel.SetActive(false);
                ResetSession();
            });
        }
    }

    void ShowInfoPanel()
    {
        infoPanel.SetActive(true);

        if (infoCloseButton != null)
        {
            infoCloseButton.onClick.RemoveAllListeners();
            infoCloseButton.onClick.AddListener(() =>
            {
                infoPanel.SetActive(false);
            });
        }

        if (infoEndButton != null)
        {
            infoEndButton.onClick.RemoveAllListeners();
            infoEndButton.onClick.AddListener(() =>
            {
                infoPanel.SetActive(false);
            });
        }
    }
}
