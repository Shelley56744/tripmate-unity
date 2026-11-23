using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 程式合成的小音效（不需要音檔）：對話嗶嗶聲、撿東西、跳躍、落地、定案、分歧。
    /// </summary>
    public static class Sfx
    {
        const int SR = 44100;
        static AudioSource src, blipSrc;
        static AudioClip blip, pop, jump, land, chime, fanfare, buzz;
        static readonly System.Random rng = new System.Random(3);

        static void Ensure()
        {
            if (src != null) return;
            var host = Camera.main ? Camera.main.gameObject : new GameObject("TM_Sfx");
#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<AudioListener>() == null) host.AddComponent<AudioListener>();
#else
            if (Object.FindObjectOfType<AudioListener>() == null) host.AddComponent<AudioListener>();
#endif
            src = host.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f;
            blipSrc = host.AddComponent<AudioSource>();
            blipSrc.playOnAwake = false; blipSrc.spatialBlend = 0f;

            blip = Sweep("blip", 620, 660, 0.05f, 0.35f, 0.35f);
            pop = Sweep("pop", 420, 1250, 0.12f, 0.5f, 0.1f);
            jump = Sweep("jump", 280, 720, 0.16f, 0.35f, 0.2f);
            land = Noise("land", 0.08f, 0.35f);
            chime = Notes("chime", new[] { 880f, 1318.5f }, 0.16f, 0.35f);
            fanfare = Notes("fanfare", new[] { 523.3f, 659.3f, 784f, 1046.5f, 1318.5f }, 0.11f, 0.4f);
            buzz = Sweep("buzz", 240, 150, 0.32f, 0.3f, 0.6f);
        }

        // 主音：正弦＋一點方波，聽起來比較像遊戲機
        static float Tone(float phase, float squareMix)
        {
            float sin = Mathf.Sin(phase);
            float sq = sin >= 0 ? 1f : -1f;
            return sin * (1f - squareMix) + sq * squareMix * 0.6f;
        }

        static AudioClip Sweep(string name, float f0, float f1, float dur, float vol, float squareMix)
        {
            int n = (int)(SR * dur);
            var d = new float[n];
            float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += 2f * Mathf.PI * f / SR;
                float env = Mathf.Min(1f, i / (SR * 0.004f)) * Mathf.Pow(1f - t, 1.6f);
                d[i] = Tone(phase, squareMix) * env * vol;
            }
            return Make(name, d);
        }

        static AudioClip Notes(string name, float[] freqs, float noteDur, float vol)
        {
            int per = (int)(SR * noteDur);
            int tail = (int)(SR * 0.25f);
            var d = new float[per * freqs.Length + tail];
            for (int k = 0; k < freqs.Length; k++)
            {
                float phase = 0;
                int len = per + tail;
                for (int i = 0; i < len && k * per + i < d.Length; i++)
                {
                    phase += 2f * Mathf.PI * freqs[k] / SR;
                    float env = Mathf.Min(1f, i / (SR * 0.004f)) * Mathf.Exp(-i / (SR * 0.12f));
                    d[k * per + i] += Tone(phase, 0.15f) * env * vol;
                }
            }
            return Make(name, d);
        }

        static AudioClip Noise(string name, float dur, float vol)
        {
            int n = (int)(SR * dur);
            var d = new float[n];
            float y = 0;
            for (int i = 0; i < n; i++)
            {
                float x = (float)rng.NextDouble() * 2f - 1f;
                y += 0.12f * (x - y); // 簡單低通，比較像「噗」
                d[i] = y * (1f - i / (float)n) * vol * 3f;
            }
            return Make(name, d);
        }

        static AudioClip Make(string name, float[] data)
        {
            var c = AudioClip.Create("TM_" + name, data.Length, 1, SR, false);
            c.SetData(data, 0);
            return c;
        }

        static void Play(AudioClip c, float pitch = 1f, float vol = 1f)
        {
            Ensure();
            src.pitch = pitch;
            src.PlayOneShot(c, vol);
        }

        /// <summary>對話打字聲，basePitch 決定說話者的聲音高低。</summary>
        public static void Blip(float basePitch)
        {
            Ensure();
            blipSrc.pitch = basePitch * (0.85f + (float)rng.NextDouble() * 0.4f);
            blipSrc.PlayOneShot(blip, 0.5f);
        }

        public static void Pop(float pitch = 1f) { Play(pop, pitch); }
        public static void Jump() { Play(jump, 1f, 0.7f); }
        public static void Land() { Play(land, 1f, 0.6f); }
        public static void Chime() { Play(chime); }
        public static void Fanfare() { Play(fanfare); }
        public static void Buzz() { Play(buzz); }
    }
}
