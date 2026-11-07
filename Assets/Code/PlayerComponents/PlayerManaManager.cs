using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManaManager : MonoBehaviour
{

    public static PlayerManaManager Singleton;

    private int _maxMana;
    private int _currentMana;
    private Transform _flipHolder;
    private List<PlayerManaIndicator> _manaIndicators;

    private const float ReplenishCooldown = 0.5f;
    private float _replenishTimer = 0f;

    public static event Action OnPlayerMaxMana;
    public static event Action OnPlayerConsumedMana;
    private bool _isRestoring;

    private void Awake()
    {
        Singleton = this;
        _flipHolder = transform.Find("FlipHolder");
        _isRestoring = false;
    }

    public int GetCurrentAvailableMana()
    {
        return _currentMana;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetUpIndicators();
        _currentMana = _maxMana;
        transform.SetParent(null);
    }

    private void SetUpIndicators()
    {
        var count = 2 + GetBonusManaFromItems();
        _maxMana = count;
        var manaPrefab = Resources.Load("Prefab/PlayerManaIndicator") as GameObject;
        for (int i = _flipHolder.childCount - 1; i >= 0; i--)
        {
            Destroy(_flipHolder.GetChild(i).gameObject);
        }

        _manaIndicators = new List<PlayerManaIndicator>();
        float spacing = 0.65f;
        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(manaPrefab, _flipHolder);
            obj.transform.localPosition = new Vector3(-6.5f, (spacing * i) - (spacing * count * 0.5f) + 2f, 0);
            _manaIndicators.Add(obj.GetComponent<PlayerManaIndicator>());
        }
    }

    // Update is called once per frame
    void Update()
    {
        var angle = Vector3.SignedAngle(PlayerFsm.Singleton.transform.forward, Camera.main.transform.forward, Vector3.up);
        _flipHolder.transform.localScale = new Vector3(angle > 5f ? 1f : -1f, 1f, 1f); // bias to one side to prvent flickering when running straight away from/ towards camera
        if (_currentMana < _maxMana)
        {
            if (PlayerFsm.Singleton.Machine.IsInState(GravityFsm.GravityFsmState.Grounded)) _replenishTimer += Time.deltaTime;
        }
        
        // if (_replenishTimer >= ReplenishCooldown)
        // {
        //     _currentMana++;
        //     _manaIndicators[_currentMana - 1].Replenish();
        //     _replenishTimer = 0;
        //     
        //     if (_currentMana == _maxMana) OnPlayerMaxMana?.Invoke();
        // }

        var xzLerp = 30f;
        var yLerp = 8f;

        var playerPos = PlayerFsm.Singleton.transform.position;
        var pos = new Vector3(
            Mathf.Lerp(transform.position.x, playerPos.x, xzLerp * Time.deltaTime),
            Mathf.Lerp(transform.position.y, playerPos.y, yLerp * Time.deltaTime),
            Mathf.Lerp(transform.position.z, playerPos.z, xzLerp * Time.deltaTime));
        transform.position = pos;
    }

    public void Consume()
    {
        if (_currentMana < 1) return;
        _isRestoring = false;
        OnPlayerConsumedMana?.Invoke();
        _manaIndicators[_currentMana - 1].Consume();
        _replenishTimer = 0f;
        _currentMana--;
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
        SetUpIndicators();
    }

    private int GetBonusManaFromItems()
    {
        var items = SaveSystem.LoadCachedSaveData().items;
        int count = 0;
        foreach (var item in items)
        {
            if (item.Contains("Mana")) count++;
        }

        return count;
    }

    public void RestoreAll()
    {
        if (PlayerFsm.Singleton.GetIsTinsicaCoyoteStatus()) return;
        if (_currentMana == _maxMana) return;
        if (_isRestoring) return;

        StartCoroutine(Coroutine());
        IEnumerator Coroutine()
        {
            _isRestoring = true;

            for (int i = _currentMana; i < _maxMana; i++)
            {
                if (!_isRestoring) yield break;
                _manaIndicators[i].Replenish();
                _currentMana++;
                yield return new WaitForSeconds(0.5f);
            }

            if (!_isRestoring) yield break;
            _currentMana = _maxMana;
            OnPlayerMaxMana?.Invoke();
            _isRestoring = false;
        }

    }
}
