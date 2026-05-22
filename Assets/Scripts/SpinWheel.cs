using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpinWheel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject spinPanel;
    [SerializeField] private RectTransform wheelTransform;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Config")]
    [SerializeField] private float minSpins = 3f;
    [SerializeField] private float maxSpins = 6f;
    [SerializeField] private float duration = 3f;

    public System.Action<bool> OnSpinComplete;

    public void StartSpin()
    {
        spinPanel.SetActive(true);
        resultText.gameObject.SetActive(false);
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        bool blueStarts = Random.value > 0.5f;

        // Sempre reiniciem a 0
        wheelTransform.eulerAngles = Vector3.zero;

        float totalSpins = Random.Range(minSpins, maxSpins);
        // Truncem a enter per assegurar voltes completes
        totalSpins = Mathf.Floor(totalSpins);

        // Angle FIX per a cada color — sempre el mateix punt
        float extraDegrees = blueStarts ? 0f : 45f;

        float totalDegrees = (totalSpins * 360f) + extraDegrees;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float easedT = EaseInOutCubic(elapsed / duration);
            wheelTransform.eulerAngles = new Vector3(0f, 0f, totalDegrees * easedT);
            yield return null;
        }

        // Forcem la rotació final EXACTA
        wheelTransform.eulerAngles = new Vector3(0f, 0f, extraDegrees);

        resultText.gameObject.SetActive(true);
        resultText.text = blueStarts ? "Blue Starts" : "Red Starts";
        resultText.color = blueStarts ? Color.blue : Color.red;

        yield return new WaitForSeconds(2.5f);
        spinPanel.SetActive(false);
        OnSpinComplete?.Invoke(blueStarts);
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}