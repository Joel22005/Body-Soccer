using UnityEngine;

/// <summary>
/// Place this script on an invisible Trigger collider positioned INSIDE each goal mouth.
///
/// Setup per goal:
///   1. Create an empty child GameObject inside the Goal object (e.g. "GoalTrigger_Blue").
///   2. Add a BoxCollider, enable "Is Trigger".
///   3. Size/position the collider to cover the goal opening.
///   4. Attach this script and set scoringTeam accordingly:
///        - Goal of BLUE team  → scoringTeam = "RedTeam"  (red scores when ball enters blue's goal)
///        - Goal of RED team   → scoringTeam = "BlueTeam"
/// </summary>
public class GoalDetector : MonoBehaviour
{
    [Tooltip("Which team SCORES when the ball enters this goal.\n" +
             "Blue goal  → RedTeam\n" +
             "Red goal   → BlueTeam")]
    [SerializeField] private string scoringTeam = "BlueTeam";

    [Tooltip("Tag on the ball object (default: 'Ball')")]
    [SerializeField] private string ballTag = "Ball";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ballTag)) return;

        Debug.Log($"[GoalDetector] Ball entered goal. Scoring team: {scoringTeam}");

        if (GameManager.Instance != null)
            GameManager.Instance.GoalScored(scoringTeam);
        else
            Debug.LogWarning("[GoalDetector] GameManager instance not found in the scene!");
    }
}