using Code.TriggerParams;
using UnityEngine;

public abstract partial class GravityFsm
{


    public void RespectParentTransformOnUpdate()
    {
        return;
        if (parentTransform == null) return;

        // Move position by the parent's movement delta
        Vector3 posDiff = parentTransform.position - _previousParentTransformPosition;
        transform.position += ComputeCollisionMove(posDiff);

        // Compute rotation delta
        Quaternion rotationDelta = parentTransform.rotation * Quaternion.Inverse(_previousParentRotation);

        // Rotate position around the parent
        Vector3 offset = transform.position - parentTransform.position;
        offset = rotationDelta * offset;
        Vector3 rotationMove = (parentTransform.position + offset) - transform.position;
        transform.position += ComputeCollisionMove(rotationMove);
    
        // Apply only the yaw (rotation around the arbitrary up vector), discarding pitch and roll
        Vector3 up = GetUpVector();
        Vector3 forward = rotationDelta * transform.forward;
        forward = Vector3.ProjectOnPlane(forward, up); // Replaces forward.y = 0;
    
        if (forward.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(forward.normalized, up);
        }

        // Update previous transform state
        _previousParentTransformPosition = parentTransform.position;
        _previousParentRotation = parentTransform.rotation;
    }

    public void ForceParentTransformSync()
    {
        _previousParentTransformPosition = parentTransform.position;
        _previousParentRotation = parentTransform.rotation;
    }

    
    private void RespectParentTransformConfigure()
    {
        Machine.Configure(GravityFsmState.RespectParentTransform)
            .OnEntry(@params =>
            {
                if (@params is not RaycastHitParam p) return;
                if (parentTransform != null)
                {
                    if (p.Hit.transform != parentTransform) OnParentTransformChanged(p.Hit.transform);
                }
                else
                {
                    OnParentTransformChanged(p.Hit.transform);                   
                }
                parentTransform = p.Hit.transform;
                _previousParentTransformPosition = parentTransform.position;
                _previousParentRotation = parentTransform.rotation;
            });
    }

}