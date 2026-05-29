using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class TrackingManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CalibrationUI calibrationUI;     // Handles UI interactions

    [Header("Interface")]
    [SerializeField] private GameObject trackingInterface;
    private bool isInterfaceActive = false;

    // Enable or disable tracking plugin.
    [Header("On / Off")]
    [SerializeField] private bool enableTracking;

    // Options for configuration
    [Header("Tracking Configuration")]
    [SerializeField] private int numberOfPlayers;
    [SerializeField] private int numberOfBaseStations;
    [SerializeField] private bool enableRotation;
    [SerializeField] private bool enableYAxis;
    [Tooltip("Provided virtual world space is the size of the plane or surface that is seen as for height, as mush as one meter in the real world should match to")]
    [SerializeField] private Vector3 virtualWorldSpace;

    //Players objects reference
    [Header("Player Objects")]
    [SerializeField] private List<GameObject> players;

    //Calibration path information
    [Header("Calibration File Path")]
    [Tooltip("Provided path must be absolute <C:/usr/...> . If no path provided, file will be saved at default location")]
    private string fullCalibrationSaveFilePath;
    [SerializeField] private string calibrationSaveFilePath;
    private string calibrationSaveFileName = "trackingCalibration";

    //Calibration information
    [Header("Calibration")]
    private Calibration calibration;
    private bool calibrated = false;

    private int playersPosAndRotDatatSize;

    // Attributes for non-tracking input
    [Header("Non-tracking")]
    [SerializeField] private int playerSelected = 0;
    [SerializeField] private int trackingDisabledPlayerSpeed = 5;
    [SerializeField] private float trackingDisabledRotSpeed = 90f;

    private int playerRotDatatSize = 7;
    private float positionUpdateInterval = 0.01f;
    private bool isTrackingInitialized;
    private bool[] playerWasCrouched;

    private float[] crouchStartTime;
    private bool[] puckSelected;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchThreshold = 0.5f;
    [Tooltip("Sube este valor en el Inspector si te cuesta deseleccionar la ficha (ej: 3 o 5)")]
    [SerializeField] private float safeZoneRadius = 0.5f; // radio del círculo seguro alrededor de la ficha

    private void Awake()
    {
        playerWasCrouched = new bool[8];
        crouchStartTime = new float[8];
        puckSelected = new bool[8];

        if (calibrationUI == null)
        {
            Debug.LogError("Missing one or more dependencies. Assign required scripts in the Inspector.");
            return;
        }

        trackingInterface.SetActive(isInterfaceActive);

        if (enableTracking)
        {
            PluginConnector.StartTracking(numberOfPlayers, numberOfBaseStations);
            isTrackingInitialized = true;

            int detectedBaseStations = PluginConnector.GetNumberOfBaseStations();
            if (detectedBaseStations == numberOfBaseStations)
                calibrationUI.SetNumberOfBaseStations(detectedBaseStations);
            else
                calibrationUI.SetNumberOfBaseStations("Discrepancy");

            int detectedPlayers = PluginConnector.GetNumberOfTrackers();
            if (detectedPlayers == numberOfPlayers)
            {
                calibrationUI.SetNumberOfPlayers(detectedPlayers);
                playersPosAndRotDatatSize = numberOfPlayers * playerRotDatatSize;
            }
            else
            {
                Debug.Log("No tracker detcted");
                calibrationUI.SetNumberOfPlayers("Discrepancy");
            }

            calibration.Initialize();
        }
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(calibrationSaveFilePath))
            calibrationSaveFilePath = Application.persistentDataPath;

        fullCalibrationSaveFilePath = calibrationSaveFilePath + "/" + calibrationSaveFileName + ".json";

        LoadCalibrationJson();

        for (int i = 0; i < players.Count; i++)
        {
            if (i >= numberOfPlayers) players[i].SetActive(false);
        }

        if (enableTracking) StartCoroutine(GetPositions());
    }

    public void LoadCalibrationJson()
    {
        Debug.Log("Fetching file at: " + fullCalibrationSaveFilePath);

        if (!File.Exists(fullCalibrationSaveFilePath))
        {
            Debug.LogWarning("[Tracking] No hay archivo de calibración. Esto es normal si no has calibrado las cámaras físicas.");
            if (calibrationUI != null) calibrationUI.SetCalibrationFileStatus("No File Found");
            return;
        }

        try
        {
            calibration = CalibrationUtils.LoadCalibrationJson(fullCalibrationSaveFilePath);
            UpdateCalibrationUICalibrationData();
            calibrated = true;
            if (calibrationUI != null) calibrationUI.SetCalibrationFileStatus("Loaded Calibration!");
        }
        catch (Exception e)
        {
            Debug.LogError("Error loading calibration: " + e.Message);
            if (calibrationUI != null) calibrationUI.SetCalibrationFileStatus("Calibration Failed!");
        }
    }

    private IEnumerator GetPositions()
    {
        for (; ; )
        {
            float[] openVrOutputArr = new float[playersPosAndRotDatatSize];
            PluginConnector.UpdatePositions(openVrOutputArr, false, true, false);

            for (int i = 0; i < numberOfPlayers; i++)
            {
                int playerIndex = i * playerRotDatatSize;

                if (openVrOutputArr.Length >= numberOfPlayers)
                {
                    Vector3 playersRawPosition = new Vector3(openVrOutputArr[0 + playerIndex], openVrOutputArr[1 + playerIndex], openVrOutputArr[2 + playerIndex]);
                    Quaternion playerRotation = new Quaternion(openVrOutputArr[3 + playerIndex], openVrOutputArr[4 + playerIndex], openVrOutputArr[5 + playerIndex], openVrOutputArr[6 + playerIndex]);

                    if (calibrated)
                    {
                        Vector3 calibratedPos = CalibrationUtils.CalibrateRawPos(playersRawPosition, enableYAxis, calibration, virtualWorldSpace);

                        if (MatchFlowManager.Instance != null)
                            MatchFlowManager.Instance.UpdatePlayerPosition(i + 1, calibratedPos);

                        players[i].GetComponent<PlayerMovement>().SetPosition(calibratedPos);

                        bool isCurrentlyCrouched = calibratedPos.y < crouchThreshold;
                        int playerID = players[i].GetComponent<PlayerMovement>().playerID;

                        // SE AGACHA → Selecciona la ficha
                        if (isCurrentlyCrouched && !playerWasCrouched[i])
                        {
                            if (GameManager.Instance != null)
                            {
                                puckSelected[i] = true;
                                GameManager.Instance.OnPlayerCrouch(playerID, calibratedPos);
                                Debug.Log($"[Tracking] Jugador {playerID} se agacha. Ficha enganchada.");
                            }
                        }

                        // SE LEVANTA → Decide si Deselecciona o Chuta
                        if (!isCurrentlyCrouched && playerWasCrouched[i] && puckSelected[i])
                        {
                            if (GameManager.Instance != null)
                            {
                                GameObject selectedPuck = (playerID == 1)
                                    ? GameManager.Instance.selectedRedPuck
                                    : GameManager.Instance.selectedBluePuck;

                                bool insideSafeZone = false;

                                if (selectedPuck != null)
                                {
                                    Vector2 playerXZ = new Vector2(calibratedPos.x, calibratedPos.z);
                                    Vector2 puckXZ = new Vector2(selectedPuck.transform.position.x, selectedPuck.transform.position.z);

                                    float dist = Vector2.Distance(playerXZ, puckXZ);
                                    insideSafeZone = dist <= safeZoneRadius;

                                    Debug.Log($"[Tracking] Jugador {playerID} se levanta. Distancia a ficha: {dist}. Umbral: {safeZoneRadius}. ¿Dentro?: {insideSafeZone}");
                                }

                                if (insideSafeZone)
                                {
                                    // 1. Apaga el tracker
                                    puckSelected[i] = false;
                                    // 2. Avisa al GameManager para que suelte la ficha físicamente
                                    GameManager.Instance.DeselectPuck(playerID);
                                    Debug.Log($"[Tracking] Jugador {playerID} deselecciona (dentro de la zona segura).");
                                }
                                else
                                {
                                    players[i].GetComponent<PlayerMovement>().KickWithDistance();
                                    puckSelected[i] = false;
                                    Debug.Log($"[Tracking] Jugador {playerID} dispara (fuera de la zona segura).");
                                }
                            }
                        }

                        playerWasCrouched[i] = isCurrentlyCrouched;
                        calibrationUI.SetPlayerXPos(i, calibratedPos);

                        if (enableRotation)
                        {
                            Quaternion calibratedPlayerRotation = CalibrationUtils.CalibratedRawRot(playerRotation, calibration);
                            players[i].GetComponent<PlayerMovement>().SetRotation(calibratedPlayerRotation);
                            calibrationUI.SetPlayerXRot(i, calibratedPlayerRotation);
                        }
                    }
                    else
                    {
                        players[i].GetComponent<PlayerMovement>().SetPosition(playersRawPosition);
                        calibrationUI.SetPlayerXPos(i, playersRawPosition);
                        if (enableRotation)
                        {
                            players[i].GetComponent<PlayerMovement>().SetRotation(playerRotation);
                            calibrationUI.SetPlayerXRot(i, playerRotation);
                        }
                    }
                }
            }
            yield return new WaitForSeconds(positionUpdateInterval);
        }
    }

    private void Update()
    {
        ListenToControls();

        if (!enableTracking)
        {
            DisabledTrackingPlayerSelector();
            DisabledTrackingPlayerMovement();
        }
    }

    private void ListenToControls()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.iKey.wasPressedThisFrame)
        {
            isInterfaceActive = !isInterfaceActive;
            trackingInterface.SetActive(isInterfaceActive);
        }
        if (kb.escapeKey.wasPressedThisFrame) Application.Quit();
    }

    private void DisabledTrackingPlayerSelector()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) playerSelected = 1;
        if (kb.digit2Key.wasPressedThisFrame) playerSelected = 2;
        if (kb.digit3Key.wasPressedThisFrame) playerSelected = 3;
    }

    private void DisabledTrackingPlayerMovement()
    {
        if (players == null || players.Count == 0) return;
        if (playerSelected < 1 || playerSelected > players.Count) return;

        GameObject player = players[playerSelected - 1];
        if (player == null || !player.activeSelf) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        Vector3 move = Vector3.zero;
        if (kb.wKey.isPressed) move += Vector3.forward;
        if (kb.sKey.isPressed) move += Vector3.back;
        if (kb.aKey.isPressed) move += Vector3.left;
        if (kb.dKey.isPressed) move += Vector3.right;
        player.transform.Translate(move * Time.deltaTime * trackingDisabledPlayerSpeed, Space.World);

        if (kb.qKey.isPressed) player.transform.Rotate(Vector3.up, -90f * Time.deltaTime);
        if (kb.eKey.isPressed) player.transform.Rotate(Vector3.up, 90f * Time.deltaTime);

        PlayerMovement pm = player.GetComponent<PlayerMovement>();

        if (kb.fKey.wasPressedThisFrame)
        {
            if (pm == null) return;
            if (GameManager.Instance != null) GameManager.Instance.OnPlayerCrouch(pm.playerID, player.transform.position);
        }

        if (kb.spaceKey.wasPressedThisFrame)
        {
            if (pm != null) pm.KickWithDistance();
        }
    }

    private void OnDisable()
    {
        if (enableTracking && isTrackingInitialized)
            PluginConnector.StopTracking();
    }

    private void UpdateCalibrationUICalibrationData()
    {
        calibrationUI.SetCenter(calibration.GetCalibrationCenter());
        calibrationUI.SetPhysicalWorldSize(calibration.GetCalibrationRealWorldSize());
        calibrationUI.SetRotationOffset(calibration.GetCalibrationRotation());
    }
}