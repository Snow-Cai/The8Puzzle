using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VerificationManager : MonoBehaviour
{
    [Header("Refs")]
    public GameManager game;
    public TMP_Text movesText;
    public Slider suspicionBar;
    public TMP_Text suspicionText;

    [Header("Rules")]
    [Range(0f, 1f)] public float hintPenalty = 0.1f;
    public bool failOnSolve = true;                   // pressing Solve is “suspicious”

    // state
    private int moves = 0;
    private float suspicion = 0f;

    [Header("End UI")]
    public GameObject endPanel;
    public TMP_Text endTitleText;
    public TMP_Text endPercentText;
    public Button endRestartButton;
    public Button endQuitButton;

    void Start()
    {
        endPanel.SetActive(false);

        // subscribe to simple events (we'll add them in GameManager next)
        game.onPlayerMove += OnPlayerMove;
        game.onPlayerSolved += OnPlayerSolved;
        game.onPlayerPressedSolve += OnPlayerPressedSolve;
        game.onPlayerHint += OnPlayerHint;

        ResetHUD();
    }

    public void ResetHUD()
    {
        moves = 0;
        suspicion = 0f;
        UpdateHUD();
        if (endPanel != null) endPanel.SetActive(false);
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
        // simple pass/fail messaging (no timer)
        if (suspicion >= 1f)
            Debug.Log("Suspicious: solved but over suspicion threshold.");
        else
            Debug.Log("Verified Human: solved with acceptable suspicion.");
    }

    void OnPlayerPressedSolve()
    {
        if (failOnSolve)
        {
            suspicion = 1f;   // max it
            UpdateHUD();
            Debug.Log("🤖 Suspicious: Solve was used.");
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
        // (the actual hint action will be triggered from GameManager; this only updates the meter)
    }

    int HumanPercent(){
        return Mathf.Clamp(Mathf.RoundToInt((1f - suspicion) * 100f), 0, 100);
    }

    string VerdictForPercent(int p)
    {
        if (p >= 90) return "You are Definitely Human!";
        if (p >= 70) return "You are Probably Human.";
        if (p >= 50) return "You might be Human.";
        if (p >= 30) return "You are Probably NOT Human.";
        return "You are Definitely NOT Human!";
    }

    public void ShowEndPanel(string title, string details, int percent)
    {
        if (endPanel != null) endPanel.SetActive(true);
        if (endTitleText != null) endTitleText.text = title;
        if (endPercentText != null) endPercentText.text = $"You are {percent}% human";

        if (endRestartButton != null)
        {
            endRestartButton.onClick.RemoveAllListeners();
            endRestartButton.onClick.AddListener(() =>
            {
                endPanel.SetActive(false);
                RestartGame();
                ResetHUD();
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
    }

    public void RestartGame()
    {
        if (endRestartButton != null)
        {

        }
    }
}