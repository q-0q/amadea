using System;
using System.Collections;
using Cinemachine;
using UnityEngine;

public class GravityCamera : MonoBehaviour
{
    private CinemachineVirtualCamera _camera;

    private void Awake()
    {
        _camera = GetComponent<CinemachineVirtualCamera>();
        _camera.Priority = -100;
    }

    private void OnEnable()
    {
        GravityGun.OnGravityGunUsed += OnGravityGun;
    }

    private void OnDisable()
    {
        GravityGun.OnGravityGunUsed -= OnGravityGun;
    }

    private void OnGravityGun()
    {
        StartCoroutine(Coroutine());
        IEnumerator Coroutine()
        {
            _camera.transform.position = Camera.main.transform.position;
            _camera.transform.rotation = Camera.main.transform.rotation;
            _camera.Priority = 100;
            yield return new WaitForSeconds(0.05f);
            _camera.Priority = -100;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
