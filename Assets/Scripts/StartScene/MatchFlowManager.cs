using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchFlowManager : MonoBehaviour
{
    public static MatchFlowManager Instance { get; private set; }

    // ─── Estados del juego ───────────────────────────────────────────
    public enum GameState { StartMenu, Loading, Gameplay, GameOver }
    public GameState currentState = GameState.StartMenu;

    // ─── Referencias UI ──────────────────────────────────────────────
    [Header("Start Menu UI")]
    public GameObject startMenuCanvas;
    public TeamZone redZone;
    public TeamZone blueZone;
    public GameObject statusPromptImage; // 'hoverovertheteam'
    public GameObject startGameImage;    // <-- AÑADIDO: Tu imagen 'startgame'
    public Image fadePanel;

    [Header("Zone Centers (tracking virtual coords)")]
    public Vector3 redZoneCenter;
    public Vector3 blueZoneCenter;

    // ─── Referencias Gameplay ─────────────────────────────────────────
    [Header("Gameplay Objects to enable/disable")]
    public GameObject gameplayRoot;
    public GameObject ball;
    public List<GameObject> players;

    // ─── Config ──────────────────────────────────────────────────────
    [Header("Match Config")]
    public float transitionDuration = 1f;

    // ─── Posiciones jugadores (actualizadas por TrackingManager) ─────
    [HideInInspector] public Vector3 player1Position;
    [HideInInspector] public Vector3 player2Position;

    private bool transitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        EnterStartMenu();
    }

    private void Update()
    {
        if (transitioning) return;

        if (currentState == GameState.StartMenu)
            UpdateStartMenu();

        // Controles de teclado activos siempre para poder probar en Build sin tracking
        UpdateDebugKeyboard();
    }

    private void EnterStartMenu()
    {
        currentState = GameState.StartMenu;

        if (startMenuCanvas != null) startMenuCanvas.SetActive(true);
        if (statusPromptImage != null) statusPromptImage.SetActive(true);
        if (startGameImage != null) startGameImage.SetActive(true); // <-- AÑADIDO: Mostrar al inicio

        SetGameplayActive(false);

        redZone?.ResetZone();
        blueZone?.ResetZone();

        if (GameManager.Instance != null) GameManager.Instance.FullReset();

        StartCoroutine(FadeIn());
    }

    private void UpdateStartMenu()
    {
        bool p1InRed = IsInside(player1Position, redZoneCenter, redZone.zoneRadius);
        bool p2InBlue = IsInside(player2Position, blueZoneCenter, blueZone.zoneRadius);

        redZone.SetPlayerInside(p1InRed);
        blueZone.SetPlayerInside(p2InBlue);

        // Ocultar las imágenes si ambos están listos
        if (redZone.isReady && blueZone.isReady)
        {
            if (statusPromptImage != null) statusPromptImage.SetActive(false);
            if (startGameImage != null) startGameImage.SetActive(false); // <-- AÑADIDO: Ocultar al empezar
            StartCoroutine(TransitionToGameplay());
        }
    }

    public void EnterGameOver()
    {
        if (currentState == GameState.GameOver) return;
        currentState = GameState.GameOver;
        StartCoroutine(TransitionToStartMenu());
    }

    private IEnumerator TransitionToGameplay()
    {
        transitioning = true;
        currentState = GameState.Loading;

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FadeOut());

        if (startMenuCanvas != null) startMenuCanvas.SetActive(false);
        SetGameplayActive(true);

        yield return StartCoroutine(FadeIn());

        currentState = GameState.Gameplay;
        transitioning = false;

        if (GameManager.Instance != null) GameManager.Instance.StartMatch();
    }

    private IEnumerator TransitionToStartMenu()
    {
        transitioning = true;
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(FadeOut());
        EnterStartMenu();
        transitioning = false;
    }

    private void SetGameplayActive(bool active)
    {
        // Activar/desactivar física de la pelota
        if (ball != null)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = !active;
            ball.SetActive(active);
        }

        // Activar/desactivar todos los gráficos del campo de fútbol
        if (gameplayRoot != null)
        {
            gameplayRoot.SetActive(active);
        }

        // Activar/desactivar movimiento de los jugadores
        foreach (GameObject p in players)
        {
            if (p == null) continue;
            PlayerMovement pm = p.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = active;
        }

        // Avisar al GameManager
        if (GameManager.Instance != null) GameManager.Instance.gameStarted = active;
    }

    private bool IsInside(Vector3 playerPos, Vector3 center, float radius)
    {
        Vector2 p = new Vector2(playerPos.x, playerPos.z);
        Vector2 c = new Vector2(center.x, center.z);
        return Vector2.Distance(p, c) <= radius;
    }

    private IEnumerator FadeIn()
    {
        if (fadePanel == null) yield break;
        fadePanel.gameObject.SetActive(true);
        Color c = fadePanel.color;
        float t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / transitionDuration;
            c.a = Mathf.Clamp01(t);
            fadePanel.color = c;
            yield return null;
        }
        fadePanel.gameObject.SetActive(false);
    }

    private IEnumerator FadeOut()
    {
        if (fadePanel == null) yield break;
        fadePanel.gameObject.SetActive(true);
        Color c = fadePanel.color;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            c.a = Mathf.Clamp01(t);
            fadePanel.color = c;
            yield return null;
        }
    }

    public void UpdatePlayerPosition(int playerID, Vector3 pos)
    {
        if (playerID == 1) player1Position = pos;
        else if (playerID == 2) player2Position = pos;
    }

    // Se ejecuta siempre (para testear sin tracking con teclado)
    private void UpdateDebugKeyboard()
    {
        if (currentState != GameState.StartMenu) return;
        float speed = 2f * Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftArrow)) player1Position.x -= speed;
        if (Input.GetKey(KeyCode.RightArrow)) player1Position.x += speed;
        if (Input.GetKey(KeyCode.UpArrow)) player1Position.z += speed;
        if (Input.GetKey(KeyCode.DownArrow)) player1Position.z -= speed;

        if (Input.GetKey(KeyCode.A)) player2Position.x -= speed;
        if (Input.GetKey(KeyCode.D)) player2Position.x += speed;
        if (Input.GetKey(KeyCode.W)) player2Position.z += speed;
        if (Input.GetKey(KeyCode.S)) player2Position.z -= speed;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(redZoneCenter, redZone != null ? redZone.zoneRadius : 1.5f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(blueZoneCenter, blueZone != null ? blueZone.zoneRadius : 1.5f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(player1Position, 0.2f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(player2Position, 0.2f);
    }
}