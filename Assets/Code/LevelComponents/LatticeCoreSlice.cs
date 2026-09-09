using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class LatticeCoreSlice : MonoBehaviour
{
    public Color _offLightColor;
    public Color _completeLightColor;
    
    private Collider _collider;

    public Material material;
    private Renderer _renderer;
    private CustomPointLight _light;
    private ParticleSystem _onParticles;
    private ParticleSystem _offParticles;
    private bool _on;
    private bool _complete;
    

    private void Awake()
    {
        _on = false;
        _collider = GetComponentInChildren<Collider>();
        _light = GetComponentInChildren<CustomPointLight>();
        _light.Color = _offLightColor;
        _light.gameObject.SetActive(false);

        _renderer = _collider.transform.GetComponent<Renderer>();
        material = _renderer.material;
        _collider.enabled = false;
        _renderer.enabled = false;
        _onParticles = transform.Find("OnParticles").GetComponent<ParticleSystem>();
        _offParticles = transform.Find("OffParticles").GetComponent<ParticleSystem>();
        // _offParticles.Play();
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    public void MakeSliceCompleted()
    {
        _renderer.enabled = true;
        _onParticles.Play();
        _offParticles.Clear();
        _offParticles.Stop();
        _complete = true;
        _collider.enabled = true;
        _light.gameObject.SetActive(true);
        _light.Color = _completeLightColor;
        material.SetFloat("_SolidWeight", 1f);
        material.SetFloat("_CompleteWeight", 1f);
        StartCoroutine(GlowCoroutine());
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void SetAdjacencies(bool top, bool bottom)
    {
        material.SetFloat("_TopMask", top ? 1f : 0f);
        material.SetFloat("_BottomMask", bottom ? 1f : 0f);
    }
    
    private IEnumerator GlowCoroutine()
    {
        var t = 0f;
        var d = 0.5f;
        while (t < d)
        {
            material.SetFloat("_GlowWeight", 1f - (t / d));
            t += Time.deltaTime;
            yield return null;
        }
            
        material.SetFloat("_GlowWeight", 0f);

        if (!_on)
        {
            _on = true;
        }
    }
}
