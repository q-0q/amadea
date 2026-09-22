using System;
using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    public float maximumHeight = 20f;
    public const float MaximumVelocity = 10f;

    public GameObject BaseConsole;
    public GameObject PlatformConsole;
    public Transform PlatformParent;
    private Vector3 loweredPlatformPosition;

    private Interactable baseInteractable;
    private Interactable platformInteractable;

    private bool rising = false;
    private bool falling = false;

    public List<string> upPositionIds;


    private void Awake()
    {
        baseInteractable = BaseConsole.GetComponentInChildren<Interactable>();
        platformInteractable = PlatformConsole.GetComponentInChildren<Interactable>();
        loweredPlatformPosition = PlatformParent.position;
        
        BaseConsole.SetActive(false);
        PlatformConsole.SetActive(true);

        var saveData = SaveSystem.LoadCachedSaveData();
        var positionId = saveData.playerInGamePositionId;
        foreach (var id in upPositionIds)
        {
            if (id == positionId)
            {
                PlatformParent.position = loweredPlatformPosition + Vector3.up * maximumHeight;
                BaseConsole.SetActive(true);
                PlatformConsole.SetActive(false);
                return;
            }
        }
    }

    private void OnEnable()
    {
        baseInteractable.OnInteracted += OnBaseInteracted;
        platformInteractable.OnInteracted += OnPlatformInteracted;
    }

    

    private void OnDisable()
    {
        baseInteractable.OnInteracted -= OnBaseInteracted;
        platformInteractable.OnInteracted -= OnPlatformInteracted;
    }
    
    
    
    private void OnPlatformInteracted()
    {
        BaseConsole.SetActive(true);
        PlatformConsole.SetActive(false);
        rising = true;
        falling = false;
    }

    private void OnBaseInteracted()
    {
        BaseConsole.SetActive(false);
        PlatformConsole.SetActive(true);
        
        rising = false;
        falling = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!rising && !falling) return;
        var destination = rising ? loweredPlatformPosition + Vector3.up * maximumHeight : loweredPlatformPosition;
        var delta = PlatformParent.position - destination;
        var speed = Mathf.Lerp(0f, MaximumVelocity, Mathf.InverseLerp(0f, 5f, delta.magnitude));
        
        var newPos = Vector3.MoveTowards(PlatformParent.position, destination, Time.deltaTime * speed);
        if (delta.magnitude < 0.05f)
        {
            rising = false;
            falling = false;
            PlatformParent.position = destination;
        }
        else
        {
            PlatformParent.position = newPos;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var center = PlatformParent.position + Vector3.up * (maximumHeight - 2.5f);
        Gizmos.DrawCube(center, new Vector3(50f, 5f, 50f));
    }
}
