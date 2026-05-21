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
        // Decidim el resultat ara (50/50 pur)
        bool blueStarts = Random.value > 0.5f;

        // Calculem els graus totals per aturar-se al lloc correcte
        float[] blueAngles = { 60f, 150f, 240f, 330f };
        float[] redAngles = { 15f, 105f, 195f, 285f };
        float[] targetAngles = blueStarts ? blueAngles : redAngles;
        float extraDegrees = targetAngles[Random.Range(0, targetAngles.Length)];
        float totalDegrees = (Random.Range(minSpins, maxSpins) * 360f) + extraDegrees;

        float elapsed = 0f;
        float startRotation = wheelTransform.eulerAngles.z;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float easedT = EaseInOutCubic(elapsed / duration);
            wheelTransform.eulerAngles = new Vector3(0f, 0f, startRotation + totalDegrees * easedT);
            yield return null;
        }

        // Assegurem la rotació final exacta
        wheelTransform.eulerAngles = new Vector3(0f, 0f, startRotation + totalDegrees);

        // Mostrem el resultat
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