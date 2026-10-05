using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUpdraftParticles : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    public bool side = false;

    // Start is called before the first frame update
    void Start()
    {
        TryGetComponent(out _particleSystem);
    }

    // Update is called once per frame
    void Update()
    {
        if (!PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Updraft) &&
            !PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.SlideLateral))
        {
            _particleSystem.Stop();
        }
    }

    private void OnPlayerEnterUpdraft()
    {
        // print(transform.parent.name);
        _particleSystem.Play();
    }

    private void OnPlayerEnterSlideLateral(bool flip)
    {
        if (flip != side) return;
        _particleSystem.Play();
    }

    private void OnEnable()
    {
        PlayerFsm.OnPlayerEnterUpdraft += OnPlayerEnterUpdraft;
        PlayerFsm.OnPlayerEnteredSlideLateral += OnPlayerEnterSlideLateral;
    }

    private void OnDisable()
    {
        PlayerFsm.OnPlayerEnterUpdraft -= OnPlayerEnterUpdraft;
        PlayerFsm.OnPlayerEnteredSlideLateral -= OnPlayerEnterSlideLateral;
    }
}