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
                if (YVelocity > -5f)
                {
                    FMODUnity.RuntimeManager.PlayOneShotAttached(splashFmodEvent, gameObject);
                    return;
                };
                if (SwimRaycast(out var swimRaycastParam))
                {
                    _splashParticles.transform.up = GetUpVector();
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
        else
        {
            Debug.DrawRay(transform.position + GetUpVector() * 3f, GetUpVector() * 6f, Color.purple);
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
        return distance > 1f;
    }
    
    private bool IsSwimTriggerAtSurface(TriggerParams triggerParams)
    {
        if (triggerParams is not SwimRaycastParam SwimRaycastParam) return false;
        var distance = SwimRaycastParam.surfaceDistance;
        return distance < 1f;
    }

    public bool GetWaterDepth(out float depth, out Collider collider)
{
    var point = transform.position;
    var upDirection = GetUpVector().normalized;
    var radius = 1.5f;
    var waterTriggerLayer = LayerMask.GetMask("Water");

    depth = float.MinValue;
    collider = null;

    Collider[] overlappingColliders =
        Physics.OverlapSphere(point, radius, waterTriggerLayer, QueryTriggerInteraction.Collide);

    if (overlappingColliders.Length > 0)
    {
        foreach (Collider waterCollider in overlappingColliders)
        {
            // 1. Get accurate height range along the custom upDirection (fixes world-space AABB bounds bug)
            GetColliderHeightRange(waterCollider, upDirection, out float minHeight, out float maxHeight);
            float pointHeight = Vector3.Dot(point, upDirection);

            // If our point is vertically outside this specific collider's top or bottom along upDirection, ignore it.
            if (pointHeight > maxHeight || pointHeight < minHeight)
            {
                continue;
            }

            collider = waterCollider;
            Vector3 colCenter = waterCollider.bounds.center;
            float hCenter = Vector3.Dot(colCenter, upDirection);
            
            // 2. Cast down from safely above the collider along -upDirection to find the exact surface
            Vector3 safeTopOrigin = colCenter + upDirection * ((maxHeight - hCenter) + 5f);
            float rayDistance = (maxHeight - minHeight) + 10f;
            RaycastHit hit;

            if (waterCollider.Raycast(new Ray(safeTopOrigin, -upDirection), out hit, rayDistance))
            {
                depth = Vector3.Dot(hit.point - point, upDirection);
                return true;
            }
            
            // Fallback if surface raycast misses at the exact edge: calculate depth relative to maxHeight
            depth = maxHeight - pointHeight;
            return true;
        }
    }

    // 3. Point is NOT inside. Check if it is ABOVE a trigger using SphereCast along -upDirection.
    RaycastHit[] downHits = Physics.SphereCastAll(
        point + (upDirection * radius), 
        radius, 
        -upDirection, 
        Mathf.Infinity, 
        waterTriggerLayer,
        QueryTriggerInteraction.Collide
    );

    if (downHits.Length > 0)
    {
        System.Array.Sort(downHits, (a, b) => a.distance.CompareTo(b.distance));
        RaycastHit nearestHit = downHits[0];
        collider = nearestHit.collider;
        float distanceToSurface = Vector3.Dot(point - nearestHit.point, upDirection);
        depth = -distanceToSurface; // Negative depth means above the trigger

        if (Physics.Raycast(point + upDirection, -upDirection, out var envHit, Mathf.Infinity, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {
            if (envHit.distance - 1f < distanceToSurface) return false;
        }
        
        return true;
    }

    return false;
}

private void GetColliderHeightRange(Collider col, Vector3 upDir, out float minHeight, out float maxHeight)
{
    minHeight = float.MaxValue;
    maxHeight = float.MinValue;

    if (col is BoxCollider box)
    {
        Vector3 ext = box.size * 0.5f;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 localPt = new Vector3(ext.x * x, ext.y * y, ext.z * z);
                    Vector3 worldPt = box.transform.TransformPoint(localPt + box.center);
                    float h = Vector3.Dot(worldPt, upDir);
                    if (h < minHeight) minHeight = h;
                    if (h > maxHeight) maxHeight = h;
                }
            }
        }
    }
    else if (col is SphereCollider sphere)
    {
        float centerH = Vector3.Dot(sphere.transform.TransformPoint(sphere.center), upDir);
        minHeight = centerH - sphere.radius;
        maxHeight = centerH + sphere.radius;
    }
    else if (col is CapsuleCollider capsule)
    {
        Vector3 localDir = Vector3.zero;
        localDir[capsule.direction] = 1f;
        float halfHeight = capsule.height * 0.5f;
        Vector3 p1 = capsule.center + localDir * (halfHeight - capsule.radius);
        Vector3 p2 = capsule.center - localDir * (halfHeight - capsule.radius);
        Vector3 w1 = capsule.transform.TransformPoint(p1);
        Vector3 w2 = capsule.transform.TransformPoint(p2);
        
        float h1 = Vector3.Dot(w1, upDir);
        float h2 = Vector3.Dot(w2, upDir);
        minHeight = Mathf.Min(h1, h2) - capsule.radius;
        maxHeight = Mathf.Max(h1, h2) + capsule.radius;
    }
    else
    {
        // Fallback for MeshColliders: project world AABB bounds corners onto upDir
        Bounds b = col.bounds;
        Vector3 center = b.center;
        Vector3 ext = b.extents;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + new Vector3(ext.x * x, ext.y * y, ext.z * z);
                    float h = Vector3.Dot(corner, upDir);
                    if (h < minHeight) minHeight = h;
                    if (h > maxHeight) maxHeight = h;
                }
            }
        }
    }
}

}