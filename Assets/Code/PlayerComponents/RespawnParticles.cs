using System;
using System.Collections;
using Code.Misc;
using DG.Tweening;
using UnityEngine;

public class RespawnParticles : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    private Renderer _curvedStarRenderer;
    private Renderer _haloRenderer;
    private CustomPointLight _light;
    private Color _baseLightColor;

    private Vector3 _baseHaloLocalScale;
    

    private void Awake()
    {
        _particleSystem = GetComponentInChildren<ParticleSystem>();
        _curvedStarRenderer = _particleSystem.transform.Find("CurvedStar").GetComponent<Renderer>();
        _haloRenderer = _particleSystem.transform.Find("Halo").GetComponent<Renderer>();
        _light = GetComponentInChildren<CustomPointLight>();
        _baseLightColor = _light.Color;
        _baseHaloLocalScale = _haloRenderer.transform.localScale;
        SetValues(1f, 1f, 0f);
    }

    public void PlayDeath()
    {
        StartCoroutine(MainCoroutine());
        StartCoroutine(ScaleCoroutine());

        IEnumerator MainCoroutine()
        {
            transform.position = PlayerFsm.Singleton.transform.position;
            
            
            yield return new WaitForSeconds(0.25f);
            _particleSystem.Play();
            
            var t = 0f;
            var d = 0.5f;
            
            
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                SetValues(1f - w, 1f - w, w);

                
                t += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.25f);
            
            t = 0f;
            d = 1f;
            
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                w = (Mathf.Pow(w, 0.5f));
                SetValues(w, w, 1f - w);
                
                t += Time.deltaTime;
                yield return null;
            }
            
            _particleSystem.Stop();
            
            
        }
        
        IEnumerator ScaleCoroutine()
        {
            
            
            var newScale = _baseHaloLocalScale * 0.75f;
            _haloRenderer.transform.localScale = newScale;
            yield return new WaitForSeconds(0.75f);


            
            var t = 0f;
            var d = 0.25f;
            
            
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                w = (Mathf.Pow(w, 0.5f));
                _haloRenderer.transform.localScale = Vector3.Lerp(newScale, _baseHaloLocalScale, w);
                t += Time.deltaTime;
                yield return null;
            }
            
        }
    }

    public void PlayRespawn()
    {
        
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        
    }

    public void SetValues(float starClip, float haloClip, float lightBrightness)
    {
        starClip = 1f;
        _curvedStarRenderer.material.SetFloat("_Clip", starClip);
        _haloRenderer.material.SetFloat("_Clip", Mathf.Lerp(-0.05f, 1.5f, haloClip));
        _light.Color = _baseLightColor * lightBrightness;

        _curvedStarRenderer.enabled = starClip < 0.999f;
        _haloRenderer.enabled = haloClip < 0.999f;
        _light.gameObject.SetActive(lightBrightness > 0.001f);
    }
}
