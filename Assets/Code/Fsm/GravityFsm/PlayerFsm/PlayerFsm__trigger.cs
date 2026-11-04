using System;
using Code.TriggerParams;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerFsm
{
    

    public override void OnFireTriggers()
    {
        
        base.OnFireTriggers();
        
        if (_inputBuffer.IsBuffered("Jump"))
        {
            if (!CutsceneManager.Singleton.IsCutsceneJumpDisabled()) Machine.Fire(PlayerFsmTrigger.Jump);
        }
        
        if (_inputBuffer.IsBuffered("Dash"))
        {
            Machine.Fire(PlayerFsmTrigger.Dash);
        }
        
        
        if (_inputBuffer.IsBuffered("Attack"))
        {
            Machine.Fire(PlayerFsmTrigger.Attack);
        }
        
        if (_inputBuffer.IsBuffered("Inventory"))
        {
            if (SaveSystem.GetAllItems().Count > 0) Machine.Fire(PlayerFsmTrigger.Inventory);
        }
        
        if (_inputBuffer.IsBuffered("Map"))
        {
            if (SaveSystem.GetAllItems().Contains("Map")) Machine.Fire(PlayerFsmTrigger.Map);
        }
        
        if (_playerInput.actions["Photo"].WasPressedThisFrame() && (SaveSystem.GetAllItems().Contains("Camera")))
        {
            if (PhotoManager.Singleton.IsActive()) PhotoManager.Singleton.MakeInactive();
            else PhotoManager.Singleton.MakeActive();
        }
        
        if (_inputBuffer.IsBuffered("Trick"))
        {
            
            Machine.Fire(PlayerFsmTrigger.Trick);
        }
        
        var v3 = GetInputMovementVector3();
        var angle = Vector3.Angle(v3.normalized, transform.forward.normalized);
        if (angle > HardTurnMinimumAngle && !PhotoManager.Singleton.IsActive())
        {
            Machine.Fire(PlayerFsmTrigger.HardTurn);
        }

        var signedAngle = Vector3.SignedAngle(v3.normalized, transform.forward.normalized, GetUpVector());
        if (signedAngle > 70f)
        {
            Machine.Fire(PlayerFsmTrigger.SoftTurnLeft);
        } else if (signedAngle < -70f)
        {
            Machine.Fire(PlayerFsmTrigger.SoftTurnRight);
        }

        if (_momentum < NoMomentumThreshold)
        {
            Machine.Fire(PlayerFsmTrigger.NoMomentum);
        }


        var walkToPositionTargetDistance = Vector3.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(walkToPositionTarget.x, walkToPositionTarget.z));
        if (walkToPositionTargetDistance < ArriveAtWalkPositionTargetDistance * walkToPositionArrivalDistanceModifier)
        {
            Machine.Fire(PlayerFsmTrigger.ArriveAtWalkToPositionTarget);
            Machine.Fire(PlayerFsmTrigger.ArriveAtWalkToPositionTargetRanged);
        } else if (walkToPositionTargetDistance < ArriveAtWalkPositionTargetRangedDistance * walkToPositionArrivalDistanceModifier)
        {
            Machine.Fire(PlayerFsmTrigger.ArriveAtWalkToPositionTargetRanged);
        } 

        FireFaceTriggersRedux();
        var flank = FireFlankTriggers();

        if (IsInGust)
        {
            Machine.Fire(PlayerFsmTrigger.StartUpdraft);
        }
        else if (!IsInGust)
        {
            Machine.Fire(PlayerFsmTrigger.EndUpdraft);
        }

        foreach (var neighbor in Physics.OverlapSphere(transform.position, 2.75f, LayerMask.GetMask("Piton"), QueryTriggerInteraction.Collide))
        {
            var yDelta = neighbor.transform.position.y - transform.position.y;
            if (yDelta > 4f) continue;
            if (yDelta < -2f) continue;
            
            if (YVelocity < 0 && Physics.Raycast(transform.position, GetDownVector(), 2f * GetRaycastTimeModifier(), GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore)) continue;
            var param = new PitonParam() { Piton = neighbor.transform.parent};
            Machine.Fire(PlayerFsmTrigger.EnterPitonTrigger, param);
        }

        FireSwimTrigger();

        if (YVelocity < 15f && _momentum > 12f && !flank)
        {
            if (Physics.Raycast(transform.position, GetDownVector() + transform.forward, out var hit, 55f, ~LayerMask.GetMask("PlayerClothCollider", "PlayerCloth", "Player", "FoliageSystems")))
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Water")) Machine.Fire(PlayerFsmTrigger.IsAboveWater);
            }
        }
        
        
        foreach (var neighbor in Physics.OverlapSphere(transform.position, 7f, LayerMask.GetMask("RopeSwing"), QueryTriggerInteraction.Collide))
        {
            if (YVelocity < 0 && Physics.Raycast(transform.position, GetDownVector(), 2f * GetRaycastTimeModifier(), GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore)) continue;
            var ropeSwing = neighbor.transform.parent.parent.GetComponent<RopeSwing>();
            var pos = ropeSwing.GetWorldspaceAttachPoint(transform.position);
            var relativePos = transform.InverseTransformPoint(pos);

            var maxZ = Mathf.Lerp(2f, 6f, Mathf.InverseLerp(0.3f, 0.7f, ComputeMomentumWeight()));
            if (relativePos.z < -1f) continue;
            if (relativePos.z > maxZ) continue;
            if (Mathf.Abs(relativePos.x) > 3f) continue;
            if (Mathf.Abs(relativePos.y) > 3f) continue;
            
            var param = new RopeSwingHitParam() { RopeSwing = ropeSwing};
            Machine.Fire(PlayerFsmTrigger.EnterRopeSwingTrigger, param);
        }
        
        if (PressRaycast(out _)) Machine.Fire(PlayerFsmTrigger.Press);
        
        if (!Physics.Raycast(transform.position + transform.forward * (GetRaycastTimeModifier() * Mathf.Lerp(0, 6f, ComputeMomentumWeight())), GetDownVector(), out var hit2, 25f, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {
            Machine.Fire(PlayerFsmTrigger.IsAboveLongFall);
        }
        
        // foreach (var neighbor in Physics.OverlapSphere(transform.position, 2.75f, LayerMask.GetMask("MinorLeylineTrigger"), QueryTriggerInteraction.Collide))
        // {
        //     _currentMinorLeyline = neighbor.transform.parent.parent.GetComponent<MinorLeyline>();
        //     _currentMinorLeylineTrigger = neighbor.transform;
        //     Machine.Fire(PlayerFsmTrigger.MinorLeylineTrigger);
        // }
    }

    private bool PressRaycast(out RaycastHit hit)
    {
        var b = (Physics.Raycast(transform.position + (GetUpVector() * FaceHighLedgeHeight) - (transform.forward),
            transform.forward, out hit,
            8f, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore));

        if (!b) return false;
        var slope = Vector3.Angle(hit.normal, GetUpVector());
        // if (slope < 70f) return false;
        var angle = Vector3.SignedAngle(-Flatten(hit.normal), transform.forward, GetUpVector());
        var angleWeight = Mathf.InverseLerp(0, 40f, Mathf.Abs(angle));
        var distanceThreshhold = Mathf.Lerp(2f, 2.25f + (Machine.IsInState(PlayerFsmState.Press) ? 4f : 0f), angleWeight);
        return hit.distance < distanceThreshhold;
    }

    private bool ProbeWallAboveLedge()
    {
        return false;
        var origin = transform.position + GetUpVector() * (FaceWallHeight + GetCurrentDashRaycastHeightOffset());
        if (!Physics.Raycast(origin,
                transform.forward,
                out var hit, 0.4f, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore)) return false;
        var slope = Vector3.Angle(hit.normal, GetUpVector());
        return slope > 60f;
    }
    
    Vector3 Flatten(Vector3 vector3)
    {
        return new Vector3(vector3.x, 0f, vector3.z);
    }
    
    private void FireFaceTriggersRedux()
{
    var forwardRaycastDistance = ComputeDynamicForwardRaycastDistance() + 1f;
    Vector3 up = GetUpVector();
    Vector3 down = -up; // Or GetDownVector() if implemented to match your custom up

    // Helper function to flatten vectors relative to the arbitrary up vector
    Vector3 Flatten(Vector3 v) => Vector3.ProjectOnPlane(v, up);

    bool ProbeDownwardsLedge(Vector3 wallPoint)
    {
        var v = 2f;
        var h = 0.6f;
        var origin = wallPoint + (up * v) + (transform.forward * h);
        
        if (!Physics.Raycast(origin, down,
                out var hit, v * 2f, GetEnvironmentalLayermask(),
                QueryTriggerInteraction.Ignore)) return false;

        var angle = Vector3.Angle(up, hit.normal);
        if (angle > 60f) return false;
        
        return true;
    }
    
    bool ProbeWall()
    {
        var origin = transform.position + up * (FaceWallHeight + GetCurrentDashRaycastHeightOffset());
        
        if (!Physics.Raycast(
                origin,
                transform.forward,
                out var hit, forwardRaycastDistance, GetEnvironmentalLayermask(),
                QueryTriggerInteraction.Ignore)) return false;

        if (Vector3.Angle(-Flatten(hit.normal), transform.forward) > FaceWallMaximumAngle) return false;
        
        var slope = Vector3.Angle(hit.normal, up);
        if (slope < 60f) return false;
        
        Machine.Fire(Vector3.Angle(-Flatten(hit.normal), transform.forward) < FaceWallStrictMaximumAngle
            ? PlayerFsmTrigger.FaceWallStrict
            : PlayerFsmTrigger.FaceWall, new RaycastHitParam() { Hit = hit});
        
        return true;
    }
    
    bool ProbeHighLedge()
    {
        var origin = transform.position + up * (FaceHighLedgeHeight + GetCurrentDashRaycastHeightOffset());
        
        if (!Physics.Raycast(origin, transform.forward,
                out var hit, forwardRaycastDistance, GetEnvironmentalLayermask(),
                QueryTriggerInteraction.Ignore)) return false;
        
        if (Vector3.Angle(-Flatten(hit.normal), transform.forward) > FaceWallMaximumAngle) return false;
        if (ProbeWallAboveLedge()) return false;
        if (!ProbeDownwardsLedge(hit.point)) return false;
        
        var slope = Vector3.Angle(hit.normal, up);
        if (slope < 60f) return false;
        
        Machine.Fire(PlayerFsmTrigger.FaceHighLedge, new RaycastHitParam() { Hit = hit});
        return true;
    }
    
    bool ProbeLedge()
    {
        var origin = transform.position + up * (FaceLedgeHeight + GetCurrentDashRaycastHeightOffset());
        
        if (!Physics.Raycast(origin, transform.forward,
                out var hit, forwardRaycastDistance, GetEnvironmentalLayermask(),
                QueryTriggerInteraction.Ignore)) return false;
        
        if (Vector3.Angle(-Flatten(hit.normal), transform.forward) > FaceWallMaximumAngle) return false;
        if (ProbeWallAboveLedge()) return false;
        if (!ProbeDownwardsLedge(hit.point)) return false;
        
        var slope = Vector3.Angle(hit.normal, up);
        if (slope < 60f) return false;
        
        Machine.Fire(PlayerFsmTrigger.FaceLedge, new RaycastHitParam() { Hit = hit});
        return true;
    }
    
    if (ProbeWall()) return;
    if (ProbeHighLedge()) return;
    if (ProbeLedge()) return;
    
    Machine.Fire(PlayerFsmTrigger.FaceOpen);
}

    private void FireFaceTriggers()
    {
        // todo: rewrite this method
        
        // reverse the order of checks and use nested ifs to propogate logic
        // that way we can fall back, ie if a more specific wall check fails we can 
        // fall back onto ledge
        
        var forwardRaycastDistance = ComputeDynamicForwardRaycastDistance();
        var skew = FaceRaycastSkew * GetRaycastTimeModifier();
        var minSlope = 80f;
        if (Physics.Raycast(transform.position + GetUpVector() * (FaceWallHeight + GetCurrentDashRaycastHeightOffset()), transform.forward, 
                out var hit, forwardRaycastDistance + skew * 2f, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore) && Vector3.Angle(-hit.normal, transform.forward) < FaceWallMaximumAngle)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ForceSlide")) return;
            var slope = Vector3.Angle(hit.normal, GetUpVector());
            
            // right now if this if fails, we arent able to fall back on other ledge types at this point
            if (slope > minSlope - 30f) Machine.Fire(Vector3.Angle(-hit.normal, transform.forward) < FaceWallStrictMaximumAngle
                ? PlayerFsmTrigger.FaceWallStrict
                : PlayerFsmTrigger.FaceWall, new RaycastHitParam() { Hit = hit});
        } else if (Physics.Raycast(transform.position + GetUpVector() *
                       (FaceHighLedgeHeight + GetCurrentDashRaycastHeightOffset()), transform.forward, 
                       out hit, forwardRaycastDistance + skew, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore) && Vector3.Angle(-hit.normal, transform.forward) < FaceWallMaximumAngle)
        {
            if (ProbeWallAboveLedge()) return;
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ForceSlide")) return;
            var slope = Vector3.Angle(hit.normal, GetUpVector());
            // same here
            if (slope > minSlope) Machine.Fire(PlayerFsmTrigger.FaceHighLedge, new RaycastHitParam() { Hit = hit});
        } else if (Physics.Raycast(transform.position + GetUpVector() * FaceLedgeHeight, transform.forward,
                       out hit, forwardRaycastDistance, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore) && Vector3.Angle(-hit.normal, transform.forward) < FaceWallMaximumAngle)
        {
            if (ProbeWallAboveLedge()) return;
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ForceSlide")) return;
            var slope = Vector3.Angle(hit.normal, GetUpVector());
            // and even here for face open
            if (slope > minSlope) Machine.Fire(PlayerFsmTrigger.FaceLedge, new RaycastHitParam() { Hit = hit});
        }
        else
        {
            Machine.Fire(PlayerFsmTrigger.FaceOpen);
        }
        
        Debug.DrawRay(transform.position + GetUpVector() * (FaceWallHeight  + GetCurrentDashRaycastHeightOffset()), transform.forward *
            (forwardRaycastDistance + skew * 2f), Color.red);
        Debug.DrawRay(transform.position + GetUpVector() * (FaceHighLedgeHeight + GetCurrentDashRaycastHeightOffset()), transform.forward *
            (forwardRaycastDistance + skew), Color.yellow);
        Debug.DrawRay(transform.position + GetUpVector() * FaceLedgeHeight, transform.forward, Color.cyan);
    }

    private bool FireFlankTriggers()
    {
        RaycastHit hit;
        var maximumFlankRaycastDistance = MaximumFlankWallDistance;
        var flankRaycastOrigin = transform.position + GetUpVector() * FlankWallHeight;
        if 
            (Physics.Raycast(flankRaycastOrigin, transform.right,
                 out hit, maximumFlankRaycastDistance, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore) 
             && IsHitValidFlank(hit, true) && _previousWallrunSide != FlankType.Right)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ForceSlide")) return false;
            Machine.Fire(PlayerFsmTrigger.FlankWall, new RaycastHitParam() { Hit = hit});
            _currentFlankWallNormal = hit.normal;
            _currentFlankType = FlankType.Right;
            _currentWallrunTransform = hit.transform;
            Animator.SetFloat("Flip", 0);
            return true;

        } else if 
            (Physics.Raycast(flankRaycastOrigin, -transform.right, 
                 out hit, maximumFlankRaycastDistance, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore)
             && IsHitValidFlank(hit, false) && _previousWallrunSide != FlankType.Left)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ForceSlide")) return false;
            Machine.Fire(PlayerFsmTrigger.FlankWall, new RaycastHitParam() { Hit = hit});
            _currentFlankWallNormal = hit.normal;
            _currentFlankType = FlankType.Left;
            _currentWallrunTransform = hit.transform;
            Animator.SetFloat("Flip", 1);
            return true;
        }
        else if (!Physics.Raycast(flankRaycastOrigin + (GetUpVector() * FlankWallOpenYOffset), transform.right,
                     out hit, maximumFlankRaycastDistance, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {
            Machine.Fire(PlayerFsmTrigger.FlankOpen);
            return false;
        }
        
        
        Debug.DrawRay(flankRaycastOrigin, transform.right * maximumFlankRaycastDistance, Color.blue);
        Debug.DrawRay(flankRaycastOrigin, -transform.right * maximumFlankRaycastDistance, Color.blue);
        
        return false;
    }
}