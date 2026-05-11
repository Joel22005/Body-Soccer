using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Settings")]
    public int playerID;

    [Header("Kick Power")]
    public float minKickForce = 5f;
    public float maxKickForce = 25f;
    public float maxChargeTime = 2f;

    [Header("Tracking Data")]
    public Quaternion q;
    public bool manual;

    // Carga actual 0-1, leida por PuckVisualFeedback
    public float ChargePercent { get; private set; } = 0f;

    private bool isCharging = false;
    private float chargeTime = 0f;
    public float maxKickDistance = 3f;

    public void StartCharge()
    {
        string myTeam = (playerID == 1) ? "RedTeam" : "BlueTeam";
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.currentTurnTeam != myTeam) return;
        isCharging = true;
        chargeTime = 0f;
        ChargePercent = 0f;
    }

    public void UpdateCharge(float deltaTime)
    {
        if (!isCharging) return;
        chargeTime = Mathf.Min(chargeTime + deltaTime, maxChargeTime);
        ChargePercent = chargeTime / maxChargeTime;
    }

    public void ReleaseKick()
    {
        if (!isCharging) return;
        isCharging = false;

        string myTeam = (playerID == 1) ? "RedTeam" : "BlueTeam";
        if (GameManager.Instance == null || GameManager.Instance.currentTurnTeam != myTeam)
        {
            ChargePercent = 0f;
            return;
        }

        GameObject targetPuck = (playerID == 1)
            ? GameManager.Instance.selectedRedPuck
            : GameManager.Instance.selectedBluePuck;

        if (targetPuck != null)
        {
            Rigidbody rb = targetPuck.GetComponent<Rigidbody>();
            if (rb != null)
            {
                float force = Mathf.Lerp(minKickForce, maxKickForce, ChargePercent);
                Vector3 dir = transform.forward;
                dir.y = 0f;
                rb.AddForce(dir.normalized * force, ForceMode.Impulse);
                Debug.Log($"[PlayerMovement] Jugador {playerID} chuta con fuerza {force:F1}");
                GameManager.Instance.SwitchTurn();
            }
        }

        ChargePercent = 0f;
    }

    public void KickWithDistance()
    {
        string myTeam = (playerID == 1) ? "RedTeam" : "BlueTeam";
        if (GameManager.Instance == null || GameManager.Instance.currentTurnTeam != myTeam) return;

        GameObject targetPuck = (playerID == 1)
            ? GameManager.Instance.selectedRedPuck
            : GameManager.Instance.selectedBluePuck;

        if (targetPuck == null) return;

        Rigidbody rb = targetPuck.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Distancia entre jugador y ficha → potencia
        float dist = Vector3.Distance(transform.position, targetPuck.transform.position);
        float t = Mathf.Clamp01(dist / maxKickDistance); // maxKickDistance = distancia máxima = fuerza máxima
        float force = Mathf.Lerp(minKickForce, maxKickForce, t);

        Vector3 dir = transform.forward;
        dir.y = 0f;
        rb.AddForce(dir.normalized * force, ForceMode.Impulse);

        GameManager.Instance.SwitchTurn();
    }

    // Métodos tracking
    public void SetPosition(Vector3 pos) => transform.position = pos;
    public Vector3 GetPosition() => transform.position;
    public void SetRotation(Quaternion rot) => transform.rotation = rot;
    public Quaternion GetRotation() => transform.rotation;
}