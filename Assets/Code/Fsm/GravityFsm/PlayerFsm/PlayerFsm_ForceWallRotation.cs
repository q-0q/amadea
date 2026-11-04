using UnityEngine;

public partial class PlayerFsm
{
    private void ForceWallRotationOnUpdate()
    {
        Animator.SetLayerWeight(1, 0);
        if (Physics.Raycast(transform.position + GetUpVector() * (FaceLedgeHeight + GetCurrentDashRaycastHeightOffset()), transform.forward, out var hit,
                ForceWallRotationRaycastDistance * GetRaycastTimeModifier(), GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {
            var quaternion = Quaternion.LookRotation(-hit.normal, GetUpVector());
            transform.rotation = Quaternion.Slerp(transform.rotation, quaternion,
                RotationSpeed * Time.deltaTime * ForceWallRotationSpeed);
        }
    }
}