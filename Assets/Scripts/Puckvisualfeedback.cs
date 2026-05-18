using UnityEngine;

/// <summary>
/// Mecanica de tirachinas:
///   - Linea de goma: del jugador a la ficha (muestra la tension)
///   - Flecha de disparo: desde la ficha en la direccion contraria al jugador
///   - Color y longitud de la flecha segun la distancia (potencia)
///   - Anillo alrededor de la ficha seleccionada
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PuckVisualFeedback : MonoBehaviour
{
    [Header("Anillo de seleccion")]
    [SerializeField] private float ringRadius = 1.5f;
    [SerializeField] private int ringSegments = 40;
    [SerializeField] private float ringWidth = 0.08f;
    [SerializeField] private Color ringColor = Color.yellow;

    [Header("Flecha de disparo")]
    [SerializeField] private float minArrowLength = 0.3f;
    [SerializeField] private float maxArrowLength = 5f;
    [SerializeField] private float arrowWidth = 0.08f;
    [SerializeField] private Color colorMin = Color.green;
    [SerializeField] private Color colorMax = Color.red;

    [Header("Linea de goma (tirachinas)")]
    [SerializeField] private float rubberWidth = 0.04f;
    [SerializeField] private Color rubberColor = new Color(1f, 1f, 0f, 0.6f);

    [Header("Configuracion")]
    [SerializeField] private float yOffset = 0.05f;

    private LineRenderer ringLR;
    private LineRenderer arrowLR;
    private LineRenderer rubberLR;
    private PlayerMovement pm;
    private string myTeam;

    private void Awake()
    {
        pm = GetComponent<PlayerMovement>();
        myTeam = (pm.playerID == 1) ? "RedTeam" : "BlueTeam";

        ringLR = CreateLineRenderer("SelectionRing", ringWidth);
        arrowLR = CreateLineRenderer("ShootArrow", arrowWidth);
        rubberLR = CreateLineRenderer("RubberBand", rubberWidth);

        BuildRing();

        arrowLR.positionCount = 2;
        arrowLR.useWorldSpace = true;

        rubberLR.positionCount = 2;
        rubberLR.useWorldSpace = true;
        rubberLR.startColor = rubberColor;
        rubberLR.endColor = new Color(rubberColor.r, rubberColor.g, rubberColor.b, 0f);
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
        rubberLR.enabled = show;

        if (!show) return;

        // Posiciones aplanadas en Y para que todo quede al nivel del suelo
        Vector3 puckPos = new Vector3(puck.transform.position.x,
                                        puck.transform.position.y + yOffset,
                                        puck.transform.position.z);
        Vector3 playerPos = new Vector3(transform.position.x,
                                        puck.transform.position.y + yOffset,
                                        transform.position.z);

        // Anillo alrededor de la ficha
        ringLR.transform.position = puckPos;

        // Vector de ficha al jugador (goma del tirachinas)
        Vector3 toPlayer = playerPos - puckPos;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        // Direccion de disparo = opuesta al jugador
        Vector3 shootDir = (dist > 0.01f) ? -toPlayer.normalized : Vector3.forward;

        // Potencia segun distancia
        float t = Mathf.Clamp01(dist / pm.maxKickDistance);
        float arrowLen = Mathf.Lerp(minArrowLength, maxArrowLength, t);
        Color col = Color.Lerp(colorMin, colorMax, t);

        // Flecha de disparo (desde la ficha hacia donde ira)
        arrowLR.SetPosition(0, puckPos);
        arrowLR.SetPosition(1, puckPos + shootDir * arrowLen);
        arrowLR.startColor = col;
        arrowLR.endColor = new Color(col.r, col.g, col.b, 0f);

        // Linea de goma (del jugador a la ficha)
        rubberLR.SetPosition(0, playerPos);
        rubberLR.SetPosition(1, puckPos);
    }

    private void BuildRing()
    {
        ringLR.loop = true;
        ringLR.useWorldSpace = false;
        ringLR.positionCount = ringSegments;

        for (int i = 0; i < ringSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / ringSegments;
            ringLR.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * ringRadius, 0f,
                Mathf.Sin(angle) * ringRadius));
        }

        ringLR.startColor = ringColor;
        ringLR.endColor = ringColor;
    }

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