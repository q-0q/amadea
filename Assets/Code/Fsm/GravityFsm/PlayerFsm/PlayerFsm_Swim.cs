using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMOD.Studio;
using UnityEditor;
using UnityEngine;
using Wasp;

public partial class PlayerFsm
{

    private void SwimOnUpdate()
    {
        if (_playerInput.actions["Sprint"].IsPressed()) isSprinting = true;
        
        HandleInputMomentumChange();
        HandleTurning(1f, false, 1f, false, isSprinting ? 0.5f : 1f);
        
        SetAnimatorMomentum();
        SetAnimatorSpeedMod();

        if (!SwimRaycast(out var swimRaycastParam)) return;
        if (swimRaycastParam.WaterHazardType != WaterHazardType.Type.Freeze)
        {
            _freezeTimer = 0;
            freezeFmodInstance.stop(STOP_MODE.ALLOWFADEOUT);
            return;
        }

        if (_freezeTimer < 0.01f) freezeFmodInstance.start(); // kinda a hack but w/e
        _freezeTimer += Time.deltaTime;
        if (_freezeTimer >= SwimFreezeDuration) InvokePlayerDeath();
    }
    
    
    private void SwimSurfaceRiseOnUpdate()
    {
        var desiredYVelocity = 25f;
        
        if (SwimRaycast(out var swimRaycastParam))
        {
            desiredYVelocity = Mathf.Lerp(25f, 0f, Mathf.InverseLerp(4f, -0.80f, swimRaycastParam.surfaceDistance));

            
            
            if (swimRaycastParam.WaterHazardType == WaterHazardType.Type.InstantDrown)
            {
                _momentum = Mathf.Lerp(_momentum, 0,
                    Time.deltaTime * Mathf.Lerp(2f, 10f, Mathf.InverseLerp(0, 0.5f, TimeInCurrentState())));
            };
        }
        
        else
        {
            Machine.Jump(PlayerFsm.PlayerFsmState.Fall);
        }
        
        YVelocity = Mathf.Lerp(YVelocity, desiredYVelocity,
            Time.deltaTime * Mathf.Lerp(1f, 20f, Mathf.InverseLerp(0, 0.45f, TimeInCurrentState())));
        HandleCollisionMove(Mathf.Lerp(0.1925f, 0.0925f * 0.75f, Mathf.InverseLerp(0, 0.4f, TimeInCurrentState())));
    }
    
    
    private void DrownOnUpdate()
    {
        if (!_swimSurfaceRippleQueued) StartCoroutine(SwimSurfaceRippleCoroutine());
        transform.position += GetDownVector() * (Time.deltaTime * 0.5f);
    }
    
    private void SwimSurfaceOnUpdate()
    {
        YVelocity = 0f;
        
        if (SwimRaycast(out var swimRaycastParam))
        {


            transform.position += ComputeCollisionMove(((swimRaycastParam.point + GetUpVector() * -0.80f) - transform.position) * (Time.deltaTime * 15f));
                
            if (swimRaycastParam.WaterHazardType == WaterHazardType.Type.InstantDrown)
            {
                _momentum = Mathf.Lerp(_momentum, 0,
                    Time.deltaTime * Mathf.Lerp(2f, 10f, Mathf.InverseLerp(0, 0.5f, TimeInCurrentState())));
            };
            
        }

        else
        {
            Machine.Jump(PlayerFsm.PlayerFsmState.Fall);
            return;
        }

        if (!PhotoManager.Singleton.IsActive()) OnPlayerWakeGenerated?.Invoke(GetRipplePosition(), Mathf.Lerp(0.075f, 0.075f, ComputeMomentumWeight()), Mathf.Lerp(0.0011f, 0.0009f, ComputeMomentumWeight()));
        if (!_swimSurfaceRippleQueued) StartCoroutine(SwimSurfaceRippleCoroutine());
        HandleCollisionMove(0.0925f * 0.75f);
    }

    private IEnumerator SwimSurfaceRippleCoroutine()
    {
        _swimSurfaceRippleQueued = true;
        OnPlayerRippleGenerated?.Invoke(GetRipplePosition(), 0.5f, 0.001f);
        yield return new WaitForSeconds(Mathf.Lerp(SwimSurfaceRippleTimer * 7f, SwimSurfaceRippleTimer, ComputeMomentumWeight()));
        _swimSurfaceRippleQueued = false;
    }

