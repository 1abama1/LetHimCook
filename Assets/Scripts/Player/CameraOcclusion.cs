using UnityEngine;
using System.Collections.Generic;
public class CameraOcclusion : MonoBehaviour
{
    public Transform player;
    public LayerMask buildingLayer;
    public float cutoutRadius = 2f;
    
    public float fadeSpeed = 3f;
    public float targetAlpha = 0.2f;

    private Dictionary<Renderer, Material[]> _originalMats = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Material[]> _fadeMats = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, float> _currentAlphas = new Dictionary<Renderer, float>();
    
    private HashSet<Renderer> _hiddenThisFrame = new HashSet<Renderer>();
    private List<Renderer> _toRemove = new List<Renderer>();

    void LateUpdate()
    {
        if (player == null) return;
        _hiddenThisFrame.Clear();

        Vector3 targetPos = player.position + Vector3.up * 1f;
        Vector3 dir = targetPos - transform.position;
        float dist = dir.magnitude;

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, cutoutRadius, dir.normalized, dist, buildingLayer);

        foreach (var hit in hits)
        {
            Renderer r = hit.collider.GetComponent<Renderer>();
            if (r != null && hit.normal.y < 0.9f)
            {
                _hiddenThisFrame.Add(r);
                
                if (!_originalMats.ContainsKey(r))
                {
                    SetupTransparentMaterials(r);
                }
                
                if (r.materials != _fadeMats[r]) r.materials = _fadeMats[r];
                
                _currentAlphas[r] = Mathf.MoveTowards(_currentAlphas[r], targetAlpha, Time.deltaTime * fadeSpeed);
                SetAlpha(r, _currentAlphas[r]);
            }
        }

        _toRemove.Clear();
        foreach (var r in _originalMats.Keys)
        {
            if (!_hiddenThisFrame.Contains(r))
            {
                _currentAlphas[r] = Mathf.MoveTowards(_currentAlphas[r], 1f, Time.deltaTime * fadeSpeed);
                SetAlpha(r, _currentAlphas[r]);
                
                if (_currentAlphas[r] >= 0.99f)
                {
                    _toRemove.Add(r);
                }
            }
        }

        foreach (var r in _toRemove)
        {
            r.materials = _originalMats[r];
            _originalMats.Remove(r);
            _fadeMats.Remove(r);
            _currentAlphas.Remove(r);
        }
    }

    void SetupTransparentMaterials(Renderer r)
    {
        _originalMats[r] = r.materials;
        Material[] transMats = new Material[r.materials.Length];
        
        for (int i = 0; i < r.materials.Length; i++)
        {
            transMats[i] = new Material(r.materials[i]);
            
            transMats[i].SetFloat("_Surface", 1); 
            transMats[i].SetFloat("_Mode", 3);
            transMats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transMats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transMats[i].SetInt("_ZWrite", 0);
            transMats[i].DisableKeyword("_ALPHATEST_ON");
            transMats[i].EnableKeyword("_ALPHABLEND_ON");
            transMats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
            transMats[i].renderQueue = 3000;
        }
        
        _fadeMats[r] = transMats;
        _currentAlphas[r] = 1f;
    }

    void SetAlpha(Renderer r, float a)
    {
        Material[] mats = _fadeMats[r];
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i].HasProperty("_BaseColor"))
            {
                Color c = mats[i].GetColor("_BaseColor");
                c.a = a;
                mats[i].SetColor("_BaseColor", c);
            }
            if (mats[i].HasProperty("_Color"))
            {
                Color c = mats[i].GetColor("_Color");
                c.a = a;
                mats[i].SetColor("_Color", c);
            }
        }
    }
}
