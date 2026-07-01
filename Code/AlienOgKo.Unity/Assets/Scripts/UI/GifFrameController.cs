using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GifFrameController : MonoBehaviour
{
    [SerializeField] Sprite[] frames;

    Image _image;

    void Awake()
    {
        _image = GetComponent<Image>();
        if (frames is { Length: > 0 })
            _image.sprite = frames[0];
    }

    public int FrameCount => frames?.Length ?? 0;

    public void SetFrames(Sprite[] sprites)
    {
        frames = sprites;
        if (frames is { Length: > 0 } && _image != null)
            _image.sprite = frames[0];
    }

    public void SetFrame(int index)
    {
        if (frames == null || frames.Length == 0) return;
        _image.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }
}
