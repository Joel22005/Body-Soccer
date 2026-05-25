using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animacion de gol por frames.
/// 
/// SETUP:
///   1. Añade este script al GoalPanel
///   2. En el Inspector asigna los frames en orden:
///        Red Frames  → las 5 imagenes del Red Team (de menor a mayor)
///        Blue Frames → las 5 imagenes del Blue Team (de menor a mayor)
///   3. Asigna el componente Image del GoalPanel al campo "goalImage"
///   4. En GameManager llama a goalAnimator.Show("RedTeam") o Show("BlueTeam")
/// </summary>
public class GoalAnimator : MonoBehaviour
{
    [Header("Imagen que muestra los frames")]
    [SerializeField] private Image goalImage;

    [Header("Frames Red Team (orden: pequeño → grande)")]
    [SerializeField] private Sprite[] redFrames;

    [Header("Frames Blue Team (orden: pequeño → grande)")]
    [SerializeField] private Sprite[] blueFrames;

    [Header("Velocidad de entrada (frames rapidos)")]
    [SerializeField] private float entryFrameRate = 0.2f;  // segundos entre frames de entrada
    [SerializeField] private float holdDuration = 2.0f;   // tiempo que se queda en el ultimo frame
    [SerializeField] private float exitDuration = 0.4f;   // duracion del fade de salida

    [Header("Escala de entrada")]
    [SerializeField] private float startScale = 0.3f;   // escala inicial
    [SerializeField] private float overshoot = 1.15f;  // escala maxima antes de asentarse

    private CanvasGroup canvasGroup;
    private Coroutine currentAnim;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;           // invisible pero activo
        canvasGroup.blocksRaycasts = false;
        transform.localScale = Vector3.zero;
        // NO hacer SetActive(false) aqui
    }

    // ------------------------------------------------------------------
    // Llamar desde GameManager: Show("RedTeam") o Show("BlueTeam")
    // ------------------------------------------------------------------
    public void Show(string team)
    {
        Debug.Log("[GoalAnimator] Show llamado para: " + team);

        if (currentAnim != null) StopCoroutine(currentAnim);

        Sprite[] frames = (team == "RedTeam") ? redFrames : blueFrames;

        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("[GoalAnimator] No hay frames asignados para: " + team);
            return;
        }   

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        transform.localScale = Vector3.one * startScale;

        currentAnim = StartCoroutine(AnimateGoal(frames)); // ahora sí
    }

    // ------------------------------------------------------------------
    // Llamar desde GameManager para ocultar
    // ------------------------------------------------------------------
    public void Hide()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(AnimateHide());
    }

    // ------------------------------------------------------------------
    // Animacion principal:
    //   1. Flash rapido de frames (explosion de entrada)
    //   2. Zoom bounce en el ultimo frame
    //   3. Espera holdDuration
    //   4. Fade out
    // ------------------------------------------------------------------
    private IEnumerator AnimateGoal(Sprite[] frames)
    {
        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one * startScale;

        // --- FASE 1: ciclar frames rapido (efecto explosion) ---
        // Los primeros frames van muy rapido, los ultimos un poco mas lentos
        for (int i = 0; i < frames.Length - 1; i++)
        {
            goalImage.sprite = frames[i];
            Debug.Log("[GoalAnimator] Frame: " + i); // <-- añade esto
            // Escala crece progresivamente con cada frame
            float t = (float)i / (frames.Length - 1);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, overshoot, t);

            // float wait = entryFrameRate * (1f + i * 0.3f); // cada frame tarda un poco mas
            yield return new WaitForSeconds(entryFrameRate);
        }

        // --- FASE 2: ultimo frame con bounce ---
        goalImage.sprite = frames[frames.Length - 1];

        float bounceTime = 0.25f;
        float elapsed = 0f;
        while (elapsed < bounceTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / bounceTime;
            // Overshoot → settle a 1
            float scale = progress < 0.6f
                ? Mathf.Lerp(overshoot, overshoot * 1.1f, progress / 0.6f)
                : Mathf.Lerp(overshoot * 1.1f, 1f, (progress - 0.6f) / 0.4f);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        transform.localScale = Vector3.one;

        // --- FASE 3: pulso (late dos veces mientras espera) ---
        yield return StartCoroutine(Pulse());
        yield return new WaitForSeconds(holdDuration * 0.4f);
        yield return StartCoroutine(Pulse());
        yield return new WaitForSeconds(holdDuration * 0.4f);

        // --- FASE 4: fade out ---
        yield return StartCoroutine(AnimateHide());
    }

    // Pulso: se agranda un poco y vuelve
    private IEnumerator Pulse()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin(elapsed / duration * Mathf.PI); // sube y baja suave
            transform.localScale = Vector3.one * (1f + t * 0.06f);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private IEnumerator AnimateHide()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < exitDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / exitDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t * t);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.8f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        transform.localScale = Vector3.zero;
        currentAnim = null;
    }
}