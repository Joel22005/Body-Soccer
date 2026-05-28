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
    public GameObject statusPromptImage;
    public GameObject startGameImage;
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

        // Mueve las botas reales con el teclado para pruebas
        UpdateDebugKeyboard();
    }

    private void EnterStartMenu()
    {
        currentState = GameState.StartMenu;

        if (startMenuCanvas != null) startMenuCanvas.SetActive(true);
        if (statusPromptImage != null) statusPromptImage.SetActive(true);
        if (startGameImage != null) startGameImage.SetActive(true);

        SetGameplayActive(false);

        redZone?.ResetZone();
        blueZone?.ResetZone();

        if (GameManager.Instance != null) GameManager.Instance.FullReset();

        StartCoroutine(FadeIn());
    }

    private void UpdateStartMenu()
    {
        // AHORA LEEMOS LA POSICIÓN REAL DE LAS BOTAS EN EL MUNDO 3D
        Vector3 p1Pos = players.Count > 0 && players[0] != null ? players[0].transform.position : Vector3.zero;
        Vector3 p2Pos = players.Count > 1 && players[1] != null ? players[1].transform.position : Vector3.zero;

        bool p1InRed = IsInside(p1Pos, redZoneCenter, redZone.zoneRadius);
        bool p2InBlue = IsInside(p2Pos, blueZoneCenter, blueZone.zoneRadius);

        redZone.SetPlayerInside(p1InRed);
        blueZone.SetPlayerInside(p2InBlue);

        if (redZone.isReady && blueZone.isReady)
        {
            if (statusPromptImage != null) statusPromptImage.SetActive(false);
            if (startGameImage != null) startGameImage.SetActive(false);
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
        if (ball != null)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = !active;
            ball.SetActive(active);
        }

        if (gameplayRoot != null)
        {
            gameplayRoot.SetActive(active);
        }

        // ¡MAGIA AQUÍ! 
        // Hemos eliminado el código que apagaba a los jugadores. 
        // Las botas ahora siempre estarán visibles y funcionarán en el menú de inicio.

        if (GameManager.Instance != null) GameManager.Instance.gameStarted = active;
    }

    private bool IsInside(Vector3 playerPos, Vector3 center, float radius)
    {
        // Comprobamos si la bota real ha entrado en el círculo virtual
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
        // Lo dejamos vacío para que el TrackingManager no dé error,
        // pero ya no lo usamos porque leemos directamente la bota física.
    }

    // AHORA ESTO MUEVE LAS BOTAS 3D REALES EN LUGAR DE UN PUNTO INVISIBLE
    private void UpdateDebugKeyboard()
    {
        if (currentState != GameState.StartMenu) return;
        float speed = 15f * Time.deltaTime; // Velocidad ajustada para las botas

        if (players.Count >= 2)
        {
            if (players[0] != null) // Jugador 1 (Rojo)
            {
                Vector3 move1 = Vector3.zero;
                if (Input.GetKey(KeyCode.LeftArrow)) move1.x -= speed;
                if (Input.GetKey(KeyCode.RightArrow)) move1.x += speed;
                if (Input.GetKey(KeyCode.UpArrow)) move1.z += speed;
                if (Input.GetKey(KeyCode.DownArrow)) move1.z -= speed;
                players[0].transform.Translate(move1, Space.World);
            }

            if (players[1] != null) // Jugador 2 (Azul)
            {
                Vector3 move2 = Vector3.zero;
                if (Input.GetKey(KeyCode.A)) move2.x -= speed;
                if (Input.GetKey(KeyCode.D)) move2.x += speed;
                if (Input.GetKey(KeyCode.W)) move2.z += speed;
                if (Input.GetKey(KeyCode.S)) move2.z -= speed;
                players[1].transform.Translate(move2, Space.World);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Mostramos las zonas virtuales, pero ya NO dibujamos las bolitas amarillas/verdes
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(redZoneCenter, redZone != null ? redZone.zoneRadius : 1.5f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(blueZoneCenter, blueZone != null ? blueZone.zoneRadius : 1.5f);
    }
}