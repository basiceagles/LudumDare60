using UnityEngine;

[ExecuteAlways]
public class SwitchboardCable : MonoBehaviour
{
    public Transform startAnchor;
    public Transform endAnchor;
    public Transform plug;
    public Material cableMaterial;
    public Texture2D cableTexture;
    [Min(0.001f)] public float diameter = 0.012f;
    [Min(0.01f)] public float textureRepeatsPerMeter = 5f;
    [Min(0)] public float sag = 0.25f;
    public Vector3 bend = new Vector3(0, 0, -0.15f);

    private const int Segments = 24;
    private readonly Vector3[] points = new Vector3[Segments + 1];
    private LineRenderer lineRenderer;

    private void OnEnable()
    {
        InitRenderer();
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void InitRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.numCornerVertices = 6;
            lineRenderer.numCapVertices = 6;
        }

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null && mr.enabled)
        {
            mr.enabled = false;
        }
    }

    public void Refresh()
    {
        if (startAnchor == null || endAnchor == null)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
            return;
        }

        InitRenderer();
        lineRenderer.enabled = true;
        lineRenderer.sharedMaterial = cableMaterial;
        lineRenderer.startWidth = diameter;
        lineRenderer.endWidth = diameter;
        lineRenderer.positionCount = Segments + 1;

        Vector3 start = startAnchor.position;
        Vector3 end = endAnchor.position;
        float dist = Vector3.Distance(start, end);
        float radius = diameter * 0.5f;
        Vector3 droop = Vector3.Lerp(bend + Vector3.down * sag, Vector3.zero, dist / 1.7f);
        float castY = Mathf.Max(start.y, end.y) + 0.8f;

        points[0] = start;
        points[Segments] = end;

        for (int i = 1; i < Segments; i++)
        {
            float t = (float)i / Segments;
            Vector3 pt = Vector3.Lerp(start, end, t) + droop * Mathf.Sin(t * Mathf.PI);
            points[i] = ClampToSurface(pt, castY, radius);
        }

        for (int i = 1; i < Segments; i++)
        {
            points[i] = ClampToSurface((points[i - 1] + points[i + 1]) * 0.5f, castY, radius);
        }

        lineRenderer.SetPositions(points);
    }

    private Vector3 ClampToSurface(Vector3 pt, float castY, float radius)
    {
        if (Physics.Raycast(new Vector3(pt.x, castY, pt.z), Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (plug == null || !hit.collider.transform.IsChildOf(plug))
            {
                float surfaceY = hit.point.y + radius + 0.002f;
                if (pt.y < surfaceY)
                {
                    pt.y = surfaceY;
                }
            }
        }

        return pt;
    }
}
