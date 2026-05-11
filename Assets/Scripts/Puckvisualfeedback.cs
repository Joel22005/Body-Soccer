using UnityEngine;
[RequireComponent(typeof(PlayerMovement))]
public class PuckVisualFeedback : MonoBehaviour
{
    [Header("Anillo de seleccion")]
    [SerializeField] private float ringRadius = 0.65f;
    [SerializeField] private int ringSegments = 40;
    [SerializeField] private float ringWidth = 0.05f;
    [SerializeField] private Color ringColor = Color.yellow;

    [Header("Flecha de direccion / potencia")]
    [SerializeField] private float minArrowLength = 0.5f;
    [SerializeField] private float maxArrowLength = 4f;
    [SerializeField] private float arrowWidth = 0.06f;
    [SerializeField] private Color colorMin = Color.green;
    [SerializeField] private Color colorMax = Color.red;

    [Header("Altura sobre el suelo")]
    [SerializeField] private float yOffset = 0.05f;

    // LineRenderers creados en tiempo de ejecucion
    private LineRenderer ringLR;
    private LineRenderer arrowLR;
    private PlayerMovement pm;
    private string myTeam;

    // ---------------------------------------------------------------
    private void Awake()
    {
        pm = GetComponent<PlayerMovement>();
        myTeam = (pm.playerID == 1) ? "RedTeam" : "BlueTeam";

        ringLR = CreateLineRenderer("SelectionRing", ringWidth);
        arrowLR = CreateLineRenderer("DirectionArrow", arrowWidth);

        BuildRing();

        arrowLR.positionCount = 2;
        arrowLR.useWorldSpace = true;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        bool isMyTurn = GameManager.Instance.currentTurnTeam == myTeam;

        GameObject puck = (pm.playerID == 1)
            ? GameManager.Instance.selectedRedPuck
            : GameManager.Instance.selectedBluePuck;

        bool show = isMyTurn && puck != null;
        ringLR.enabled = show;
        arrowLR.enabled = show;

        if (!show) return;

        Vector3 puckPos = puck.transform.position;

        // --- Anillo ---
        // El ring esta construido en espacio local; moverlo con el transform
        ringLR.transform.position = new Vector3(puckPos.x, puckPos.y + yOffset, puckPos.z);

        // --- Flecha ---
        float dist = Vector3.Distance(transform.position, puck.transform.position);
        float charge = Mathf.Clamp01(dist / pm.maxKickDistance);
        float length = Mathf.Lerp(minArrowLength, maxArrowLength, charge);
        Color col = Color.Lerp(colorMin, colorMax, charge);
        Vector3 dir = transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
        dir.Normalize();

        Vector3 arrowStart = new Vector3(puckPos.x, puckPos.y + yOffset, puckPos.z);
        Vector3 arrowEnd = arrowStart + dir * length;

        arrowLR.SetPosition(0, arrowStart);
        arrowLR.SetPosition(1, arrowEnd);
        arrowLR.startColor = col;
        arrowLR.endColor = new Color(col.r, col.g, col.b, 0f); // fade en la punta
    }

    // ---------------------------------------------------------------
    // Construye el anillo como un circulo en espacio local
    private void BuildRing()
    {
        ringLR.loop = true;
        ringLR.useWorldSpace = false;
        ringLR.positionCount = ringSegments;

        for (int i = 0; i < ringSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / ringSegments;
            ringLR.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * ringRadius,
                0f,
                Mathf.Sin(angle) * ringRadius));
        }

        ringLR.startColor = ringColor;
        ringLR.endColor = ringColor;
    }

    // Crea un LineRenderer en un GameObject hijo con material basico
    private LineRenderer CreateLineRenderer(string goName, float width)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = width;
        lr.endWidth = width;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        return lr;
    }
}