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

    [Header("Animation Frames")]
    [SerializeField] int loopStartFrame = 11;
    [SerializeField] int loopEndFrame = 23;

    [Header("Animation Speed (fps)")]
    [SerializeField] float openingFps = 30f;
    [SerializeField] float loopFps = 20f;
    [SerializeField] float finishFps = 15f;

    enum Phase { Idle, Opening, Looping, Finishing }

    GifFrameController _ctrl;
    Vector3 _lowPass;
    Phase _phase = Phase.Idle;
    float _frame;
    bool _loopForward;
    float _idleTimer;
    float _prevHighPass;
    float _retriggerCooldown;

    void Awake()
    {
        _ctrl = GetComponent<GifFrameController>();
        if (Accelerometer.current != null)
        {
            InputSystem.EnableDevice(Accelerometer.current);
            _lowPass = Accelerometer.current.acceleration.ReadValue();
        }
    }

    void Start()
    {
        // In the editor (no phone), start straight in the loop so the animation is visible
        if (!Application.isMobilePlatform)
        {
            _frame = loopEndFrame;
            _loopForward = false;
            _idleTimer = 0f;
            _phase = Phase.Looping;
            _ctrl.SetFrame(loopEndFrame);
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

        HandleSpace();
        Tick(highPass);
        _prevHighPass = highPass;
    }

    void HandleSpace()
    {
        if (!(Keyboard.current?.spaceKey.wasPressedThisFrame ?? false)) return;

        if (_phase == Phase.Looping)
            _phase = Phase.Finishing;   // Space during loop → play end
        else
            BeginOpening();             // Space during Idle/Finishing → restart
    }

    void Tick(float highPass)
    {
        switch (_phase)
        {
            case Phase.Idle:
                if (_retriggerCooldown > 0f)
                    _retriggerCooldown -= Time.deltaTime;
                // Only trigger on a rising edge after cooldown has expired
                else if (highPass > triggerThreshold && _prevHighPass <= triggerThreshold)
                    BeginOpening();
                break;

            case Phase.Opening:
                _frame += openingFps * Time.deltaTime;
                if (_frame >= loopEndFrame)
                {
                    _frame = loopEndFrame;
                    _loopForward = false;
                    _idleTimer = 0f;
                    _phase = Phase.Looping;
                }
                _ctrl.SetFrame((int)_frame);
                break;

            case Phase.Looping:
                float loopDelta = loopFps * Time.deltaTime;
                _frame += _loopForward ? loopDelta : -loopDelta;

                if (_frame <= loopStartFrame) { _frame = loopStartFrame; _loopForward = true; }
                else if (_frame >= loopEndFrame) { _frame = loopEndFrame; _loopForward = false; }

                _ctrl.SetFrame((int)_frame);

                // On device: exit loop when movement stops
                if (Application.isMobilePlatform)
                {
                    _idleTimer = Mathf.Abs(highPass) < idleThreshold
                        ? _idleTimer + Time.deltaTime : 0f;

                    if (_idleTimer >= idleTimeout)
                        _phase = Phase.Finishing;
                }
                break;

            case Phase.Finishing:
                _frame += finishFps * Time.deltaTime;
                if (_frame >= _ctrl.FrameCount)
                {
                    _frame = 0;
                    _retriggerCooldown = 1f;
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
