using UnityEngine;

// Triggers the GIF animation on upward phone movement (accelerometer) or keyboard.
// Keyboard controls: Space = play/restart, Left/Right = step frame (pauses), P = pause/resume.
[RequireComponent(typeof(GifAutoPlay))]
public class GifShakeController : MonoBehaviour
{
    [Header("Shake Detection")]
    [SerializeField] float upwardThreshold = 1.2f;   // g-force delta to trigger
    [SerializeField] float cooldownSeconds = 0.5f;    // minimum time between triggers
    [SerializeField] float lowPassStrength = 0.1f;    // lower = smoother gravity removal

    GifAutoPlay _autoPlay;
    Vector3 _lowPass;
    float _cooldownRemaining;
    bool _accelAvailable;

    void Awake()
    {
        _autoPlay = GetComponent<GifAutoPlay>();
        _accelAvailable = SystemInfo.supportsAccelerometer;
        if (_accelAvailable)
            _lowPass = Input.acceleration;
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
        if (!_accelAvailable) return;

        var accel = Input.acceleration;
        _lowPass = Vector3.Lerp(_lowPass, accel, lowPassStrength);
        float upwardForce = (accel - _lowPass).y;   // high-pass: removes gravity

        if (upwardForce > upwardThreshold && _cooldownRemaining <= 0f)
            Trigger();
    }

    void HandleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            Trigger();

        if (Input.GetKeyDown(KeyCode.RightArrow))
            _autoPlay.StepFrame(1);

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            _autoPlay.StepFrame(-1);

        if (Input.GetKeyDown(KeyCode.P))
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
