using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Settings")]
    public int playerID;
    public float kickForce = 15f;
    [Header("Tracking Data")]
    public Quaternion q;
    public bool manual;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PerformKick();
        }
    }

    public void PerformKick()
    {
        string myTeam = (playerID == 1) ? "RedTeam" : "BlueTeam";
        if (GameManager.Instance.currentTurnTeam != myTeam)
        {
            Debug.Log($"Jugador {playerID} ha intentat xutar però no és el seu torn!");
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
                Vector3 shootDirection = transform.forward;
                shootDirection.y = 0;
                rb.AddForce(shootDirection.normalized * kickForce, ForceMode.Impulse);
                Debug.Log($"Jugador {playerID} ha xutat la fitxa {targetPuck.name}");
                GameManager.Instance.SwitchTurn();
            }
        }
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public void SetRotation(Quaternion rot)
    {
        transform.rotation = rot;
    }

    public Quaternion GetRotation()
    {
        return transform.rotation;
    }
}