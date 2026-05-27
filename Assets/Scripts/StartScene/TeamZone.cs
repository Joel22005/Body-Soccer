using UnityEngine;
using UnityEngine.UI;

public class TeamZone : MonoBehaviour
{
    [Header("Config")]
    public string teamName;
    public float zoneRadius = 1.5f;
    public float fillSpeed = 0.4f;

    [Header("UI References")]
    public Image loadingRing;
    public Image puckGlow;

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

        if (loadingRing != null)
            loadingRing.fillAmount = fillAmount;

        if (puckGlow != null)
        {
            Color c = puckGlow.color;
            c.a = fillAmount * 0.7f;
            puckGlow.color = c;
        }
    }

    public void SetPlayerInside(bool inside)
    {
        playerInside = inside;
    }

    public void ResetZone()
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