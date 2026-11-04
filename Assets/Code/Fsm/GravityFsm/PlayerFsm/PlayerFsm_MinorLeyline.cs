using System.Collections;
using Code.Misc;
using UnityEngine;

public partial class PlayerFsm
{
    private void MinorLeylineConfigure()
    {
        Machine.Configure(PlayerFsmState.MinorLeylineInteractable)
            .PermitIf(PlayerFsmTrigger.MinorLeylineTrigger, PlayerFsmState.MinorLeylineStartup, _ =>
            {
                return !Machine.IsInState(PlayerFsmState.Jump) || YVelocity < 5f;
            });
        
        Machine.Configure(PlayerFsmState.MinorLeylineStartup)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.MinorLeylineActive)
            .OnEntry(_ =>
            {
                Util.InvokeSphereEffect(transform.position + GetUpVector(), Vector3.one * 5f, 1.5f, 1f, 0 );

                // _speedLinesParticlesDuration = -1f;
                // _speedLinesParticles.Play();
                YVelocity = 0;
                _minorLeylineHalo.Show();
                MakeAllRenderersInvisible();
                _currentMinorLeylineDirection = _currentMinorLeyline.GetDirectionFromTrigger(_currentMinorLeylineTrigger);
                _currentMinorLeylineWeight = _currentMinorLeylineDirection ? 0f : 1f;
            })
            .OnExit(_ =>
            {


            })
            .SubstateOf(GravityFsmState.IgnoreDepenetration);
        
        Machine.Configure(PlayerFsmState.MinorLeylineActive)
            .Permit(PlayerFsmTrigger.Jump, PlayerFsmState.Jump)
            .OnEntry(_ =>
            {

            })
            .SubstateOf(GravityFsmState.IgnoreDepenetration)
            .OnExit(_ =>
            {
                _inputBuffer.ConsumeBuffer("Jump");
                StartCoroutine(MinorLeylineCameraCleanupCoroutine());
                LastUpwardsPosition = transform.position;
                Util.InvokeSphereEffect(transform.position + GetUpVector(), Vector3.one * 5f, 1.5f, 1f, 0 );
                isSprinting = true;
                _timeSinceMinorLeyline = 0;
                _minorLeylineHalo.Hide();
                transform.rotation = _currentMinorLeyline.GetPlayerRotationAt(_currentMinorLeylineWeight, _currentMinorLeylineDirection, out var _);

                transform.position += _currentMinorLeyline.GetUpVectorAt(_currentMinorLeylineWeight) * 1f;
                
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;
                _currentFlankType = FlankType.None;
                currentRopeSwing = null;

                float upMod = _timeSinceMinorLeylineUp < 0.3f ? 1f : 0;
                _momentum = Mathf.Lerp(MaxMomentum, 6f, upMod);
                PlaySpeedLineParticlesForDuration(Mathf.Lerp(0.75f, 0.25f, upMod));
                YVelocity = Mathf.Max(YVelocity, 27f * upMod + 26f);
                MakeAllRenderersVisible();
            });
    }
    
    private void MinorLeylineStartupOnUpdate()
    {
        var speedModifier = Mathf.Lerp(0.25f, 1f, Mathf.InverseLerp(0.3f, 0.45f, TimeInCurrentState()));
        var lerpSpeed = Mathf.Lerp(0.05f, 3f, Mathf.InverseLerp(0.3f, 0.5f, TimeInCurrentState()));
        MoveAlongCurrentMinorLeyline(speedModifier, lerpSpeed);
        PushOutMinorLeylineCamera();
     
        // MoveAlongCurrentMinorLeyline(1f, 3f);
    }
    
    private void MinorLeylineActiveOnUpdate()
    {
     
        PushOutMinorLeylineCamera();
        MoveAlongCurrentMinorLeyline(1f, 3f);
    }

    private void MoveAlongCurrentMinorLeyline(float speedModifier, float lerpSpeed)
    {
        var newWeight = _currentMinorLeylineWeight +
            Time.deltaTime * MinorLeylineSpeed * speedModifier * (_currentMinorLeylineDirection ? 1f : -1f) / _currentMinorLeyline.Length();

        transform.position = Vector3.Lerp(transform.position, _currentMinorLeyline.EvaluatePosition(newWeight), lerpSpeed);
        _currentMinorLeylineWeight = newWeight;
        
        _currentMinorLeyline.GetPlayerRotationAt(_currentMinorLeylineWeight, _currentMinorLeylineDirection, out var yVelocityMod);
        if (yVelocityMod > 0.01f)
        {
            _timeSinceMinorLeylineUp = 0f;
        }
        
        _currentMinorLeyline.SetShaderWeight(_currentMinorLeylineWeight);
        
    }

    private void PushOutMinorLeylineCamera()
    {
        var freeLook = PlayerCinemachineFreeLook.Singleton.GetFreeLook();
        var offset = freeLook.transform.GetComponent<CinemachineCameraOffset>();
        offset.m_Offset = Vector3.Lerp(offset.m_Offset,
            new Vector3(0, 0, -8f), Time.deltaTime * 1f);
    }

    private IEnumerator MinorLeylineCameraCleanupCoroutine()
    {
        var freeLook = PlayerCinemachineFreeLook.Singleton.GetFreeLook();
        var offset = freeLook.transform.GetComponent<CinemachineCameraOffset>();
        
        var t = 0f;
        var d = 1f;
        while (t < d)
        {
            var w = t / d;
            offset.m_Offset = Vector3.Lerp(offset.m_Offset,
                new Vector3(0, 0, 0), w);
            t += Time.deltaTime;
            yield return null;
        }
    }
}