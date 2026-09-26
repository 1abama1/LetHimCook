using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Прозрачность зданий между камерой и игроком.
/// Использует SphereCastNonAlloc, renderer/материалы кешируются,
/// sharedMaterials используется для записи/чтения (без неявных инстансов через getter).
/// OnDestroy восстанавливает все оригинальные материалы и уничтожает runtime-копии.
/// </summary>
public class CameraOcclusion : MonoBehaviour
{
    [SerializeField] public Transform player;
    [SerializeField] public LayerMask buildingLayer;
    [SerializeField] public float cutoutRadius = 2f;
    [SerializeField] public float fadeSpeed    = 3f;
    [SerializeField] public float targetAlpha  = 0.2f;

    private const int MaxHits = 32;

    // Pre-allocated physics buffer — zero GC в LateUpdate.
    private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];

    // Collider → Renderer: GetComponent вызывается ровно один раз на коллайдер.
    private readonly Dictionary<Collider, Renderer> _rendererCache = new();

    // Сохранённые оригинальные sharedMaterials (snapshot при первом входе в occlusion).
    private readonly Dictionary<Renderer, Material[]> _originalMats = new();

    // Созданные runtime fade-материалы (копии оригинальных, с прозрачностью).
    private readonly Dictionary<Renderer, Material[]> _fadeMats = new();

    // Текущая alpha каждого окклюдированного renderer.
    private readonly Dictionary<Renderer, float> _currentAlphas = new();

    // Множество renderer'ов, перекрывших игрока в текущем кадре.
    private readonly HashSet<Renderer> _hiddenThisFrame = new();

    // Буфер для renderer'ов, которые полностью восстановлены и должны быть удалены.
    private readonly List<Renderer> _toRestore = new();

    // ─────────────────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        if (player == null) return;

        _hiddenThisFrame.Clear();

        Vector3 targetPosition = player.position + Vector3.up;
        Vector3 direction      = targetPosition - transform.position;
        float   distance       = direction.magnitude;

        if (distance <= Mathf.Epsilon) return;

        Vector3 normalizedDir = direction / distance;

        int hitCount = Physics.SphereCastNonAlloc(
            transform.position,
            cutoutRadius,
            normalizedDir,
            _hits,
            distance,
            buildingLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _hits[i].collider;

            if (!_rendererCache.TryGetValue(col, out Renderer renderer))
            {
                renderer = col.GetComponent<Renderer>();
                _rendererCache[col] = renderer; // кешируем null тоже, чтобы не повторять GetComponent
            }

            // Пропускаем горизонтальные поверхности (полы) и null-renderer'ы.
            if (renderer == null || _hits[i].normal.y >= 0.9f)
                continue;

            _hiddenThisFrame.Add(renderer);

            // Первый вход — делаем snapshot и создаём fade-материалы.
            if (!_originalMats.ContainsKey(renderer))
                SetupTransparentMaterials(renderer);

            // Двигаем alpha к targetAlpha.
            float alpha = Mathf.MoveTowards(
                _currentAlphas[renderer],
                targetAlpha,
                fadeSpeed * Time.deltaTime
            );
            _currentAlphas[renderer] = alpha;
            SetAlpha(renderer, alpha);
        }

        RestoreVisibleRenderers();
    }

    /// <summary>
    /// Для renderer'ов, которые не перекрывали игрока в этом кадре,
    /// плавно возвращаем alpha к 1 и при достижении восстанавливаем оригинальные материалы.
    /// </summary>
    private void RestoreVisibleRenderers()
    {
        _toRestore.Clear();

        foreach (Renderer renderer in _originalMats.Keys)
        {
            if (_hiddenThisFrame.Contains(renderer))
                continue;

            float alpha = Mathf.MoveTowards(
                _currentAlphas[renderer],
                1f,
                fadeSpeed * Time.deltaTime
            );
            _currentAlphas[renderer] = alpha;
            SetAlpha(renderer, alpha);

            if (alpha >= 0.99f)
                _toRestore.Add(renderer);
        }

        foreach (Renderer renderer in _toRestore)
            RestoreRenderer(renderer);
    }

    /// <summary>
    /// Снимает snapshot sharedMaterials, создаёт runtime fade-копии и
    /// назначает их как sharedMaterials (один раз при входе в occlusion).
    /// </summary>
    private void SetupTransparentMaterials(Renderer renderer)
    {
        // Snapshot оригиналов — только shared, без неявного инстансирования.
        Material[] originalMaterials = renderer.sharedMaterials;
        _originalMats[renderer] = originalMaterials;

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Standard");

        Material[] fadeMaterials = new Material[originalMaterials.Length];

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            Material original = originalMaterials[i];
            if (original == null)
            {
                fadeMaterials[i] = null;
                continue;
            }

            // Копируем оригинал целиком — сохраняем текстуры и цвет.
            Material fadeMat = new Material(original);

            // Переключаем шейдер на Lit/Standard для поддержки прозрачности,
            // если текущий шейдер не поддерживает _Surface/_Mode.
            if (!fadeMat.HasProperty("_Surface") && litShader != null)
                fadeMat.shader = litShader;

            ConfigureTransparency(fadeMat);
            fadeMaterials[i] = fadeMat;
        }

        _fadeMats[renderer]     = fadeMaterials;
        _currentAlphas[renderer] = 1f;

        // Назначаем через sharedMaterials — без GC-аллокации getter'а.
        renderer.sharedMaterials = fadeMaterials;
    }

    private static void ConfigureTransparency(Material mat)
    {
        if (mat.HasProperty("_Surface"))  mat.SetFloat("_Surface", 1f); // 1 = Transparent
        if (mat.HasProperty("_Mode"))     mat.SetFloat("_Mode",    3f); // 3 = Transparent
        if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite"))   mat.SetInt("_ZWrite", 0);

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    /// <summary>Устанавливает alpha всем fade-материалам renderer'а.</summary>
    private void SetAlpha(Renderer renderer, float alpha)
    {
        if (!_fadeMats.TryGetValue(renderer, out Material[] mats)) return;

        for (int i = 0; i < mats.Length; i++)
        {
            Material mat = mats[i];
            if (mat == null) continue;

            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
            else if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                c.a = alpha;
                mat.SetColor("_Color", c);
            }
        }
    }

    /// <summary>
    /// Восстанавливает оригинальные sharedMaterials renderer'а и
    /// уничтожает runtime fade-материалы.
    /// </summary>
    private void RestoreRenderer(Renderer renderer)
    {
        if (renderer != null && _originalMats.TryGetValue(renderer, out Material[] originals))
            renderer.sharedMaterials = originals;

        if (_fadeMats.TryGetValue(renderer, out Material[] fadeMats))
        {
            foreach (Material mat in fadeMats)
                if (mat != null) Destroy(mat);
        }

        _originalMats.Remove(renderer);
        _fadeMats.Remove(renderer);
        _currentAlphas.Remove(renderer);
    }

    /// <summary>
    /// При уничтожении компонента — восстанавливаем все renderer'ы и
    /// уничтожаем все runtime-материалы, чтобы не оставить сцену в битом состоянии.
    /// </summary>
    private void OnDestroy()
    {
        // Копируем ключи, т.к. RestoreRenderer модифицирует словари.
        _toRestore.Clear();
        _toRestore.AddRange(_originalMats.Keys);

        foreach (Renderer renderer in _toRestore)
            RestoreRenderer(renderer);

        _rendererCache.Clear();
        _hiddenThisFrame.Clear();
    }
}
