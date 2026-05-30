using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animacion de victoria por frames.
///
/// SETUP:
///   1. Crea un VictoryPanel en la misma jerarquia que el GoalPanel
///   2. Añade este script al VictoryPanel
///   3. Asigna los 5 frames de cada equipo en orden (pequeño → grande)
///   4. Asigna el Image del panel al campo victoryImage
///   5. En GameManager arrastra el VictoryPanel al campo victoryAnimator
///
/// TIMING:
///   - Frame 0: explosion inicial  → frame0Duration (mas largo)
///   - Frame 1: tamaño maximo      → frame1Duration (el mas largo)
///   - Frames 2-4: asentamiento    → frameDuration  (rapido)
///   - Ultimo frame: bounce + pulsos + espera
///   - Fade out automatico → llama a OnAnimationComplete
/// </summary>
public class VictoryAnimator : MonoBehaviour
{
    [Header("Imagen que muestra los frames")]
    [SerializeField] private Image victoryImage;

    [Header("Frames Red Team (orden: pequeño → grande)")]
    [SerializeField] private Sprite[] redFrames;

    [Header("Frames Blue Team (orden: pequeño → grande)")]
    [SerializeField] private Sprite[] blueFrames;

    [Header("Duracion de cada frame")]
    [SerializeField] private float frame0Duration = 0.45f; // explosion inicial - mas largo
    [SerializeField] private float frame1Duration = 0.55f; // tamaño maximo - el mas largo
    [SerializeField] private float frameDuration = 0.12f; // frames 2 en adelante - rapido

    [Header("Espera en el ultimo frame")]
    [SerializeField] private float holdDuration = 4f;  // segundos visible antes de salir

    [Header("Escala")]
    [SerializeField] private float startScale = 0.2f;
    [SerializeField] private float overshoot = 1.2f;

    [Header("Salida")]
    [SerializeField] private float exitDuration = 0.5f;

    // GameManager se suscribe a esto para saber cuando ha terminado la animacion
    public event Action OnAnimationComplete;

    private CanvasGroup canvasGroup;
    private Coroutine currentAnim;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        transform.localScale = Vector3.zero;
    }

    // ---------------------------------------------------------------
    public void Show(string team)
    {
        Debug.Log("[VictoryAnimator] Show para: " + team);

        if (currentAnim != null) StopCoroutine(currentAnim);

        Sprite[] frames = (team == "RedTeam") ? redFrames : blueFrames;
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("[VictoryAnimator] No hay frames para: " + team);
            OnAnimationComplete?.Invoke();
            return;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        transform.localScale = Vector3.one * startScale;

        currentAnim = StartCoroutine(AnimateVictory(frames));
    }

    public void Hide()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(AnimateHide());
    }

    // ---------------------------------------------------------------
    private IEnumerator AnimateVictory(Sprite[] frames)
    {
        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one * startScale;

        // --- FASE 1: ciclar frames con duraciones especiales para los primeros ---
        for (int i = 0; i < frames.Length - 1; i++)
        {
            victoryImage.sprite = frames[i];

            // Escala crece con cada frame
            float t = (float)i / (frames.Length - 1);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, overshoot, t);

            // Duracion especial para los dos primeros frames
            float wait = i == 0 ? frame0Duration
                       : i == 1 ? frame1Duration
                       : frameDuration;

            yield return new WaitForSeconds(wait);
        }

        // --- FASE 2: ultimo frame con bounce explosivo ---
        victoryImage.sprite = frames[frames.Length - 1];

        float bounceTime = 0.3f;
        float elapsed = 0f;
        while (elapsed < bounceTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / bounceTime;
            float scale = progress < 0.6f
                ? Mathf.Lerp(overshoot, overshoot * 1.12f, progress / 0.6f)
                : Mathf.Lerp(overshoot * 1.12f, 1f, (progress - 0.6f) / 0.4f);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        transform.localScale = Vector3.one;

        // --- FASE 3: pulsos mientras espera ---
        float holdRemaining = holdDuration;
        float pulseInterval = holdDuration / 3f;

        while (holdRemaining > 0f)
        {
            yield return StartCoroutine(Pulse());
            holdRemaining -= pulseInterval;
            if (holdRemaining > 0f)
                yield return new WaitForSeconds(Mathf.Min(pulseInterval - 0.2f, holdRemaining));
            holdRemaining -= (pulseInterval - 0.2f);
        }

        // --- FASE 4: fade out ---
        yield return StartCoroutine(AnimateHide());

        // Avisa al GameManager que ha terminado
        OnAnimationComplete?.Invoke();
    }

    private IEnumerator Pulse()
    {
        float duration = 0.25f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin(elapsed / duration * Mathf.PI);
            transform.localScale = Vector3.one * (1f + t * 0.08f);
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
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, t);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        transform.localScale = Vector3.zero;
        currentAnim = null;
    }
}