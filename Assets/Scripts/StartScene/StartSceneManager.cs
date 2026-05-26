using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartSceneManager : MonoBehaviour
{
    public static StartSceneManager Instance { get; private set; }

    [Header("Team Zones")]
    public TeamZone redZone;
    public TeamZone blueZone;

    [Header("Zone Centers")]
    public Vector3 redZoneCenter;
    public Vector3 blueZoneCenter;

    [Header("Scene to load")]
    public string gameSceneName = "SampleScene";

    [Header("Transition")]
    public float transitionDelay = 0.8f;
    public Image fadePanel;            // panel negro para fade in/out

    [Header("Status Text ")]
    public TMP_Text statusText;

    // Posiciones actualizadas por el TrackingManager
    [HideInInspector] public Vector3 player1Position;
    [HideInInspector] public Vector3 player2Position;

    private bool loading = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Empieza con fade desde negro
        if (fadePanel != null)
            StartCoroutine(FadeIn());
    }

    private void Update()
    {
        // Controles de teclado activos siempre, tanto en Editor como en Build
        UpdateDebugKeyboard();

        if (loading) return;

        bool p1InRed = IsInside(player1Position, redZoneCenter, redZone.zoneRadius);
        bool p2InBlue = IsInside(player2Position, blueZoneCenter, blueZone.zoneRadius);

        redZone.SetPlayerInside(p1InRed);
        blueZone.SetPlayerInside(p2InBlue);

        // Feedback de texto
        if (statusText != null)
        {
            if (!p1InRed && !p2InBlue)
                statusText.text = "Hover over the team you want";
            else if (p1InRed && !p2InBlue)
                statusText.text = "Player 2 go to BLUE team";
            else if (!p1InRed && p2InBlue)
                statusText.text = "Player 1 go to RED team";
            else
                statusText.text = "Hold still";
        }

        // Ambos listos → cargar juego
        if (redZone.isReady && blueZone.isReady)
        {
            loading = true;
            StartCoroutine(LoadGame());
        }
    }

    // --- MÉTODOS ---

    // Este método ahora se ejecutará siempre (sin la restricción #if UNITY_EDITOR)
    private void UpdateDebugKeyboard()
    {
        float speed = 2f * Time.deltaTime;

        // Jugador 1 — flechas del teclado
        if (Input.GetKey(KeyCode.LeftArrow)) player1Position.x -= speed;
        if (Input.GetKey(KeyCode.RightArrow)) player1Position.x += speed;
        if (Input.GetKey(KeyCode.UpArrow)) player1Position.z += speed;
        if (Input.GetKey(KeyCode.DownArrow)) player1Position.z -= speed;

        // Jugador 2 — WASD
        if (Input.GetKey(KeyCode.A)) player2Position.x -= speed;
        if (Input.GetKey(KeyCode.D)) player2Position.x += speed;
        if (Input.GetKey(KeyCode.W)) player2Position.z += speed;
        if (Input.GetKey(KeyCode.S)) player2Position.z -= speed;
    }

    // Ver zonas visualmente en Editor (Gizmos)
    private void OnDrawGizmos()
    {
        // Zona roja
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(redZoneCenter, redZone != null ? redZone.zoneRadius : 1.5f);

        // Zona azul  
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(blueZoneCenter, blueZone != null ? blueZone.zoneRadius : 1.5f);

        // Posición jugador 1 (amarillo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(player1Position, 0.2f);

        // Posición jugador 2 (verde)
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(player2Position, 0.2f);
    }

    private bool IsInside(Vector3 playerPos, Vector3 center, float radius)
    {
        Vector2 p = new Vector2(playerPos.x, playerPos.z);
        Vector2 c = new Vector2(center.x, center.z);
        return Vector2.Distance(p, c) <= radius;
    }

    private IEnumerator LoadGame()
    {
        if (statusText != null)
            statusText.text = "GET READY";

        yield return new WaitForSeconds(transitionDelay);

        // Fade a negro antes de cargar
        if (fadePanel != null)
            yield return StartCoroutine(FadeOut());

        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator FadeIn()
    {
        float t = 1f;
        Color c = fadePanel.color;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            c.a = t;
            fadePanel.color = c;
            yield return null;
        }
        fadePanel.gameObject.SetActive(false);
    }

    private IEnumerator FadeOut()
    {
        fadePanel.gameObject.SetActive(true);
        float t = 0f;
        Color c = fadePanel.color;
        while (t < 1f)
        {
            t += Time.deltaTime;
            c.a = t;
            fadePanel.color = c;
            yield return null;
        }
    }

    // Llamado por el TrackingManager
    public void UpdatePlayerPosition(int playerID, Vector3 pos)
    {
        if (playerID == 1) player1Position = pos;
        else if (playerID == 2) player2Position = pos;
    }
}