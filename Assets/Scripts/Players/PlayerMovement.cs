using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Settings")]
    public int playerID;

    [Header("Kick Power")]
    public float minKickForce = 15f;
    public float maxKickForce = 100f;
    public float maxKickDistance = 20f; // distancia = potencia maxima

    [Header("Tracking Data")]
    public Quaternion q;
    public bool manual;

    // ------------------------------------------------------------------
    // CHUTE: igual para tracking y teclado
    // Potencia y direccion basadas en distancia/posicion del jugador
    // ------------------------------------------------------------------
    private void Start()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = (playerID == 1) ? Color.red : Color.blue;
        }
    }
    public void KickWithDistance()
    {
        if (GameManager.Instance == null || !GameManager.Instance.gameStarted) return;
        string myTeam = (playerID == 1) ? "RedTeam" : "BlueTeam";
        if (GameManager.Instance == null || GameManager.Instance.currentTurnTeam != myTeam) return;

        GameObject targetPuck = (playerID == 1)
            ? GameManager.Instance.selectedRedPuck
            : GameManager.Instance.selectedBluePuck;

        if (targetPuck == null) return;

        Rigidbody rb = targetPuck.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 toPlayer = transform.position - targetPuck.transform.position;
        toPlayer.y = 0f;

        float dist = toPlayer.magnitude;
        float t = Mathf.Clamp01(dist / maxKickDistance);
        float force = 2 * Mathf.Lerp(minKickForce, maxKickForce, t);

        Vector3 shootDir = (dist > 0.01f) ? -toPlayer.normalized : transform.forward;

        rb.AddForce(shootDir * force, ForceMode.Impulse);
        Debug.Log($"[PlayerMovement] Jugador {playerID} chuta. Fuerza: {force:F1}");

        GameManager.Instance.SwitchTurn();
    }

    // ------------------------------------------------------------------
    // Metodos tracking
    // ------------------------------------------------------------------
    public void SetPosition(Vector3 pos) => transform.position = pos;
    public Vector3 GetPosition() => transform.position;
    public void SetRotation(Quaternion rot) => transform.rotation = rot;
    public Quaternion GetRotation() => transform.rotation;
}