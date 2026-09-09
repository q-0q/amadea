using System;
using System.Collections;
using UnityEngine;

public class LatticeNode : MonoBehaviour
{
    public Color _offLightColor;
    public Color _onLightColor;
    public Color _completeLightColor;
    
    private Collider _collider;

    private Material _material;
    private CustomPointLight _light;
    private ParticleSystem _onParticles;
    private ParticleSystem _offParticles;
    private bool _on;
    private bool _complete;
    private Lattice _lattice;

    private Material _cellMaterial;

    private void Awake()
    {
        _on = false;
        _collider = GetComponentInChildren<Collider>();
        _light = GetComponentInChildren<CustomPointLight>();
        _light.Color = _offLightColor;
        _material = _collider.transform.GetComponent<Renderer>().material;
        _collider.enabled = false;
        _onParticles = transform.Find("OnParticles").GetComponent<ParticleSystem>();
        _offParticles = transform.Find("OffParticles").GetComponent<ParticleSystem>();
        _offParticles.Play();
        
    }

    private void OnEnable()
    {
        DroneFsm.OnDronePulsed += OnDronePulsed;
        Lattice.OnLatticeCompleted += OnLatticeCompleted;
    }

    private void OnDisable()
    {
        DroneFsm.OnDronePulsed -= OnDronePulsed;
        Lattice.OnLatticeCompleted -= OnLatticeCompleted;
    }

    private void OnDronePulsed(Vector3 dronePosition)
    {
        var d = Vector3.Distance(dronePosition, transform.position);
        if (d > DroneFsm.DronePulseRadius) return;
        StartCoroutine(GlowCoroutine());
        _collider.enabled = true;
        _light.gameObject.SetActive(true);
        _light.Color = _complete ? _completeLightColor : _onLightColor;
        _material.SetFloat("_SolidWeight", 1f);
        _onParticles.Play();
        _offParticles.Clear();
        _offParticles.Stop();
        _cellMaterial.SetFloat("_OnWeight", 1f);
        
    }

    private void OnLatticeCompleted(Lattice lattice, bool _)
    {
        if (lattice != _lattice) return;
        _on = true;
        _onParticles.Play();
        _offParticles.Clear();
        _offParticles.Stop();
        _complete = true;
        _collider.enabled = true;
        _light.gameObject.SetActive(true);
        _light.Color = _completeLightColor;
        _material.SetFloat("_SolidWeight", 1f);
        _material.SetFloat("_CompleteWeight", 1f);
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

    public void SetLattice(Lattice lattice)
    {
        _lattice = lattice;
    }

    public void SetCellMaterial(Material material)
    {
        _cellMaterial = material;
    }
    
    public void SetAdjacencies(bool left, bool right, bool top, bool bottom, bool front, bool back)
    {
        _material.SetFloat("_LeftMask", left ? 1f : 0f);
        _material.SetFloat("_RightMask", right ? 1f : 0f);
        _material.SetFloat("_TopMask", top ? 1f : 0f);
        _material.SetFloat("_BottomMask", bottom ? 1f : 0f);
        _material.SetFloat("_FrontMask", front ? 1f : 0f);
        _material.SetFloat("_BackMask", back ? 1f : 0f);
    }
    
    private IEnumerator GlowCoroutine()
    {
        var t = 0f;
        var d = 0.5f;
        while (t < d)
        {
            _material.SetFloat("_GlowWeight", 1f - (t / d));
            t += Time.deltaTime;
            yield return null;
        }
            
        _material.SetFloat("_GlowWeight", 0f);

        if (!_on)
        {
            _on = true;
            _lattice.IncrementCompletedNodes();
        }
    }
}
