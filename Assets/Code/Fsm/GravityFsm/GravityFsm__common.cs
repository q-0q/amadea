
using System.Linq;
using Code.Misc;
using UnityEngine;


public abstract partial class GravityFsm
{
    protected float YVelocity;
    protected float GravityStrength;
    protected float BonusGravityModifier;
    protected float TimeInAir;
    protected float MinYVelocity = -50f;
    protected float LastUpwardsY;
    public float GroundForwardSlope;
    protected Vector3 GroundNormal;
    private Collider _depenetrationCollider;
    private float _depenetrationTimer;
    protected bool IsInGust = false;
    private float _timeOfLastGustSinOffset;
    private float _timeAtTopOfGust;
    private float _currentSlopeCastMinimumDistanceOffset;

    protected Vector3 _safeGroundPosition;
    protected Vector3 _firstAirPosition;

    public Transform parentTransform;
    protected Vector3 _previousParentTransformPosition;
    protected Quaternion _previousParentRotation;
    public GravityFsmSpringCollider springCollider;
    protected Vector3 aerialMomentumOffset;


    private const float GroundedYPositionLerpStrength = 50f;
    protected const float GroundedRaycastLength = 2f;
    protected const float GroundedRaycastForwardOffset = 0.05f;

    private const float GroundedRaycastMaximumAngle = 65f;

    private const float CollisionMoveSphereCastRadius = 0.4f;
    private const float GroundCollisionMoveSphereCastHeight = 1.15f;
    private const float FallingCollisionMoveSphereCastHeight = 1.15f;
    private const float FallingCollisionMoveSphereCastHeightYVelocityThreshhold = -10f;
    private const float CollisionMoveSphereCastDistance = 0.45f;
    private const float DepenetrationTimerDuration = 0.25f;

    public enum GroundKind
    {
        Standard,
        Tightrope
    }


