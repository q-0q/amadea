using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ColorPuzzleStation : MonoBehaviour
{

    private Interactable _interactable;
    private bool _active;
    private string _baseInteractableText;
    private CustomPointLight _light;

    public static readonly Color Red = Color.salmon;
    public static readonly Color Blue = Color.turquoise;
    public static readonly Color Yellow = Color.yellow;
    public static readonly Color Green = Color.paleGreen;
    public static readonly Color Purple = Color.plum;

    private int _currentColorIndex;

    private Renderer _curvedStar;
    private float _timeSinceColorChanged;
    private const float ColorChangeTime = 0.75f;

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
}
