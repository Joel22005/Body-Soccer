using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private TMP_Text blueScoreText;
    [SerializeField] private TMP_Text redScoreText;
    [SerializeField] private GameObject goalPanel;
    [SerializeField] private Image blueGoalImage;
    [SerializeField] private Image redGoalImage;
    [SerializeField] private float resetDelay = 2.5f;
    [SerializeField] private AudioClip goalSound;
    private AudioSource audioSource;
    [SerializeField] private GameObject ball;
    public string currentTurnTeam;
    public GameObject selectedBluePuck;
    public GameObject selectedRedPuck;
    private List<GameObject> bluePucks = new List<GameObject>();
    private List<GameObject> redPucks = new List<GameObject>();
    private int blueIndex = 0;
    private int redIndex = 0;
    private int blueScore = 0;
    private int redScore = 0;
    private bool goalInProgress = false;
    private struct SavedTransform { public Vector3 pos; public Quaternion rot; }
    private Dictionary<Rigidbody, SavedTransform> initialTransforms = new Dictionary<Rigidbody, SavedTransform>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        currentTurnTeam = (Random.value > 0.5f) ? "BlueTeam" : "RedTeam";
        Debug.Log("Inici del partit! Comença: " + currentTurnTeam);
        SaveInitialTransforms();
        InitializePuckLists();
        UpdateScoreUI();
        if (goalPanel != null)
        {
            goalPanel.SetActive(false);
        }
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void InitializePuckLists()
    {
        bluePucks.AddRange(GameObject.FindGameObjectsWithTag("BlueTeam"));
        redPucks.AddRange(GameObject.FindGameObjectsWithTag("RedTeam"));
        if (bluePucks.Count > 0)
        {
            selectedBluePuck = bluePucks[0];
        }   
        if (redPucks.Count > 0)
        {
            selectedRedPuck = redPucks[0];
        }
        UpdateVisualSelection();
    }

    public void OnPlayerCrouch(int playerID, Vector3 playerPosition)
    {
        if (goalInProgress) return;
        string playerTeam = (playerID == 1) ? "RedTeam" : "BlueTeam";
        if (playerTeam != currentTurnTeam) return;

        List<GameObject> myPucks = (playerID == 1) ? redPucks : bluePucks;
        if (myPucks.Count == 0) return;

        // Seleccionar la ficha más cercana a la posición del jugador
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
        if (goalInProgress)
        {
            return;
        }
        goalInProgress = true;
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
        if (goalSound)
        {
            audioSource.PlayOneShot(goalSound);
        }
        if (goalPanel)
        {
            goalPanel.SetActive(true);
            blueGoalImage?.gameObject.SetActive(scoringTeam == "BlueTeam");
            redGoalImage?.gameObject.SetActive(scoringTeam == "RedTeam");
        }
        StartCoroutine(ResetMatchAfterDelay());
    }

    private void SaveInitialTransforms()
    {
        if (ball)
        {
            SaveRB(ball.GetComponent<Rigidbody>());
        }
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("BlueTeam")) SaveRB(g.GetComponent<Rigidbody>());
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("RedTeam")) SaveRB(g.GetComponent<Rigidbody>());
    }

    private void SaveRB(Rigidbody rb)
    {
        if (rb)
        {
            initialTransforms[rb] = new SavedTransform { pos = rb.position, rot = rb.rotation };
        }
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
        if (goalPanel)
        {
            goalPanel.SetActive(false);
        }
        goalInProgress = false;
        UpdateVisualSelection();
    }

    private void UpdateScoreUI()
    {
        if (blueScoreText)
        {
            blueScoreText.text = blueScore.ToString();
        }
        if (redScoreText)
        {
            redScoreText.text = redScore.ToString();
        }
    }

    public void SwitchTurn()
    {
        currentTurnTeam = (currentTurnTeam == "BlueTeam") ? "RedTeam" : "BlueTeam";
        Debug.Log("Canvi de torn. Ara li toca a: " + currentTurnTeam);
        UpdateVisualSelection();
    }
}