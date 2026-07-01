using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GifFrameController))]
public class GifShakeController : MonoBehaviour
{
    [Header("Shake Detection")]
    [SerializeField] float triggerThreshold = 0.6f;
    [SerializeField] float lowPassStrength = 0.1f;
    [SerializeField] float idleThreshold = 0.15f;
    [SerializeField] float idleTimeout = 0.5f;

    [Header("Animation Timing")]
    [SerializeField] float openingFps = 30f;
    [SerializeField] float loopFps = 20f;
    [SerializeField] float finishFps = 15f;
    [SerializeField] int loopStartFrame = 11;
    [SerializeField] int loopEndFrame = 23;

    enum Phase { Idle, Opening, Looping, Finishing }

    GifFrameController _ctrl;
    Vector3 _lowPass;
    Phase _phase = Phase.Idle;
    float _frame;
    bool _loopForward;
    float _idleTimer;

    void Awake()
    {
        _ctrl = GetComponent<GifFrameController>();
        if (Accelerometer.current != null)
        {
            InputSystem.EnableDevice(Accelerometer.current);
            _lowPass = Accelerometer.current.acceleration.ReadValue();
        }
    }

    void Update()
    {
        float highPass = 0f;
        if (Accelerometer.current != null)
        {
            var accel = (Vector3)Accelerometer.current.acceleration.ReadValue();
            _lowPass = Vector3.Lerp(_lowPass, accel, lowPassStrength);
            highPass = (accel - _lowPass).y;
        }

        Tick(highPass);
    }

    void Tick(float highPass)
    {
        switch (_phase)
        {
            case Phase.Idle:
                if (highPass > triggerThreshold || (Keyboard.current?.spaceKey.wasPressedThisFrame ?? false))
                    BeginOpening();
                break;

            case Phase.Opening:
                // Play 1 → loopEndFrame at constant fps
                _frame += openingFps * Time.deltaTime;
                if (_frame >= loopEndFrame)
                {
                    _frame = loopEndFrame;
                    _loopForward = false; // next direction: backward toward loopStartFrame
                    _idleTimer = 0f;
                    _phase = Phase.Looping;
                }
                _ctrl.SetFrame((int)_frame);
                break;

            case Phase.Looping:
                // Ping-pong between loopStartFrame and loopEndFrame at constant fps
                float loopDelta = loopFps * Time.deltaTime;
                _frame += _loopForward ? loopDelta : -loopDelta;

                if (_frame <= loopStartFrame)
                {
                    _frame = loopStartFrame;
                    _loopForward = true;
                }
                else if (_frame >= loopEndFrame)
                {
                    _frame = loopEndFrame;
                    _loopForward = false;
                }
                _ctrl.SetFrame((int)_frame);

                // Detect when movement stops
                _idleTimer = Mathf.Abs(highPass) < idleThreshold
                    ? _idleTimer + Time.deltaTime
                    : 0f;

                if (_idleTimer >= idleTimeout)
                {
                    // Drive to loopEndFrame before finishing so we always exit from frame 23
                    _loopForward = true;
                    _phase = Phase.Finishing;
                }
                break;

            case Phase.Finishing:
                _frame += finishFps * Time.deltaTime;
                if (_frame >= _ctrl.FrameCount)
                {
                    _frame = 0;
                    _phase = Phase.Idle;
                    _ctrl.SetFrame(0);
                    break;
                }
                _ctrl.SetFrame((int)_frame);
                break;
        }
    }

    void BeginOpening()
    {
        _phase = Phase.Opening;
        _frame = 1f;
        _ctrl.SetFrame(1);
    }
}
