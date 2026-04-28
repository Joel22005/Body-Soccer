using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages game state: score tracking, goal events, and match reset.
/// Place this script on an empty GameObject called "GameManager" in the scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Score UI (assign TMP Text objects from the Scoreboard)")]
    [SerializeField] private TMP_Text blueScoreText;
    [SerializeField] private TMP_Text redScoreText;

    [Header("Goal Feedback")]
    [SerializeField] private GameObject goalPanel;          // Panel que se activa al marcar gol
    [SerializeField] private UnityEngine.UI.Image blueGoalImage;  // Imagen que aparece cuando marca el equipo azul
    [SerializeField] private UnityEngine.UI.Image redGoalImage;   // Imagen que aparece cuando marca el equipo rojo
    [SerializeField] private float resetDelay = 2.5f;      // Segundos antes de resetear

    [Header("Ball")]
    [SerializeField] private GameObject ball;

    // Scores
    private int blueScore = 0;
    private int redScore = 0;

    // Stores every puck + the ball's original transform so we can restore them
    private struct SavedTransform
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    private Dictionary<Rigidbody, SavedTransform> initialTransforms = new Dictionary<Rigidbody, SavedTransform>();

    // Prevents multiple goals being registered during the reset delay
    private bool goalInProgress = false;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SaveInitialTransforms();
    }

    private void Start()
    {
        UpdateScoreUI();

        // Make sure the goal panel starts hidden
        if (goalPanel != null)
            goalPanel.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Public API called by GoalDetector
    // -------------------------------------------------------------------------

    /// <summary>
    /// Call this when the ball enters a goal.
    /// scoringTeam should be "BlueTeam" or "RedTeam".
    /// </summary>
    public void GoalScored(string scoringTeam)
    {
        if (goalInProgress) return;
        goalInProgress = true;

        // Update score
        if (scoringTeam == "BlueTeam")
            blueScore++;
        else if (scoringTeam == "RedTeam")
            redScore++;

        UpdateScoreUI();
        Debug.Log($"[GameManager] GOAL! {scoringTeam} scores. Blue {blueScore} - Red {redScore}");

        // Mostrar panel con la imagen del equipo que ha marcado
        if (goalPanel != null)
        {
            // Ocultar ambas imagenes primero
            if (blueGoalImage != null) blueGoalImage.gameObject.SetActive(false);
            if (redGoalImage != null) redGoalImage.gameObject.SetActive(false);

            // Activar solo la imagen del equipo que ha marcado
            if (scoringTeam == "BlueTeam" && blueGoalImage != null)
                blueGoalImage.gameObject.SetActive(true);
            else if (scoringTeam == "RedTeam" && redGoalImage != null)
                redGoalImage.gameObject.SetActive(true);

            goalPanel.SetActive(true);
        }

        StartCoroutine(ResetMatchAfterDelay());
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Saves the starting position/rotation of the ball and all pucks so we can
    /// restore them after each goal.
    /// </summary>
    private void SaveInitialTransforms()
    {
        initialTransforms.Clear();

        // Save ball
        if (ball != null)
        {
            var rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
                initialTransforms[rb] = new SavedTransform
                {
                    position = ball.transform.position,
                    rotation = ball.transform.rotation
                };
        }

        // Save all pucks (tagged BlueTeam or RedTeam)
        SaveTaggedObjects("BlueTeam");
        SaveTaggedObjects("RedTeam");
    }

    private void SaveTaggedObjects(string tag)
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(tag))
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
                initialTransforms[rb] = new SavedTransform
                {
                    position = go.transform.position,
                    rotation = go.transform.rotation
                };
        }
    }

    private IEnumerator ResetMatchAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);

        ResetAllPositions();

        if (goalPanel != null)
            goalPanel.SetActive(false);

        goalInProgress = false;
    }

    /// <summary>
    /// Teleports every saved Rigidbody back to its initial position and
    /// zeroes out any velocity so physics restarts cleanly.
    /// </summary>
    private void ResetAllPositions()
    {
        foreach (var pair in initialTransforms)
        {
            Rigidbody rb = pair.Key;
            if (rb == null) continue;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.transform.position = pair.Value.position;
            rb.transform.rotation = pair.Value.rotation;
        }

        Debug.Log("[GameManager] Match reset.");
    }

    private void UpdateScoreUI()
    {
        if (blueScoreText != null) blueScoreText.text = blueScore.ToString();
        if (redScoreText != null) redScoreText.text = redScore.ToString();
    }
}