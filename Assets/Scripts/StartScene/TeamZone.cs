using UnityEngine;
using UnityEngine.UI;

public class TeamZone : MonoBehaviour
{
    [Header("Config")]
    public string teamName;           // "RedTeam" o "BlueTeam"
    public float zoneRadius = 1.5f;   // radio detección en coords virtuales
    public float fillSpeed = 0.4f;    // velocidad llenado (1/fillSpeed = segundos)

    [Header("UI")]
    public Image loadingRing;         // arrastra el LoadingRing aquí

    [Header("Visual Feedback")]
    public Image puckGlow;            // imagen de glow detrás de la ficha (opcional)

    [HideInInspector] public bool isReady = false;

    private float fillAmount = 0f;
    private bool playerInside = false;

    private void Update()
    {
        if (playerInside)
            fillAmount += fillSpeed * Time.deltaTime;
        else
            fillAmount -= fillSpeed * Time.deltaTime;

        fillAmount = Mathf.Clamp01(fillAmount);
        isReady = fillAmount >= 1f;

        // Actualizar UI
        if (loadingRing != null)
            loadingRing.fillAmount = fillAmount;

        // Glow proporcional al llenado
        if (puckGlow != null)
        {
            Color c = puckGlow.color;
            c.a = fillAmount * 0.6f;
            puckGlow.color = c;
        }
    }

    public void SetPlayerInside(bool inside)
    {
        playerInside = inside;
    }

    public void Reset()
    {
        fillAmount = 0f;
        isReady = false;
        playerInside = false;
        if (loadingRing != null) loadingRing.fillAmount = 0f;
        if (puckGlow != null)
        {
            Color c = puckGlow.color;
            c.a = 0f;
            puckGlow.color = c;
        }
    }
}