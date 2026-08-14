using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Additive camera shake. Several sources can shake at once; the strongest one wins and decays,
    /// which avoids the jitter you get from stacking independent shake coroutines.
    /// Runs on unscaled time so it still reads during the hit-stop on damage.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Tooltip("How fast the shake dies down. Higher = snappier.")]
        public float decay = 5.5f;

        [Tooltip("Noise speed. Higher = more frantic.")]
        public float frequency = 34f;

        Vector3 _anchor;
        float _strength;
        float _rotationStrength;
        float _seed;

        void Awake()
        {
            _anchor = transform.localPosition;
            _seed = Random.Range(0f, 100f);
        }

        /// <summary>Requests a shake. Weaker requests are ignored while a stronger one is still running.</summary>
        public void Shake(float strength, float rotation = 0.35f)
        {
            _strength = Mathf.Max(_strength, strength);
            _rotationStrength = Mathf.Max(_rotationStrength, rotation);
        }

        /// <summary>Re-reads the rest pose. Call after the camera has been repositioned.</summary>
        public void SetAnchor(Vector3 localPosition)
        {
            _anchor = localPosition;
            transform.localPosition = _anchor;
        }

        void LateUpdate()
        {
            if (_strength <= 0.0001f)
            {
                transform.localPosition = _anchor;
                transform.localRotation = Quaternion.identity;
                return;
            }

            float t = Time.unscaledTime * frequency;
            // Perlin noise instead of Random gives a smooth, rolling shake rather than static.
            float x = (Mathf.PerlinNoise(_seed, t) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(_seed + 13.7f, t) - 0.5f) * 2f;
            float r = (Mathf.PerlinNoise(_seed + 27.3f, t) - 0.5f) * 2f;

            transform.localPosition = _anchor + new Vector3(x, y, 0f) * _strength;
            transform.localRotation = Quaternion.Euler(0f, 0f, r * _strength * _rotationStrength * 10f);

            _strength = Mathf.MoveTowards(_strength, 0f, decay * _strength * Time.unscaledDeltaTime + 0.0005f);
        }
    }
}
