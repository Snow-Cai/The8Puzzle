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

    void Start()
    {
        // subscribe to simple events (we'll add them in GameManager next)
        game.onPlayerMove += OnPlayerMove;
        game.onPlayerSolved += OnPlayerSolved;
        game.onPlayerPressedSolve += OnPlayerPressedSolve;
        game.onPlayerHint += OnPlayerHint;

        ResetSession();
    }

    public void ResetSession()
    {
        moves = 0;
        suspicion = 0f;
        UpdateHUD();
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
}
