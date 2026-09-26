using System.Collections.Generic;
using UnityEngine;

public sealed class CameraOcclusion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Occlusion")]
    [SerializeField] private LayerMask buildingLayer;
    [SerializeField, Min(0.05f)] private float cutoutRadius = 2f;
    [SerializeField, Min(0.01f)] private float fadeSpeed = 3f;
    [SerializeField, Range(0.05f, 1f)] private float targetAlpha = 0.2f;

    [Header("Optional fallback")]
    [Tooltip("Используется только если исходный shader не поддерживает transparency.")]
    [SerializeField] private Shader fallbackTransparentShader;

    private const int MaxHits = 32;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int ModeId = Shader.PropertyToID("_Mode");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

    private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
    private readonly Dictionary<Collider, Renderer> _rendererCache = new();
    private readonly Dictionary<Renderer, FadeState> _states = new();
    private readonly HashSet<Renderer> _hiddenThisFrame = new();
    private readonly List<Renderer> _activeRenderers = new();
    private readonly List<Renderer> _removeBuffer = new();

    private sealed class FadeState
    {
        public Material[] OriginalMaterials;
        public Material[] FadeMaterials;
        // 0 = нет цвета, 1 = _BaseColor, 2 = _Color
        public byte[] ColorProperties;
        public float Alpha = 1f;
        public bool FadeMaterialsAssigned;
        public bool Active;
    }

    private void Awake()
    {
        // Shader.Find не должен выполняться при каждом новом Renderer.
        if (fallbackTransparentShader == null)
        {
            fallbackTransparentShader = Shader.Find("Universal Render Pipeline/Lit");
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        _hiddenThisFrame.Clear();

        Vector3 origin = transform.position;
        Vector3 target = player.position + Vector3.up;
        Vector3 direction = target - origin;
        float distanceSq = direction.sqrMagnitude;

        if (distanceSq <= 0.0001f) return;

        float distance = Mathf.Sqrt(distanceSq);
        Vector3 normalizedDirection = direction / distance;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            cutoutRadius,
            normalizedDirection,
            _hits,
            distance,
            buildingLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hits[i];

            // Не fade'им полы/крыши, если удар почти вертикальный.
            if (hit.normal.y >= 0.9f) continue;

            Renderer renderer = GetCachedRenderer(hit.collider);
            if (renderer == null) continue;

            if (!_hiddenThisFrame.Add(renderer)) continue;

            FadeState state = GetOrCreateState(renderer);
            if (!state.Active)
            {
                state.Active = true;
                _activeRenderers.Add(renderer);
            }

            EnsureFadeMaterialsAssigned(renderer, state);

            state.Alpha = Mathf.MoveTowards(
                state.Alpha,
                targetAlpha,
                fadeSpeed * Time.deltaTime
            );
            ApplyAlpha(state, state.Alpha);
        }

        RestoreVisibleRenderers();
    }

    private Renderer GetCachedRenderer(Collider col)
    {
        if (_rendererCache.TryGetValue(col, out Renderer cached)) return cached;

        // Выполняется максимум один раз для данного Collider.
        Renderer renderer = col.GetComponent<Renderer>();
        _rendererCache.Add(col, renderer);
        return renderer;
    }

    private FadeState GetOrCreateState(Renderer renderer)
    {
        if (_states.TryGetValue(renderer, out FadeState state)) return state;

        Material[] originals = renderer.sharedMaterials;
        int count = originals.Length;
        Material[] fadeMaterials = new Material[count];
        byte[] colorProperties = new byte[count];

        for (int i = 0; i < count; i++)
        {
            Material original = originals[i];
            if (original == null) continue;

            Material fade = new Material(original)
            {
                name = original.name + " [Occlusion Runtime]"
            };

            bool supportsSurface = fade.HasProperty(SurfaceId) || fade.HasProperty(ModeId);
            if (!supportsSurface && fallbackTransparentShader != null)
                fade.shader = fallbackTransparentShader;

            ConfigureTransparency(fade);

            if (fade.HasProperty(BaseColorId))
                colorProperties[i] = 1;
            else if (fade.HasProperty(ColorId))
                colorProperties[i] = 2;

            fadeMaterials[i] = fade;
        }

        state = new FadeState
        {
            OriginalMaterials = originals,
            FadeMaterials = fadeMaterials,
            ColorProperties = colorProperties,
            Alpha = 1f
        };

        _states.Add(renderer, state);
        return state;
    }

    private static void ConfigureTransparency(Material material)
    {
        if (material.HasProperty(SurfaceId)) material.SetFloat(SurfaceId, 1f);
        if (material.HasProperty(ModeId)) material.SetFloat(ModeId, 3f);
        if (material.HasProperty(SrcBlendId))
        {
            material.SetInt(
                SrcBlendId,
                (int)UnityEngine.Rendering.BlendMode.SrcAlpha
            );
        }
        if (material.HasProperty(DstBlendId))
        {
            material.SetInt(
                DstBlendId,
                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
            );
        }
        if (material.HasProperty(ZWriteId)) material.SetInt(ZWriteId, 0);

        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void EnsureFadeMaterialsAssigned(
        Renderer renderer,
        FadeState state)
    {
        if (state.FadeMaterialsAssigned) return;
        renderer.sharedMaterials = state.FadeMaterials;
        state.FadeMaterialsAssigned = true;
    }

    private static void ApplyAlpha(FadeState state, float alpha)
    {
        Material[] materials = state.FadeMaterials;
        byte[] properties = state.ColorProperties;

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null) continue;

            switch (properties[i])
            {
                case 1:
                {
                    Color color = material.GetColor(BaseColorId);
                    color.a = alpha;
                    material.SetColor(BaseColorId, color);
                    break;
                }
                case 2:
                {
                    Color color = material.GetColor(ColorId);
                    color.a = alpha;
                    material.SetColor(ColorId, color);
                    break;
                }
            }
        }
    }

    private void RestoreVisibleRenderers()
    {
        float delta = fadeSpeed * Time.deltaTime;

        for (int i = _activeRenderers.Count - 1; i >= 0; i--)
        {
            Renderer renderer = _activeRenderers[i];
            if (renderer == null)
            {
                _activeRenderers.RemoveAt(i);
                continue;
            }

            if (_hiddenThisFrame.Contains(renderer)) continue;

            if (!_states.TryGetValue(renderer, out FadeState state))
            {
                _activeRenderers.RemoveAt(i);
                continue;
            }

            state.Alpha = Mathf.MoveTowards(
                state.Alpha,
                1f,
                delta
            );
            ApplyAlpha(state, state.Alpha);

            if (state.Alpha < 0.999f) continue;

            // Возвращаем shared материалы, но fade-копии НЕ уничтожаем.
            renderer.sharedMaterials = state.OriginalMaterials;
            state.FadeMaterialsAssigned = false;
            state.Active = false;
            _activeRenderers.RemoveAt(i);
        }
    }

    private void OnDestroy()
    {
        _removeBuffer.Clear();

        foreach (KeyValuePair<Renderer, FadeState> pair in _states)
        {
            Renderer renderer = pair.Key;
            FadeState state = pair.Value;

            if (renderer != null)
                renderer.sharedMaterials = state.OriginalMaterials;

            Material[] materials = state.FadeMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material != null)
                    Destroy(material);
            }
        }

        _states.Clear();
        _rendererCache.Clear();
        _hiddenThisFrame.Clear();
        _activeRenderers.Clear();
    }
}