    private Vector3 GetRipplePosition()
    {
        return transform.position + transform.forward * Mathf.Lerp(0f, 1f, ComputeMomentumWeight());
    }

    private void SwimConfigure()
    {
        Machine.Configure(PlayerFsmState.Swim)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)

            .SubstateOf(GravityFsmState.DontLoseYVelocity)
            .OnEntry(_ =>
            {
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;
                _currentFlankType = FlankType.None;
                currentRopeSwing = null;
            });
        
        Machine.Configure(PlayerFsmState.SwimSurfaceRise)
            .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.SwimSurface, @params => IsSwimTriggerAtSurface(@params) && YVelocity > -2f)
            .SubstateOf(PlayerFsmState.Swim)
            // .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.Drown, @params =>
            // {
            //     if (TimeInCurrentState() < 0.75f) return false;
            //     if (@params is not SwimRaycastParam SwimRaycastParam) return false;
            //     return SwimRaycastParam.drown;
            // }, 2)
            .OnEntry(_ =>
            {
                if (YVelocity > -5f) return;
                if (SwimRaycast(out var swimRaycastParam))
                {
                    _splashParticles.transform.position = swimRaycastParam.point;
                    _splashParticles.Play();
                    FMODUnity.RuntimeManager.PlayOneShotAttached(splashFmodEvent, _splashParticles.gameObject);
                }
                OnPlayerRippleGenerated?.Invoke(transform.position, 1.0f, 0.005f);
                OnPlayerWakeGenerated?.Invoke(transform.position, 1.0f, 0.0025f);
            });
        
        Machine.Configure(PlayerFsmState.SwimSurface)
            .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.Drown, @params =>
            {
                if (_momentum > 7f) return false;
                if (@params is not SwimRaycastParam SwimRaycastParam) return false;
                return SwimRaycastParam.WaterHazardType == WaterHazardType.Type.InstantDrown;
            },3)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.Vault, CanVault, 1)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.MediumVaultHang, _ => !Machine.IsInState(PlayerFsmState.PitonFlip) || YVelocity < PitonMaximumWallInteractYVelocity)
            .PermitIf(PlayerFsmTrigger.FaceWall, PlayerFsmState.Wallsquat,
                _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            .PermitIf(PlayerFsmTrigger.FaceWallStrict, PlayerFsmState.Wallsquat,
                _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            .PermitIf(PlayerFsmTrigger.FaceHighLedge, PlayerFsmState.Wallsquat,
                _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            .OnEntry(_ =>
            {
                YVelocity = 0;
            })
            .SubstateOf(PlayerFsmState.Swim);

        Machine.Configure(PlayerFsmState.DiveFall)
            .SubstateOf(PlayerFsmState.Fall);

        Machine.Configure(PlayerFsmState.Drown)
            .OnEntry(_ =>
            {
                EndSurge();
            })
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Dying);
    }
    
        private void FireSwimTrigger()
    {
        if (SwimRaycast(out var swimRaycastParam))
        {
            Machine.Fire(PlayerFsmTrigger.SwimTriggerRaycastHit, swimRaycastParam);
        }
    }

    private bool SwimRaycast(out SwimRaycastParam swimRaycastParam)
    {

        swimRaycastParam = new SwimRaycastParam();
        WaterHazardType.Type HazardHelper(Vector3 position, GameObject waterObject)
        {
            foreach (var dualWaterPoint in DualWaterPointRegistry.DualWaterPoints)
            {
                if (Vector3.Distance(position, dualWaterPoint.transform.position) < dualWaterPoint.Radius)
                {
                    return WaterHazardType.Type.None;
                };
            }

            var hazard = waterObject.GetComponent<WaterHazardType>();
            if (hazard == null)
            {
                return WaterHazardType.Type.None;
            };
            if (hazard.type == WaterHazardType.Type.InstantDrown) return WaterHazardType.Type.InstantDrown;
            if (hazard.type == WaterHazardType.Type.Freeze) return WaterHazardType.Type.Freeze;

            return WaterHazardType.Type.None;
        }
        
        swimRaycastParam.WaterHazardType = WaterHazardType.Type.None;


        if (GetWaterDepth(out var depth, out var collider))
        {
            swimRaycastParam.WaterHazardType = HazardHelper(transform.position, collider.gameObject);
            swimRaycastParam.surfaceDistance = depth;
            swimRaycastParam.point = transform.position + (transform.up * depth);
            
            Debug.DrawRay(swimRaycastParam.point, GetUpVector() * 3f, Color.orange);
            return true;
        }

        return false;
    }

    public float GetFreezeWeight()
    {
        return _freezeTimer / SwimFreezeDuration;
    }


    private bool IsSwimTrigger(TriggerParams triggerParams)
    {
        if (triggerParams is not SwimRaycastParam SwimRaycastParam) return false;
        var distance = SwimRaycastParam.surfaceDistance;
        return distance > 3f;
    }
    
    private bool IsSwimTriggerAtSurface(TriggerParams triggerParams)
    {
        if (triggerParams is not SwimRaycastParam SwimRaycastParam) return false;
        var distance = SwimRaycastParam.surfaceDistance;
        return distance < 3f;
    }

    public bool GetWaterDepth(out float depth, out Collider collider)
    {

        var point = transform.position;
        var upDirection = GetUpVector();
        var radius = 3f;
        var waterTriggerLayer = LayerMask.GetMask("Water");

        depth = float.MinValue;

        Collider[] overlappingColliders =
            Physics.OverlapSphere(point, radius, waterTriggerLayer, QueryTriggerInteraction.Collide);

        if (overlappingColliders.Length > 0)
        {
            // Filter out side-entry false positives. 
            // An OverlapSphere can touch a side wall even if the point is above the water's surface.
            foreach (Collider waterCollider in overlappingColliders)
            {
                Bounds bounds = waterCollider.bounds;
                float pointHeight = Vector3.Dot(point, upDirection);
                float topHeight = Vector3.Dot(bounds.max, upDirection);
                float bottomHeight = Vector3.Dot(bounds.min, upDirection);

                // If our point is vertically outside this specific collider's top or bottom, 
                // it's a side-entry illusion—ignore it and check below.
                if (pointHeight > topHeight || pointHeight < bottomHeight)
                {
                    continue;
                }

                // Genuinely inside this volume. Cast against this specific collider to find the surface.
                Vector3 safeTopOrigin = point + (upDirection * (bounds.extents.magnitude + 10f));
                RaycastHit hit;
                collider = waterCollider;

                if (waterCollider.Raycast(new Ray(safeTopOrigin, -upDirection), out hit, bounds.extents.magnitude * 4f))
                {
                    depth = Vector3.Dot(hit.point - point, upDirection);
                    return true;
                }
                
                depth = 0f; // Fallback if surface raycast is at the exact edge
                return false;
            }
        }

        // 2. Point is NOT inside (or it was a side-entry). Check if it is ABOVE a trigger.
        // SphereCastAll casts a thick ray with your radius and returns all hits.
        
        
        
        
        
        RaycastHit[] downHits = Physics.SphereCastAll(point + (upDirection * radius), radius, -upDirection, Mathf.Infinity, waterTriggerLayer,
            QueryTriggerInteraction.Collide);

        if (downHits.Length > 0)
        {
            // Sort hits by distance so the closest surface immediately below is evaluated first
            System.Array.Sort(downHits, (a, b) => a.distance.CompareTo(b.distance));
            RaycastHit nearestHit = downHits[0];
            collider = nearestHit.collider;
            float distanceToSurface = Vector3.Dot(point - nearestHit.point, upDirection);
            depth = -distanceToSurface; // Negative depth means above the trigger


            if (Physics.Raycast(point + (upDirection.normalized), -upDirection, out var envHit, Mathf.Infinity, GetEnvironmentalLayermask(),
                    QueryTriggerInteraction.Ignore))
            {
                print(envHit.distance + ", " + distanceToSurface);
                if (envHit.distance - 1f < distanceToSurface) return false;
            }
            
            return true;
        }

        collider = null;
        return false;
    }

}