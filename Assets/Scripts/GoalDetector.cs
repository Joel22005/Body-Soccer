using UnityEngine;

/// <summary>
/// Detecta cuando la pelota entra en la portería.
/// Solo registra el gol si el partido está activo (gameStarted = true)
/// y no hay ya un gol en proceso (goalInProgress = false).
/// Esto evita falsos goles durante transiciones y reinicios.
/// </summary>
public class GoalDetector : MonoBehaviour
{
    [Tooltip("Equipo que ANOTA cuando la pelota entra aqui.\n" +
             "Porteria azul → RedTeam\n" +
             "Porteria roja → BlueTeam")]
    [SerializeField] private string scoringTeam = "BlueTeam";

    [Tooltip("Tag de la pelota (por defecto: 'Ball')")]
    [SerializeField] private string ballTag = "Ball";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ballTag)) return;
        if (GameManager.Instance == null) return;

        // Ignorar si el partido no ha empezado o ya hay un gol en proceso
        if (!GameManager.Instance.gameStarted) return;
        if (GameManager.Instance.GoalInProgress) return;

        Debug.Log($"[GoalDetector] Gol! Equipo anotador: {scoringTeam}");
        GameManager.Instance.GoalScored(scoringTeam);
    }
}