using UnityEngine;

[RequireComponent(typeof(GifFrameController))]
public class GifAutoPlay : MonoBehaviour
{
    [SerializeField] float fps = 15f;
    [SerializeField] bool playOnAwake = true;
    [SerializeField] bool loop = true;

    GifFrameController _controller;
    float _elapsed;

    public bool IsPlaying { get; private set; }
    public int CurrentFrame { get; private set; }
    public int FrameCount => _controller.FrameCount;

    void Awake()
    {
        _controller = GetComponent<GifFrameController>();
        IsPlaying = playOnAwake;
    }

    void Update()
    {
        if (!IsPlaying || _controller.FrameCount == 0 || fps <= 0f) return;

        _elapsed += Time.deltaTime;
        float frameDuration = 1f / fps;

        while (_elapsed >= frameDuration)
        {
            _elapsed -= frameDuration;
            int next = CurrentFrame + 1;

            if (next >= _controller.FrameCount)
            {
                if (loop)
                {
                    next = 0;
                }
                else
                {
                    IsPlaying = false;
                    return;
                }
            }

            CurrentFrame = next;
            _controller.SetFrame(CurrentFrame);
        }
    }

    public void Play() => IsPlaying = true;

    public void Stop() => IsPlaying = false;

    public void Restart()
    {
        CurrentFrame = 0;
        _elapsed = 0f;
        _controller.SetFrame(0);
        IsPlaying = true;
    }

    public void StepFrame(int delta)
    {
        IsPlaying = false;
        CurrentFrame = (CurrentFrame + delta + _controller.FrameCount) % _controller.FrameCount;
        _controller.SetFrame(CurrentFrame);
    }
}
