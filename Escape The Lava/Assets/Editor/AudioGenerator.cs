using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeTheLava.EditorTools
{
    /// <summary>
    /// Synthesises every sound effect as a 16-bit mono WAV.
    ///
    /// Same reasoning as the art: the repository stays free of binary assets and the project runs
    /// straight after cloning. Each effect is a handful of oscillators and envelopes, which is plenty
    /// for the arcade feedback this game needs.
    /// </summary>
    public static class AudioGenerator
    {
        const int SampleRate = 44100;

        public static void Generate(GameAssets art)
        {
            BuildPaths.EnsureFolder(BuildPaths.AudioFolder);

            Write("sfx_collect", Collect());
            Write("sfx_damage", Damage());
            Write("sfx_tick", Tick());
            Write("sfx_win", Win());
            Write("sfx_lose", Lose());
            Write("sfx_ui", Ui());
            Write("sfx_whoosh", Whoosh());

            AssetDatabase.Refresh();

            art.sfxCollect = Import("sfx_collect");
            art.sfxDamage = Import("sfx_damage");
            art.sfxTick = Import("sfx_tick");
            art.sfxWin = Import("sfx_win");
            art.sfxLose = Import("sfx_lose");
            art.sfxUi = Import("sfx_ui");
            art.sfxWhoosh = Import("sfx_whoosh");

            EditorUtility.SetDirty(art);
        }

        // ---------------------------------------------------------------------- voices

        /// <summary>Crystalline pick-up: a fast upward sweep plus two harmonics.</summary>
        static float[] Collect()
        {
            float[] buffer = Allocate(0.26f);

            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / (float)SampleRate;
                float sweep = Mathf.Lerp(880f, 1480f, Mathf.Clamp01(t / 0.035f));
                float envelope = Envelope(t, 0.004f, 0.24f, 2.6f);

                buffer[i] = envelope * (
                    Sine(sweep, t) * 0.6f +
                    Sine(sweep * 2f, t) * 0.25f +
                    Sine(sweep * 3.01f, t) * 0.12f);
            }

            return Normalize(buffer, 0.9f);
        }

        /// <summary>Lava burn: a falling body thud under a hiss of steam, lightly saturated.</summary>
        static float[] Damage()
        {
            float[] buffer = Allocate(0.5f);
            float lowpass = 0f;

            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / (float)SampleRate;

                float thudFrequency = Mathf.Lerp(150f, 44f, Mathf.Clamp01(t / 0.2f));
                float thud = Sine(thudFrequency, t) * Envelope(t, 0.002f, 0.34f, 2.2f);

                // One-pole lowpass with a closing cutoff turns white noise into escaping steam.
                float noise = UnityEngine.Random.Range(-1f, 1f);
                float cutoff = Mathf.Lerp(0.45f, 0.03f, Mathf.Clamp01(t / 0.3f));
                lowpass += (noise - lowpass) * cutoff;
                float steam = lowpass * Envelope(t, 0.001f, 0.3f, 1.8f) * 0.75f;

                buffer[i] = (float)Math.Tanh((thud + steam) * 1.8f);
            }

            return Normalize(buffer, 0.95f);
        }

        /// <summary>Clock tick: one short, dry blip.</summary>
        static float[] Tick()
        {
            float[] buffer = Allocate(0.06f);

            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Envelope(t, 0.001f, 0.05f, 4f);
                buffer[i] = envelope * (Sine(1250f, t) * 0.7f + Sine(2500f, t) * 0.3f);
            }

            return Normalize(buffer, 0.75f);
        }

        /// <summary>Win fanfare: a major arpeggio with each note ringing over the next.</summary>
        static float[] Win()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
            float step = 0.11f;
            float[] buffer = Allocate(step * notes.Length + 0.7f);

            for (int n = 0; n < notes.Length; n++)
            {
                int offset = (int)(n * step * SampleRate);

                for (int i = offset; i < buffer.Length; i++)
                {
                    float t = (i - offset) / (float)SampleRate;
                    float envelope = Envelope(t, 0.006f, 0.55f, 2.4f);

                    buffer[i] += envelope * (
                        Sine(notes[n], t) * 0.5f +
                        Sine(notes[n] * 2f, t) * 0.18f +
                        Triangle(notes[n] * 0.5f, t) * 0.12f) * 0.55f;
                }
            }

            return Normalize(buffer, 0.9f);
        }

        /// <summary>Loss: a long downward sweep with a detuned second voice.</summary>
        static float[] Lose()
        {
            float[] buffer = Allocate(1.1f);

            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / (float)SampleRate;
                float progress = Mathf.Clamp01(t / 0.95f);
                float frequency = Mathf.Lerp(420f, 62f, progress * progress);
                float envelope = Envelope(t, 0.02f, 1.05f, 1.3f);

                buffer[i] = envelope * (
                    Saw(frequency, t) * 0.35f +
                    Saw(frequency * 1.008f, t) * 0.3f +
                    Sine(frequency * 0.5f, t) * 0.3f);
            }

            return Normalize(buffer, 0.85f);
        }

        /// <summary>UI confirm: a soft two-tone blip.</summary>
        static float[] Ui()
        {
            float[] buffer = Allocate(0.11f);

            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / (float)SampleRate;
                float frequency = t < 0.045f ? 620f : 930f;
                buffer[i] = Envelope(t, 0.003f, 0.1f, 3f) * Sine(frequency, t) * 0.8f;
            }

            return Normalize(buffer, 0.7f);
        }

        /// <summary>Transition whoosh: filtered noise with an opening then closing cutoff.</summary>
        static float[] Whoosh()
        {
            float[] buffer = Allocate(0.45f);
            float lowpass = 0f;

            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / (float)SampleRate;
                float progress = Mathf.Clamp01(t / 0.45f);

                float noise = UnityEngine.Random.Range(-1f, 1f);
                float cutoff = Mathf.Sin(progress * Mathf.PI) * 0.32f + 0.02f;
                lowpass += (noise - lowpass) * cutoff;

                buffer[i] = lowpass * Mathf.Sin(progress * Mathf.PI) * 0.9f;
            }

            return Normalize(buffer, 0.8f);
        }

        // ---------------------------------------------------------------------- dsp helpers

        static float[] Allocate(float seconds) => new float[Mathf.CeilToInt(seconds * SampleRate)];

        static float Sine(float frequency, float t) => Mathf.Sin(2f * Mathf.PI * frequency * t);

        static float Saw(float frequency, float t) => Mathf.Repeat(frequency * t, 1f) * 2f - 1f;

        static float Triangle(float frequency, float t) => Mathf.Abs(Mathf.Repeat(frequency * t, 1f) * 4f - 2f) - 1f;

        /// <summary>Linear attack into an exponential decay. <paramref name="curve"/> shapes the tail.</summary>
        static float Envelope(float t, float attack, float decay, float curve)
        {
            if (t < 0f) return 0f;
            if (t < attack) return attack <= 0f ? 1f : t / attack;

            float x = Mathf.Clamp01((t - attack) / Mathf.Max(0.0001f, decay));
            return Mathf.Pow(1f - x, curve);
        }

        static float[] Normalize(float[] buffer, float peak)
        {
            float max = 0f;
            foreach (float sample in buffer) max = Mathf.Max(max, Mathf.Abs(sample));
            if (max <= 0.0001f) return buffer;

            float gain = peak / max;
            for (int i = 0; i < buffer.Length; i++) buffer[i] *= gain;
            return buffer;
        }

        // ---------------------------------------------------------------------- io

        /// <summary>Writes a mono 16-bit PCM WAV file.</summary>
        static void Write(string name, float[] samples)
        {
            string absolutePath = Path.Combine(BuildPaths.ProjectRoot, $"{BuildPaths.AudioFolder}/{name}.wav");

            using var stream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);

            int dataBytes = samples.Length * 2;

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });

            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);                          // PCM chunk size
            writer.Write((short)1);                    // format: PCM
            writer.Write((short)1);                    // channels: mono
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);              // byte rate
            writer.Write((short)2);                    // block align
            writer.Write((short)16);                   // bits per sample

            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);

            foreach (float sample in samples)
            {
                writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
            }
        }

        static AudioClip Import(string name)
        {
            string path = $"{BuildPaths.AudioFolder}/{name}.wav";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;   // tiny clips, no streaming needed
                settings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