    protected bool GetGroundedRaycastHit(out RaycastHit hit, bool debug = false)
    {
        var raycastLength = GroundedRaycastLength * GetRaycastTimeModifier();
        var forward = transform.forward * (GroundedRaycastForwardOffset * GetRaycastTimeModifier());
        var f = 1.5f;
        var minDistance = 0.1f;



        
        var sphereCastOrigin = transform.position + Vector3.up * (f * raycastLength) + forward + aerialMomentumOffset;
        var sphereCastMaxDistance = raycastLength * f * 2f;
        var sphereSpherecastDirection = -Vector3.up;
        
        Debug.DrawRay(sphereCastOrigin, sphereSpherecastDirection * sphereCastMaxDistance, Color.magenta);
        
        if (Physics.SphereCast(sphereCastOrigin, 0.35f, sphereSpherecastDirection,
                out hit,
                sphereCastMaxDistance, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {


            if (Physics.Raycast(hit.point + Vector3.up * 3f, Vector3.down, out var recast, 6f,
                    GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore)) hit = recast;
            
            var angle = Vector3.Angle(hit.GetCorrectNormalForSphere(sphereSpherecastDirection), Vector3.up);
            if (angle > 75f) return false;
            var slopeMinDistanceOffset =
                Mathf.Lerp(0, 2f, Mathf.InverseLerp(0f, 90f, angle));

            _currentSlopeCastMinimumDistanceOffset = slopeMinDistanceOffset > _currentSlopeCastMinimumDistanceOffset
                ? slopeMinDistanceOffset
                : Mathf.Lerp(_currentSlopeCastMinimumDistanceOffset, slopeMinDistanceOffset, Time.deltaTime * 5f);
            
            // if (Mathf.Abs(YVelocity) < 0.075f) return true;
            var b = transform.position.y - hit.point.y < (minDistance * (Machine.IsInState(GravityFsmState.Grounded) ? 50f : 1f)) + slopeMinDistanceOffset;
            if (b)
            {
                // if (Physics.Raycast(hit.point - Vector3.up, Vector3.up, 2f, GetEnvironmentalLayermask(),
                //         QueryTriggerInteraction.Ignore))
                //     return false;
                //
                // Debug.DrawRay(hit.point, Vector3.up, Color.magenta, 5f);
                // Debug.DrawRay(hit.point, Vector3.forward, Color.magenta, 5f);
            }
            
            return b;
        }

        return false;
    }


    protected float CurrentFallDistance()
    {
        var diff = transform.position.y - LastUpwardsY;
        return diff;
    }

    private void UpdateYVelocityMetadata()
    {
        YVelocity = Mathf.Max(YVelocity, MinYVelocity);
        if (YVelocity > 0 || Machine.IsInState(GravityFsmState.Grounded))
            LastUpwardsY = transform.position.y;
    }

protected Vector3 ComputeCollisionMove(Vector3 desiredMove, float yModifier = 1f)
{
    if (GameMenu.Singleton.IsMenuOpen()) return Vector3.zero;
    var output = desiredMove;

    if (Machine.IsInState(GravityFsm.GravityFsmState.IgnoreCollisionMoveClipping)) return output; //....?

    // Radius of your character (adjust as needed)
    var backwardsPadding = 0.45f;
    float radius = CollisionMoveSphereCastRadius;
    float castDistance = (CollisionMoveSphereCastDistance * GetRaycastTimeModifier()) - (radius * 0.45f) +
                         backwardsPadding;

    Vector3 position = transform.position + Vector3.up *
        (YVelocity > FallingCollisionMoveSphereCastHeightYVelocityThreshhold
            ? GroundCollisionMoveSphereCastHeight
            : FallingCollisionMoveSphereCastHeight);
    Vector3 direction = output.normalized;

    // SphereCast to account for player volume
    if (Physics.SphereCast(position - transform.forward * backwardsPadding, radius, direction, out RaycastHit hit,
            castDistance, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
    {
        // First collision: slide along the surface
        Vector3 firstNormal = hit.normal;
        
        // Full sliding along the actual slope
        Vector3 slideMax = Vector3.ProjectOnPlane(output, firstNormal);
        // Zero vertical sliding (your original wall-flattening method)
        Vector3 slideZero = Vector3.ProjectOnPlane(output, Vector3.ProjectOnPlane(firstNormal, Vector3.up).normalized);
        
        // Interpolate based on the modifier
        output = Vector3.Lerp(slideZero, slideMax, yModifier);

        // Cast again in the new direction to handle corner (second surface)
        if (Physics.SphereCast(position - firstNormal * backwardsPadding, radius, output.normalized,
                out RaycastHit secondHit, output.magnitude + backwardsPadding))
        {
            Vector3 secondNormal = secondHit.normal;

            // Slide again for the second surface
            Vector3 secondSlideMax = Vector3.ProjectOnPlane(output, secondNormal);
            Vector3 secondSlideZero = Vector3.ProjectOnPlane(output, Vector3.ProjectOnPlane(secondNormal, Vector3.up).normalized);
            
            output = Vector3.Lerp(secondSlideZero, secondSlideMax, yModifier);

            if (output.magnitude < 0.01f)
            {
                output = Vector3.zero;
            }
        }
    }

    return output;
}


    private void HandleDepenetration()
    {
        
        
        var neighbors = Physics.OverlapCapsule(transform.position, transform.position + Vector3.up * 3f, 0.5f,
            GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore);
        foreach (var neighbor in neighbors)
        {
            if (neighbor.gameObject.layer == LayerMask.NameToLayer("Tightrope")) continue;
            if (Physics.ComputePenetration(_depenetrationCollider, _depenetrationCollider.transform.position,
                    _depenetrationCollider.transform.rotation, neighbor,
                    neighbor.transform.position, neighbor.transform.rotation, out Vector3 direction,
                    out float distance))
            {
                _depenetrationTimer += Time.deltaTime;
                if (Machine.IsInState(GravityFsmState.IgnoreDepenetration)) return;
                if (Machine.IsInState(GravityFsmState.IgnoreVerticalDepenetration))
                    direction = new Vector3(direction.x, 0f, direction.z);
                transform.position += direction * distance;
            }
            else
            {
                _depenetrationTimer = 0;
            }
            

        }
    }

    private void HandleGust()
    {
        if (Machine.IsInState(GravityFsmState.DontApplyGustYVelocity)) return;
        
        var masks = Physics.OverlapSphere(transform.position, 0.5f,
            LayerMask.GetMask("GustMask"), QueryTriggerInteraction.Collide);
        if (masks.Any())
        {
            _timeAtTopOfGust = 0f;
            IsInGust = false;
            return;
        }
        
        var neighbors = Physics.OverlapSphere(transform.position, 1f,
            LayerMask.GetMask("Gust"), QueryTriggerInteraction.Collide);
        foreach (var neighbor in neighbors)
        {
            IsInGust = true;
            
            // Calculate strengthModifier based on y position of player. Goes from 1 -> 0 as the player
            // approaches the top of the gust trigger.
            var localYToGustCollider = transform.position.y - neighbor.transform.position.y;
            var falloffWindowSize = 20f;
            var gustMaxY = neighbor.transform.lossyScale.y * 0.5f;
            var strengthModifier = Mathf.InverseLerp(gustMaxY, gustMaxY - falloffWindowSize, localYToGustCollider + 5f);
            
            // Calculate the sin-based offset which gives a bobbing effect. Increases in strength as long as the player
            // remains near the top of the gust collider. This is to prevent the bobbing from interfering with player
            // motion when traveling upwards through the gust, giving better consistency.
            if (strengthModifier <= 0.5f) _timeAtTopOfGust += Time.deltaTime;
            else _timeAtTopOfGust = 0;
            var offsetModifier = Mathf.InverseLerp(0, 0.2f, _timeAtTopOfGust);
            var offset = Mathf.Lerp(0, -15f, Mathf.InverseLerp(-1f, 1f, Mathf.Sin(Time.time * 2.5f))) * offsetModifier;
            
            // Set Y velocity as a function of the strength modifier and the offset. Less aggressive
            // when accelerating, more aggressive when decelerating.
            var desiredYVelocity = Mathf.Lerp(0, 60f, strengthModifier) + offset;
            // var lerpStrength = YVelocity < desiredYVelocity ? 2.5f : 4.5f;
            var lerpStrength = Mathf.Lerp(4.5f, 1f, strengthModifier);
            var lerpStrengthFallMod = Mathf.Lerp(1f, 0.6f, Mathf.InverseLerp(-10f, -20f, YVelocity));
            YVelocity = Mathf.Lerp(YVelocity, desiredYVelocity, Time.deltaTime * lerpStrength * lerpStrengthFallMod);
            
            return;
        }
        
        _timeAtTopOfGust = 0f;
        IsInGust = false;
    }

    


    private void InstantiateSpringCollider()
    {
        var springColliderPrefab = Resources.Load("Prefab/Fsm/GravityFsmSpringCollider") as GameObject;
        var springCollider = Instantiate(springColliderPrefab, transform.position, Quaternion.identity);
        this.springCollider = springCollider.GetComponentInChildren<GravityFsmSpringCollider>();
        this.springCollider.SetOwner(this);
    }

    protected virtual void OnParentTransformChanged(Transform t)
    {
    }

    public float GetYVelocity()
    {
        return YVelocity;
    }

    public void SetParentTransform(Transform t)
    {
        if (t != parentTransform) OnParentTransformChanged(parentTransform);
        parentTransform = t;
        _previousParentTransformPosition = parentTransform.position;
        _previousParentRotation = parentTransform.rotation;
    }
}