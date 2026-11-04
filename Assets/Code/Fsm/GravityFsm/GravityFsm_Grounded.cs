using Code.TriggerParams;
using UnityEngine;

public abstract partial class GravityFsm
{
    private void GroundedOnUpdate()
    {
        YVelocity = 0;
        _timeAtTopOfGust = 0;
        IsInGust = false;
        if (GetGroundedRaycastHit(out var hit))
        {

            var customUp = GetUpVector();
            
            
            float currentHeight = Vector3.Dot(transform.position, customUp);
            float targetHeight = Vector3.Dot(hit.point, customUp);

            // 2. Lerp between the heights along the custom up axis
            float newHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * GroundedYPositionLerpStrength);

            // 3. Rebuild the position by keeping the plane position and applying the new height
            Vector3 positionOnPlane = transform.position - (customUp * currentHeight);
            transform.position = positionOnPlane + (customUp * newHeight);

            // 4. Slope calculation (Vector3.Angle works in 3D, but ensure transform.forward aligns with your custom up)
            GroundForwardSlope = Vector3.Angle(transform.forward, hit.normal);
            
            
            // var newY = Mathf.Lerp(transform.position.y, hit.point.y, Time.deltaTime * GroundedYPositionLerpStrength);
            // transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            // GroundForwardSlope = Vector3.Angle(transform.forward, hit.normal);
            GroundNormal = hit.normal;
            if (hit.transform != parentTransform)
            {
                parentTransform = hit.transform;
                _previousParentTransformPosition = parentTransform.position;
                _previousParentRotation = parentTransform.rotation;
                OnParentTransformChanged(parentTransform);
            }
            
        }
        UpdateYVelocityMetadata();
    }

    private void GroundedConfigure()
    {
        Machine.Configure(GravityFsmState.Grounded)
            .SubstateOf(GravityFsmState.RespectParentTransform);
    }
}