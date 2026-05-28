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
    [SerializeField] private GoalAnimator goalAnimator;

    [Header("Game Elements")]
    [SerializeField] private GameObject ball;
    [SerializeField] private SpinWheel spinWheel;
    [SerializeField] private float resetDelay = 2.5f;

    [Header("Turn Timer")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float turnDuration = 15f;

    [Header("Movement Detection")]
    [SerializeField] private float stillThreshold = 0.05f;
    [SerializeField] private float stillRequiredTime = 0.5f;

    [Header("Game Over Settings")]
    [SerializeField] private int maxGoals = 3;

    // Variables de estado del juego
    private bool gameEnded = false;
    public string currentTurnTeam;
    public bool gameStarted = false;
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

        goalAnimator?.Hide();

        selectedBluePuck = null;
        selectedRedPuck = null;
        turnActive = false;
        waitingForStill = false;
        if (timerText != null) timerText.text = "...";

        // Dejamos configurado qué pasa cuando la ruleta termine
        if (spinWheel != null)
        {
            spinWheel.OnSpinComplete += (blueStarts) =>
            {
                currentTurnTeam = blueStarts ? "BlueTeam" : "RedTeam";
                gameStarted = true;
                Debug.Log("Inici del partit! Comença: " + currentTurnTeam);
                UpdateVisualSelection();

                waitingForStill = true;
                stillTimer = 0f;
            };
        }
        // ATENCIÓN: Ya no hacemos spinWheel.StartSpin() aquí. Lo hará el MatchFlowManager al llamar a StartMatch()
    }

    private void InitializePuckLists()
    {
        bluePucks.AddRange(GameObject.FindGameObjectsWithTag("BlueTeam"));
        redPucks.AddRange(GameObject.FindGameObjectsWithTag("RedTeam"));
        Debug.Log($"[InitializePuckLists] RedPucks:{redPucks.Count} BluePucks:{bluePucks.Count}");

        selectedBluePuck = null;
        selectedRedPuck = null;
        UpdateVisualSelection();
    }

    private void Update()
    {
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
                stillTimer = 0f;
            }
        }

        if (turnActive)
        {
            turnTimeRemaining -= Time.deltaTime;

            if (timerText != null)
                timerText.text = Mathf.CeilToInt(turnTimeRemaining).ToString();

            if (turnTimeRemaining <= 0f)
            {
                Debug.Log("[GameManager] Finished Time. Turn change");
                turnActive = false;
                SwitchTurn();
            }
        }
    }

    public void OnPlayerCrouch(int playerID, Vector3 playerPosition)
    {
        Debug.Log($"[OnPlayerCrouch] gameStarted:{gameStarted} goalInProgress:{goalInProgress} turnActive:{turnActive} currentTurn:{currentTurnTeam}");

        if (!gameStarted || goalInProgress) return;
        if (!turnActive) return;

        string playerTeam = (playerID == 1) ? "RedTeam" : "BlueTeam";
        Debug.Log($"[OnPlayerCrouch] playerID:{playerID} playerTeam:{playerTeam}");

        if (playerTeam != currentTurnTeam) return;

        List<GameObject> myPucks = (playerID == 1) ? redPucks : bluePucks;
        Debug.Log($"[OnPlayerCrouch] myPucks count:{myPucks.Count}");

        if (myPucks.Count == 0) return;

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
        turnActive = false;

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

        SoundManager.Instance?.PlayGoal();

        if (goalAnimator)
        {
            goalAnimator?.Show(scoringTeam);
        }

        if (blueScore >= maxGoals || redScore >= maxGoals)
        {
            EndGame(scoringTeam);
        }
        else
        {
            StartCoroutine(ResetMatchAfterDelay());
        }
    }

    private void EndGame(string winningTeam)
    {
        gameEnded = true;
        turnActive = false;
        waitingForStill = false;
        gameStarted = false;

        string winnerName = (winningTeam == "BlueTeam") ? "EQUIPO AZUL" : "EQUIPO ROJO";
        Debug.Log($"¡Fin del partido! El ganador es: {winnerName}");

        if (timerText != null)
        {
            timerText.text = "WIN!";
        }

        // --- CONEXIÓN CON MATCHFLOWMANAGER ---
        if (MatchFlowManager.Instance != null)
        {
            MatchFlowManager.Instance.EnterGameOver();
        }
    }

    // --- NUEVOS MÉTODOS AÑADIDOS DE LA GUÍA ---

    public void StartMatch()
    {
        blueScore = 0;
        redScore = 0;
        goalInProgress = false;
        gameEnded = false;

        bluePucks.Clear();
        redPucks.Clear();
        InitializePuckLists();

        UpdateScoreUI();
        selectedBluePuck = null;
        selectedRedPuck = null;
        turnActive = false;

        if (timerText != null) timerText.text = "...";

        // Lanzamos tu ruleta
        if (spinWheel != null)
        {
            spinWheel.StartSpin();
            Debug.Log("[GameManager] Partido iniciado. Girando ruleta...");
        }
        else
        {
            // Por si acaso la ruleta se desconecta
            currentTurnTeam = (Random.value > 0.5f) ? "BlueTeam" : "RedTeam";
            gameStarted = true;
            waitingForStill = true;
            stillTimer = 0f;
            Debug.Log("[GameManager] Partido iniciado sin ruleta.");
        }
    }

    public void FullReset()
    {
        blueScore = 0;
        redScore = 0;
        goalInProgress = false;
        gameStarted = false;
        gameEnded = false;
        turnActive = false;
        waitingForStill = false;
        selectedBluePuck = null;
        selectedRedPuck = null;

        foreach (var item in initialTransforms)
        {
            Rigidbody rb = item.Key;
            if (rb == null) continue;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = item.Value.pos;
            rb.rotation = item.Value.rot;
        }

        UpdateScoreUI();
        UpdateVisualSelection();

        if (timerText != null) timerText.text = "";
        goalAnimator?.Hide();

        Debug.Log("[GameManager] Reset completo.");
    }

    // ------------------------------------------

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

        goalAnimator?.Hide();
        goalInProgress = false;

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

        selectedBluePuck = null;
        selectedRedPuck = null;
        UpdateVisualSelection();

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

            if (ball != null && rb.gameObject == ball) continue;

            if (rb.linearVelocity.magnitude > stillThreshold) return false;
            if (rb.angularVelocity.magnitude > stillThreshold) return false;
        }
        return true;
    }

    public bool IsTurnActive() => turnActive;
}