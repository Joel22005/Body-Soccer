using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Score UI (assign TMP Text objects from the Scoreboard)")]
    [SerializeField] private TMP_Text blueScoreText;
    [SerializeField] private TMP_Text redScoreText;

    [Header("Goal Feedback")]
    [SerializeField] private GameObject goalPanel;
    [SerializeField] private UnityEngine.UI.Image blueGoalImage;
    [SerializeField] private UnityEngine.UI.Image redGoalImage;
    [SerializeField] private float resetDelay = 2.5f;

    [Header("Goal Sound")]
    [SerializeField] private AudioClip goalSound;
    private AudioSource audioSource;

    [Header("Ball")]
    [SerializeField] private GameObject ball;

    private int blueScore = 0;
    private int redScore = 0;

    private struct SavedTransform
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    private Dictionary<Rigidbody, SavedTransform> initialTransforms = new Dictionary<Rigidbody, SavedTransform>();
    private bool goalInProgress = false;

    private void Awake()
    {
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

        if (goalPanel != null)
            goalPanel.SetActive(false);

        // Crear AudioSource automáticamente
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void GoalScored(string scoringTeam)
    {
        if (goalInProgress) return;
        goalInProgress = true;

        if (scoringTeam == "BlueTeam")
            blueScore++;
        else if (scoringTeam == "RedTeam")
            redScore++;

        UpdateScoreUI();
        Debug.Log($"[GameManager] GOAL! {scoringTeam} scores. Blue {blueScore} - Red {redScore}");

        // Reproducir sonido de gol
        if (goalSound != null && audioSource != null)
            audioSource.PlayOneShot(goalSound);

        if (goalPanel != null)
        {
            if (blueGoalImage != null) blueGoalImage.gameObject.SetActive(false);
            if (redGoalImage != null) redGoalImage.gameObject.SetActive(false);

            if (scoringTeam == "BlueTeam" && blueGoalImage != null)
                blueGoalImage.gameObject.SetActive(true);
            else if (scoringTeam == "RedTeam" && redGoalImage != null)
                redGoalImage.gameObject.SetActive(true);

            goalPanel.SetActive(true);
        }

        StartCoroutine(ResetMatchAfterDelay());
    }

    private void SaveInitialTransforms()
    {
        initialTransforms.Clear();

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