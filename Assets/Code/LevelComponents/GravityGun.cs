using System;
using System.Collections;
using UnityEngine;

public class GravityGun : MonoBehaviour
{
    private Interactable _interactable;
    private bool _active = false;
    private GameObject _cursorObject;
    public static event Action OnGravityGunUsed;
    private Vector3 _storedCursorPosition;

    private void Awake()
    {
        _interactable = GetComponentInChildren<Interactable>();
        _cursorObject = transform.Find("Cursor").gameObject;
        _cursorObject.SetActive(false);
    }

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
    }

    private void OnInteracted()
    {

        if (_active)
        {
            OnGravityGunUsed?.Invoke();
            PlayerFsm.Singleton.GravityGunDestination = _storedCursorPosition;
            PlayerFsm.Singleton.SetUpVector(_cursorObject.transform.up);
            PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.GravityGunTravel);
            StartCoroutine(CursorCoroutine());
        }
        else
        {
            PlayerFsm.Singleton.currentInteractable = _interactable;
            PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.GenericInteract);
            _cursorObject.SetActive(true);
        }
        
        _active = !_active;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        if (!_active) return;

        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, 150f, Fsm.GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {
            _storedCursorPosition = hit.point;
            _cursorObject.transform.position = hit.point + (hit.normal.normalized * 3f);
            _cursorObject.transform.up = hit.normal;
        }
    }

    private IEnumerator CursorCoroutine()
    {
        yield return new WaitForSeconds(0.75f);
        _cursorObject.SetActive(false);
    }
}
