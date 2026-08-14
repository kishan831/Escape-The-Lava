using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Small round-robin SFX player. Every clip is generated procedurally by the editor builder,
    /// so the repository carries no audio binaries.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public GameAssets assets;

        [Range(0f, 1f)] public float masterVolume = 0.75f;

        [Tooltip("Number of AudioSources. Overlapping effects need more than one voice.")]
        public int voices = 8;

        AudioSource[] _sources;
        int _next;

        void Awake()
        {
            _sources = new AudioSource[Mathf.Max(2, voices)];
            for (int i = 0; i < _sources.Length; i++)
            {
                var go = new GameObject($"voice{i}");
                go.transform.SetParent(transform, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;   // 2D game, no positional audio
                _sources[i] = source;
            }
        }

        /// <summary>Plays a clip on the next free voice.</summary>
        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (!clip || _sources == null) return;

            AudioSource source = _sources[_next];
            _next = (_next + 1) % _sources.Length;

            source.Stop();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume) * masterVolume;
            source.pitch = Mathf.Clamp(pitch, 0.2f, 3f);
            source.Play();
        }

        public void PlayCollect(int comboMultiplier)
        {
            // Each combo step lifts the pitch a semitone, so a streak plays as a rising arpeggio.
            float pitch = Mathf.Pow(1.0595f, Mathf.Clamp(comboMultiplier - 1, 0, 8) * 2f);
            Play(assets ? assets.sfxCollect : null, 0.55f, pitch);
        }

        public void PlayDamage() => Play(assets ? assets.sfxDamage : null, 0.8f, Random.Range(0.94f, 1.06f));
        public void PlayTick(bool urgent) => Play(assets ? assets.sfxTick : null, urgent ? 0.5f : 0.28f, urgent ? 1.25f : 1f);
        public void PlayWin() => Play(assets ? assets.sfxWin : null, 0.75f);
        public void PlayLose() => Play(assets ? assets.sfxLose : null, 0.75f);
        public void PlayUi() => Play(assets ? assets.sfxUi : null, 0.5f);
        public void PlayWhoosh(float pitch = 1f) => Play(assets ? assets.sfxWhoosh : null, 0.45f, pitch);
    }
}
