using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>Easing curves for the coroutine tweens in <see cref="Tween"/>.</summary>
    public static class Easing
    {
        public enum Type
        {
            Linear,
            SineIn,
            SineOut,
            SineInOut,
            QuadIn,
            QuadOut,
            QuadInOut,
            CubicIn,
            CubicOut,
            ExpoOut,
            BackIn,
            BackOut,
            ElasticOut,
            BounceOut
        }

        const float BackOvershoot = 1.70158f;

        public static float Evaluate(Type type, float t)
        {
            t = Mathf.Clamp01(t);
            switch (type)
            {
                case Type.SineIn: return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                case Type.SineOut: return Mathf.Sin(t * Mathf.PI * 0.5f);
                case Type.SineInOut: return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                case Type.QuadIn: return t * t;
                case Type.QuadOut: return 1f - (1f - t) * (1f - t);
                case Type.QuadInOut: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case Type.CubicIn: return t * t * t;
                case Type.CubicOut: return 1f - Mathf.Pow(1f - t, 3f);
                case Type.ExpoOut: return Mathf.Approximately(t, 1f) ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case Type.BackIn: return (BackOvershoot + 1f) * t * t * t - BackOvershoot * t * t;
                case Type.BackOut:
                {
                    float p = t - 1f;
                    return 1f + (BackOvershoot + 1f) * p * p * p + BackOvershoot * p * p;
                }
                case Type.ElasticOut:
                {
                    if (t <= 0f || t >= 1f) return t;
                    const float period = 0.36f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI) / (period * 10f)) + 1f;
                }
                case Type.BounceOut: return BounceOut(t);
                default: return t;
            }
        }

        static float BounceOut(float t)
        {
            const float n = 7.5625f;
            const float d = 2.75f;
            if (t < 1f / d) return n * t * t;
            if (t < 2f / d) { t -= 1.5f / d; return n * t * t + 0.75f; }
            if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + 0.9375f; }
            t -= 2.625f / d;
            return n * t * t + 0.984375f;
        }
    }
}
