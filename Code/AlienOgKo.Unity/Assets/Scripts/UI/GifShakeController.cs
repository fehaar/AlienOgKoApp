using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Triggers the GIF animation on upward phone movement (accelerometer) or keyboard.
// Keyboard controls: Space = play/restart, Left/Right = step frame (pauses), P = pause/resume.
[RequireComponent(typeof(GifAutoPlay))]
public class GifShakeController : MonoBehaviour
{
    [Header("Shake Detection")]
    [SerializeField] float upwardThreshold = 1.2f;
    [SerializeField] float cooldownSeconds = 0.5f;
    [SerializeField] float lowPassStrength = 0.1f;

    GifAutoPlay _autoPlay;
    Vector3 _lowPass;
    float _cooldownRemaining;

    void Awake()
    {
        _autoPlay = GetComponent<GifAutoPlay>();

        if (Accelerometer.current != null)
        {
            InputSystem.EnableDevice(Accelerometer.current);
            _lowPass = Accelerometer.current.acceleration.ReadValue();
        }
    }

    void Update()
    {
        if (_cooldownRemaining > 0f)
            _cooldownRemaining -= Time.deltaTime;

        HandleAccelerometer();
        HandleKeyboard();
    }

    void HandleAccelerometer()
    {
        if (Accelerometer.current == null) return;

        var accel = (Vector3)Accelerometer.current.acceleration.ReadValue();
        _lowPass = Vector3.Lerp(_lowPass, accel, lowPassStrength);
        float upwardForce = (accel - _lowPass).y;

        if (upwardForce > upwardThreshold && _cooldownRemaining <= 0f)
            Trigger();
    }

    void HandleKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame)
            Trigger();

        if (kb.rightArrowKey.wasPressedThisFrame)
            _autoPlay.StepFrame(1);

        if (kb.leftArrowKey.wasPressedThisFrame)
            _autoPlay.StepFrame(-1);

        if (kb.pKey.wasPressedThisFrame)
        {
            if (_autoPlay.IsPlaying) _autoPlay.Stop();
            else _autoPlay.Play();
        }
    }

    void Trigger()
    {
        _cooldownRemaining = cooldownSeconds;
        _autoPlay.Restart();
    }
}
