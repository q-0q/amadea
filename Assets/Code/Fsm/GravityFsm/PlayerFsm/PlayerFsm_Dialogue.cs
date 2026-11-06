using System.Linq;
using UnityEngine;

public partial class PlayerFsm
{
    private void DialogueOnUpdate()
    {
        currentPotentialInteractable = null;

        var newMomentum = Mathf.Lerp(_momentum, 0f, Time.deltaTime * 10f);
        _momentum = newMomentum;
        if (_momentum < 1.25f && !_dialogueIdle)
        {
            _dialogueIdle = true;
            ReplaceAnimatorTrigger("IdleLong");
        }
        HandleCollisionMove();
        var interacted = _inputBuffer.IsBuffered("Interact");
        if (interacted)
        {
            DialogueCanvas.Singleton.AdvanceDialogue();
            _inputBuffer.ConsumeBuffer("Interact");
        };
        SetAnimatorMomentum();
        SetAnimatorSpeedMod();
        Animator.SetLayerWeight(1, 0);
        
        
        HandleLookAt();
    }

    private void HandleLookAt()
    {
        if (DialogueCanvas.Singleton == null) return;
        if (DialogueCanvas.Singleton.currentDialogueController == null) return;
        
        var lookAt = GetLookAtTransform();
        
        var rotationTarget = lookAt.position - transform.position;
        rotationTarget = Vector3.ProjectOnPlane(rotationTarget, GetUpVector());
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(rotationTarget + transform.forward * 0.01f, transform.up), Time.deltaTime * 10f);
    }

    private static Transform GetLookAtTransform()
    {
        return DialogueCanvas.Singleton.currentDialogueController.LookAtOverride == null
            ? DialogueCanvas.Singleton.currentDialogueController.transform
            : DialogueCanvas.Singleton.currentDialogueController.LookAtOverride;
    }

    private void DialogueConfigure()
    {
        Machine.Configure(PlayerFsmState.Dialogue)
            .SubstateOf(GravityFsmState.Grounded)
            .Permit(PlayerFsmTrigger.EndDialogue, PlayerFsmState.IdleLongLoop)
            .OnEntry(_ =>
            {
                _dialogueEntryMomentum = _momentum;
                _inputBuffer.ConsumeBuffer("Interact");
                
                // this is genuinely insane and completely pattern-breaking, but it seems like it works...
                _dialogueIdle = Animator.GetCurrentAnimatorStateInfo(0).IsName("IdleLongLoop") ||
                                Animator.GetCurrentAnimatorStateInfo(0).IsName("IdleLong");
            });
    }
}