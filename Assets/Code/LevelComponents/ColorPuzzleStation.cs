using System;
using System.Collections;
using System.Collections.Generic;
using Code.Misc;
using DG.Tweening;
using UnityEngine;

public class ColorPuzzleStation : MonoBehaviour
{


    private Interactable _interactable;
    private bool _active;
    private bool _complete;
    private string _baseInteractableText;
    private CustomPointLight _light;

    public static readonly Color Red = Color.red;
    public static readonly Color Blue = Color.lightBlue;
    public static readonly Color Yellow = Color.yellow;
    public static readonly Color Green = Color.green;
    public static readonly Color Purple = Color.purple;

    private int _currentColorIndex;

    private Renderer _curvedStar;
    private float _timeSinceColorChanged;
    private const float ColorChangeTime = 0.75f;

    public ColorPuzzle owner;
    

    private static Dictionary<int, Color> colorLookup = new Dictionary<int, Color>()
    {
        [0] = Red,
        [1] = Blue,
        [2] = Yellow,
        [3] = Green,
        [4] = Purple,
    };
    
    private void Awake()
    {
        _active = false;
        _interactable = GetComponentInChildren<Interactable>();
        _light = GetComponentInChildren<CustomPointLight>();
        _baseInteractableText = _interactable.text;
        _currentColorIndex = 0;
        _curvedStar = transform.Find("Fx").Find("CurvedStar").GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
        _interactable.OnHardInteracted += OnHardInteracted;
    }

    private void OnHardInteracted()
    {
        _timeSinceColorChanged = 0;
        _active = true;
        IncrementColor();
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
        _interactable.OnHardInteracted -= OnHardInteracted;
    }

    private void OnInteracted()
    {
        if (_active)
        {
            PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.Idle);
            _active = false;
            _interactable.text = _baseInteractableText;
            owner.OnStationColorUpdated();
            return;
        }
        
        
        PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.WalkToGenericInteractable);
        PlayerFsm.Singleton.walkToPositionTarget = _interactable.transform.position;
        PlayerFsm.Singleton.walkToPositionArrivalDistanceModifier = _interactable.arrivalDistanceModifier;
        
    }
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        if (_complete) return;
        
        if (_active && PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.GenericInteract))
        {
            _interactable.text = "Leave";
            _timeSinceColorChanged += Time.deltaTime;
            if (_timeSinceColorChanged > ColorChangeTime)
            {
                IncrementColor();
            }
        }

        _curvedStar.material.color = colorLookup[_currentColorIndex];
        _light.Color = colorLookup[_currentColorIndex] * 0.5f;
    }

    private void IncrementColor()
    {
        _currentColorIndex++;
        _curvedStar.transform.DOComplete();
        _curvedStar.transform.DOPunchPosition(Vector3.down * 0.1f, 0.15f, 20, 1f);
        _timeSinceColorChanged = 0;
        if (_currentColorIndex == 5) _currentColorIndex = 0;
    }

    public void MakeComplete()
    {
        StartCoroutine(Coroutine());
        

        IEnumerator Coroutine()
        {
            _complete = true;
            _interactable.SetEnabled(false);
            _curvedStar.transform.DOComplete();
            _curvedStar.transform.DOPunchPosition(Vector3.down * 0.15f, 0.75f, 20, 1f);
            yield return new WaitForSeconds(1.25f);
            Util.InvokeSphereEffect(_curvedStar.transform.position - Vector3.up, Vector3.one * 3f, 1.25f, 0.8f, -0.5f);
            _curvedStar.material.color = Color.white;
            _light.Color = Color.white * 0.5f;
        }
    }

    public int GetCurrentColorIndex()
    {
        return _currentColorIndex;
    }
}
