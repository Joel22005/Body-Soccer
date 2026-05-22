using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI & Score")]
    [SerializeField] private TMP_Text blueScoreText;
    [SerializeField] private TMP_Text redScoreText;
    [SerializeField] private GameObject goalPanel;
    [SerializeField] private Image blueGoalImage;
    [SerializeField] private Image redGoalImage;

    [Header("Game Elements")]
    [SerializeField] private GameObject ball;
    [SerializeField] private SpinWheel spinWheel; // ¡Ruleta restaurada!
    [SerializeField] private float resetDelay = 2.5f;
    [SerializeField] private AudioClip goalSound;
    private AudioSource audioSource;

    [Header("Turn Timer")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float turnDuration = 15f;

    [Header("Movement Detection")]
    [SerializeField] private float stillThreshold = 0.05f;
    [SerializeField] private float stillRequiredTime = 0.5f;


    [Header("Game Over Settings")]
    [SerializeField] private int maxGoals = 3; // Límite de goles para ganar



    // Variables de estado del juego
    private bool gameEnded = false;            // Controla si el partido ha terminado
    public string currentTurnTeam;
    public bool gameStarted = false; // Controla si la ruleta ya terminó
    public GameObject selectedBluePuck;
    public GameObject selectedRedPuck;

    private List<GameObject> bluePucks = new List<GameObject>();
    private List<GameObject> redPucks = new List<GameObject>();
    private int blueScore = 0;
    private int redScore = 0;
    private bool goalInProgress = false;

    // Variables de tiempo y movimiento
    private float turnTimeRemaining;
    private bool turnActive = false;
    private bool waitingForStill = false;
    private float stillTimer = 0f;

    private struct SavedTransform { public Vector3 pos; public Quaternion rot; }
    private Dictionary<Rigidbody, SavedTransform> initialTransforms = new Dictionary<Rigidbody, SavedTransform>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SaveInitialTransforms();
        InitializePuckLists();
        UpdateScoreUI();

        if (goalPanel != null) goalPanel.SetActive(false);
        audioSource = gameObject.AddComponent<AudioSource>();

        // Estado inicial de los turnos (todo apagado esperando a la ruleta)
        selectedBluePuck = null;
        selectedRedPuck = null;
        turnActive = false;
        waitingForStill = false; // Aún no esperamos movimiento, esperamos a la ruleta
        if (timerText != null) timerText.text = "...";

        // MERGE: Lanzamos la ruleta como en el código original
        spinWheel.OnSpinComplete += (blueStarts) =>
        {
            currentTurnTeam = blueStarts ? "BlueTeam" : "RedTeam";
            gameStarted = true;
            Debug.Log("Inici del partit! Comença: " + currentTurnTeam);
            UpdateVisualSelection();

            // MERGE: Una vez la ruleta acaba, AHORA activamos tu nueva lógica de quietud
            waitingForStill = true;
            stillTimer = 0f;
        };

        spinWheel.StartSpin();
    }

    private void InitializePuckLists()
    {
        bluePucks.AddRange(GameObject.FindGameObjectsWithTag("BlueTeam"));
        redPucks.AddRange(GameObject.FindGameObjectsWithTag("RedTeam"));

        // Nos aseguramos de que al empezar NO haya ninguna ficha seleccionada
        selectedBluePuck = null;
        selectedRedPuck = null;
        UpdateVisualSelection();
    }

    private void Update()
    {
        // MERGE: Si la ruleta está girando (gameStarted es false), no hacemos nada con el tiempo
        if (gameEnded || !gameStarted || goalInProgress) return;

        if (waitingForStill)
        {
            if (EverythingIsStill())
            {
                stillTimer += Time.deltaTime;
                if (stillTimer >= stillRequiredTime)
                {
                    waitingForStill = false;
                    StartTurn();
                }
            }
            else
            {
                stillTimer = 0f; // resetea si algo se mueve
            }
        }

        if (turnActive)
        {
            turnTimeRemaining -= Time.deltaTime;

            if (timerText != null)
                timerText.text = Mathf.CeilToInt(turnTimeRemaining).ToString();

            if (turnTimeRemaining <= 0f)
            {
                // Tiempo agotado → cambiar turno automáticamente
                Debug.Log("[GameManager] Finished Time. Turn change");
                turnActive = false;
                SwitchTurn();
            }
        }
    }

    public void OnPlayerCrouch(int playerID, Vector3 playerPosition)
    {
        // MERGE: Combinamos la protección de la ruleta y la de tu temporizador
        if (!gameStarted || goalInProgress) return;
        if (!turnActive) return; // bloquea si hay movimiento

        string playerTeam = (playerID == 1) ? "RedTeam" : "BlueTeam";
        if (playerTeam != currentTurnTeam) return;

        List<GameObject> myPucks = (playerID == 1) ? redPucks : bluePucks;
        if (myPucks.Count == 0) return;

        // Seleccionar la ficha más cercana a la posicion del jugador
        GameObject closest = null;
        float minDist = float.MaxValue;
        foreach (GameObject puck in myPucks)
        {
            float dist = Vector3.Distance(playerPosition, puck.transform.position);
            if (dist < minDist) { minDist = dist; closest = puck; }
        }

        if (playerID == 1) selectedRedPuck = closest;
        else selectedBluePuck = closest;

        UpdateVisualSelection();
    }

    private void UpdateVisualSelection()
    {
        foreach (var p in bluePucks)
        {
            LineRenderer lr = p.GetComponent<LineRenderer>();
            if (lr != null) lr.enabled = (p == selectedBluePuck && currentTurnTeam == "BlueTeam");
        }
        foreach (var p in redPucks)
        {
            LineRenderer lr = p.GetComponent<LineRenderer>();
            if (lr != null) lr.enabled = (p == selectedRedPuck && currentTurnTeam == "RedTeam");
        }
    }

    public void GoalScored(string scoringTeam)
    {
        if (goalInProgress || gameEnded) return;

        goalInProgress = true;
        turnActive = false; // Pausamos el tiempo inmediatamente

        if (scoringTeam == "BlueTeam")
        {
            blueScore++;
            currentTurnTeam = "RedTeam";
        }
        else
        {
            redScore++;
            currentTurnTeam = "BlueTeam";
        }

        UpdateScoreUI();

        if (goalSound) audioSource.PlayOneShot(goalSound);

        if (goalPanel)
        {
            goalPanel.SetActive(true);
            blueGoalImage?.gameObject.SetActive(scoringTeam == "BlueTeam");
            redGoalImage?.gameObject.SetActive(scoringTeam == "RedTeam");
        }

        // --- COMPROBACIÓN DE CONDICIÓN DE VICTORIA ---
        if (blueScore >= maxGoals || redScore >= maxGoals)
        {
            EndGame(scoringTeam);
        }
        else
        {
            // Si nadie ha llegado a 3, el partido se reinicia normalmente
            StartCoroutine(ResetMatchAfterDelay());
        }
    }

    private void EndGame(string winningTeam)
    {
        gameEnded = true;
        turnActive = false;
        waitingForStill = false;
        gameStarted = false; // Bloquea futuros inputs de tracking

        // Traducimos el nombre interno al texto que verá el usuario
        string winnerName = (winningTeam == "BlueTeam") ? "EQUIPO AZUL" : "EQUIPO ROJO";
        Debug.Log($"¡Fin del partido! El ganador es: {winnerName}");

        // Mostramos el ganador en el texto del temporizador del marcador
        if (timerText != null)
        {
            timerText.text = "WIN!";
        }

        // Opcional: Aquí podrías activar un panel específico de "Game Over" si lo tienes,
        // o dejar el panel de GOAL en pantalla permanentemente.
    }

    private void SaveInitialTransforms()
    {
        if (ball) SaveRB(ball.GetComponent<Rigidbody>());
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("BlueTeam")) SaveRB(g.GetComponent<Rigidbody>());
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("RedTeam")) SaveRB(g.GetComponent<Rigidbody>());
    }

    private void SaveRB(Rigidbody rb)
    {
        if (rb) initialTransforms[rb] = new SavedTransform { pos = rb.position, rot = rb.rotation };
    }

    private IEnumerator ResetMatchAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);

        foreach (var item in initialTransforms)
        {
            Rigidbody rb = item.Key;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = item.Value.pos;
            rb.rotation = item.Value.rot;
        }

        if (goalPanel != null) goalPanel.SetActive(false);

        goalInProgress = false;

        // Deseleccionar y esperar quietud para sacar del centro
        selectedBluePuck = null;
        selectedRedPuck = null;
        UpdateVisualSelection();

        waitingForStill = true;
        stillTimer = 0f;
        turnActive = false;

        if (timerText != null) timerText.text = "...";
    }

    private void UpdateScoreUI()
    {
        if (blueScoreText) blueScoreText.text = blueScore.ToString();
        if (redScoreText) redScoreText.text = redScore.ToString();
    }

    public void SwitchTurn()
    {
        currentTurnTeam = (currentTurnTeam == "BlueTeam") ? "RedTeam" : "BlueTeam";
        Debug.Log("Turn changed for: " + currentTurnTeam);

        // Deseleccionar fichas (Funcionalidad nueva añadida)
        selectedBluePuck = null;
        selectedRedPuck = null;
        UpdateVisualSelection();

        // Esperar a que todo esté quieto antes de empezar el turno
        turnActive = false;
        waitingForStill = true;
        stillTimer = 0f;

        if (timerText != null) timerText.text = "...";
    }

    private void StartTurn()
    {
        turnActive = true;
        turnTimeRemaining = turnDuration;
        Debug.Log($"[GameManager] Turn for {currentTurnTeam}. time: {turnDuration}s");
    }

    private bool EverythingIsStill()
    {
        foreach (var pair in initialTransforms)
        {
            Rigidbody rb = pair.Key;
            if (rb == null) continue;

            // LA MAGIA: Si el objeto es el balón, lo ignoramos y pasamos al siguiente
            if (ball != null && rb.gameObject == ball) continue;

            // Evaluamos velocidad lineal y angular SOLO de las chapas
            if (rb.linearVelocity.magnitude > stillThreshold) return false;
            if (rb.angularVelocity.magnitude > stillThreshold) return false;
        }
        return true;
    }

    public bool IsTurnActive() => turnActive;
}