using System;
using UnityEngine;

public class PlayerLantern : MonoBehaviour
{
    private GameObject _child;
    private bool _playerHasLantern;
    public Transform parentMotion;

    private void Awake()
    {
        _child = transform.Find("Child").gameObject;
        _child.SetActive(false);
        _playerHasLantern = SaveSystem.GetAllItems().Contains("Lantern");
    }

    private void OnEnable()
    {
        SaveSystem.OnSaveDataUpdated += OnSaveDataUpdated;
    }
    
    private void OnDisable()
    {
        SaveSystem.OnSaveDataUpdated -= OnSaveDataUpdated;
    }

    private void OnSaveDataUpdated(SaveSystem.SaveData obj)
    {
        _playerHasLantern = SaveSystem.GetAllItems().Contains("Lantern");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.position = PlayerFsm.Singleton.transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!_playerHasLantern) return;
        // float rotationSpeed = 100f;

        var playerPosition = PlayerFsm.Singleton.transform.position;
        // var playerPositionDelta = playerPosition - _previousPlayerPosition;
        
        // _currentSpeed = Vector3.Lerp(_currentSpeed, playerPositionDelta, Time.deltaTime * 4f);
        
        transform.position = parentMotion.transform.position;
        transform.rotation = parentMotion.transform.rotation;
        // transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // transform.position += _currentSpeed;
        
        _child.SetActive(Shader.GetGlobalFloat("_CustomDarknessWeight") > 0.75f);


    }
}
