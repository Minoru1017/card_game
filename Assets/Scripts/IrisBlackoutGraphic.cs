using UnityEngine;
using UnityEngine.UI;

/// <summary>全螢幕圓形光圈遮罩（Unity UI MaskableGraphic + 自訂 shader）。</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class IrisBlackoutGraphic : MaskableGraphic
{
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int UseSnapshotId = Shader.PropertyToID("_UseSnapshot");
    private static readonly int SnapshotTexId = Shader.PropertyToID("_SnapshotTex");

    private static Material s_defaultMaterial;

    [SerializeField] private float radius;
    [SerializeField] private float aspect = 1.777f;
    [SerializeField] private float edgeSoftness = 0.018f;

    private Material instanceMaterial;
    private Texture snapshotTexture;
    private bool useSnapshot;

    public float Radius
    {
        get => radius;
        set
        {
            value = Mathf.Max(0f, value);
            if (Mathf.Approximately(radius, value))
                return;
            radius = value;
            ApplyMaterialProperties();
            SetMaterialDirty();
        }
    }

    public float Aspect
    {
        get => aspect;
        set
        {
            if (Mathf.Approximately(aspect, value))
                return;
            aspect = value;
            ApplyMaterialProperties();
            SetMaterialDirty();
        }
    }

    public float EdgeSoftness
    {
        get => edgeSoftness;
        set
        {
            if (Mathf.Approximately(edgeSoftness, value))
                return;
            edgeSoftness = value;
            ApplyMaterialProperties();
            SetMaterialDirty();
        }
    }

    public override Material defaultMaterial => ResolveDefaultMaterial();

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        useLegacyMeshGeneration = false;

        Material template = ResolveDefaultMaterial();
        if (template != null)
        {
            instanceMaterial = new Material(template);
            material = instanceMaterial;
        }

        ApplyMaterialProperties();
    }

    protected override void OnDestroy()
    {
        if (instanceMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(instanceMaterial);
            else
                DestroyImmediate(instanceMaterial);
            instanceMaterial = null;
        }

        base.OnDestroy();
    }

    public void SetSnapshot(Texture texture)
    {
        snapshotTexture = texture;
        useSnapshot = snapshotTexture != null;
        ApplyMaterialProperties();
        SetMaterialDirty();
    }

    public void ClearSnapshot()
    {
        snapshotTexture = null;
        useSnapshot = false;
        ApplyMaterialProperties();
        SetMaterialDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        var tint = (Color32)color;
        vh.AddVert(new Vector3(rect.xMin, rect.yMin), tint, new Vector2(0f, 0f));
        vh.AddVert(new Vector3(rect.xMin, rect.yMax), tint, new Vector2(0f, 1f));
        vh.AddVert(new Vector3(rect.xMax, rect.yMax), tint, new Vector2(1f, 1f));
        vh.AddVert(new Vector3(rect.xMax, rect.yMin), tint, new Vector2(1f, 0f));
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);
    }

    protected override void UpdateMaterial()
    {
        base.UpdateMaterial();
        ApplyMaterialProperties();
    }

    private void ApplyMaterialProperties()
    {
        Material mat = instanceMaterial != null ? instanceMaterial : materialForRendering;
        if (mat == null)
            return;

        mat.SetFloat(RadiusId, radius);
        mat.SetFloat(SoftnessId, edgeSoftness);
        mat.SetFloat(AspectId, aspect);
        mat.SetFloat(UseSnapshotId, useSnapshot ? 1f : 0f);
        if (snapshotTexture != null)
            mat.SetTexture(SnapshotTexId, snapshotTexture);
    }

    private static Material ResolveDefaultMaterial()
    {
        if (s_defaultMaterial != null)
            return s_defaultMaterial;

        Shader shader = Shader.Find("UI/IrisBlackout");
        if (shader == null)
        {
            Debug.LogWarning("IrisBlackoutGraphic: UI/IrisBlackout shader not found.");
            return null;
        }

        s_defaultMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        return s_defaultMaterial;
    }
}
