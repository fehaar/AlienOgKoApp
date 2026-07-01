using UnityEngine;

[RequireComponent(typeof(GifFrameController))]
public class GifAutoPlay : MonoBehaviour
{
    [SerializeField] float fps = 15f;

    GifFrameController _controller;
    float _elapsed;
    int _currentFrame;

    void Awake() => _controller = GetComponent<GifFrameController>();

    void Update()
    {
        if (_controller.FrameCount == 0 || fps <= 0f) return;

        _elapsed += Time.deltaTime;
        float frameDuration = 1f / fps;

        if (_elapsed >= frameDuration)
        {
            _elapsed -= frameDuration;
            _currentFrame = (_currentFrame + 1) % _controller.FrameCount;
            _controller.SetFrame(_currentFrame);
        }
    }
}
