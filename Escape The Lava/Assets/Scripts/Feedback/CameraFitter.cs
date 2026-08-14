using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Keeps the whole 16x8 board on screen at any aspect ratio by driving the orthographic size,
    /// and re-fits when the game view is resized. Also reports the fitted rest position to
    /// <see cref="CameraShake"/> so shake always returns to the right place.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        public GameConfig config;

        [Tooltip("Extra world units of breathing room left and right of the board.")]
        public float paddingX = 0.8f;

        [Tooltip("Room reserved above the board for the HUD. Sized so the timer and hearts never sit on top of a tile.")]
        public float paddingTop = 2.4f;

        public float paddingBottom = 0.8f;

        Camera _camera;
        CameraShake _shake;
        int _lastWidth, _lastHeight;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _shake = GetComponent<CameraShake>();
        }

        void OnEnable() => Fit();

        void Update()
        {
            if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;
            Fit();
        }

        /// <summary>Recomputes orthographic size and centre so the padded board always fits.</summary>
        public void Fit()
        {
            if (!config) return;
            if (!_camera) _camera = GetComponent<Camera>();

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            Vector2 board = config.BoardSize;
            float aspect = _camera.aspect <= 0f ? 16f / 9f : _camera.aspect;

            float requiredHeight = (board.y + paddingTop + paddingBottom) * 0.5f;
            float requiredWidth = (board.x + paddingX * 2f) * 0.5f / aspect;

            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Max(requiredHeight, requiredWidth);

            // The board is centred on the origin, so the camera has to move up by half the difference
            // in padding for the extra headroom to land above the board rather than below it.
            var rest = new Vector3(0f, (paddingTop - paddingBottom) * 0.5f, -10f);
            transform.localPosition = rest;
            if (_shake) _shake.SetAnchor(rest);
        }

        /// <summary>World-space rect the board occupies. Used to place ambient effects.</summary>
        public Rect BoardRect
        {
            get
            {
                Vector2 size = config ? config.BoardSize : new Vector2(16f, 8f);
                return new Rect(-size.x * 0.5f, -size.y * 0.5f, size.x, size.y);
            }
        }
    }
}
