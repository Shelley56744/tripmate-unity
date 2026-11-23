using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TripMate
{
    /// <summary>旅遊印章：散落在小鎮各處，走過去就會收集。</summary>
    public class Collectible : MonoBehaviour
    {
        public static readonly List<Collectible> All = new List<Collectible>();
        public static int Collected;
        public static Transform Player;
        public static int Total => All.Count;

        public bool Taken { get; private set; }
        /// <summary>不是空字串就是「隱藏印章」：撿到解鎖這個隱藏景點，算 3 枚印章。</summary>
        public string hiddenPoi = "";
        Transform coin;

        public static void ResetAll()
        {
            All.Clear();
            Collected = 0;
        }

        /// <summary>紫色的隱藏印章：比一般印章大、會發光，藏在角落。</summary>
        public static Collectible CreateHidden(Transform parent, Vector3 pos, string poiId)
        {
            var root = TM.Empty("HiddenStamp", parent, pos);
            var c = root.gameObject.AddComponent<Collectible>();
            c.hiddenPoi = poiId;
            var spin = TM.Empty("Spin", root, new Vector3(0, 1.1f, 0));
            spin.gameObject.AddComponent<Spin>().speed = new Vector3(0, 110, 0);
            var bob = spin.gameObject.AddComponent<Bob>();
            bob.amp = 0.15f; bob.speed = 2f;
            Color purple = TM.Hex("#8C5BE0");
            TM.Prim(PrimitiveType.Cylinder, "Coin", spin, Vector3.zero, new Vector3(0.8f, 0.06f, 0.8f), purple, 0.9f, new Vector3(90, 0, 0));
            TM.Prim(PrimitiveType.Cylinder, "Ring", spin, Vector3.zero, new Vector3(0.62f, 0.07f, 0.62f), TM.Hex("#E9DEFF"), 0.6f, new Vector3(90, 0, 0));
            TM.Prim(PrimitiveType.Cube, "StarA", spin, Vector3.zero, new Vector3(0.3f, 0.3f, 0.09f), TM.Hex("#FFC93C"), 0.9f, new Vector3(0, 0, 45));
            TM.Prim(PrimitiveType.Cube, "StarB", spin, Vector3.zero, new Vector3(0.3f, 0.3f, 0.09f), TM.Hex("#FFC93C"), 0.9f);
            TM.MeshObj("Glow", root, TM.Torus(0.6f, 0.04f, 24, 4), new Vector3(0, 0.04f, 0), purple, 1.5f);
            TM.Prim(PrimitiveType.Cylinder, "Beam", root, new Vector3(0, 2.5f, 0), new Vector3(0.12f, 2.5f, 0.12f), TM.Hex("#C9B3FF"), 1.2f);
            c.coin = spin;
            All.Add(c);
            return c;
        }

        public static Collectible Create(Transform parent, Vector3 pos)
        {
            var root = TM.Empty("Stamp", parent, pos);
            var c = root.gameObject.AddComponent<Collectible>();
            var spin = TM.Empty("Spin", root, new Vector3(0, 0.95f, 0));
            spin.gameObject.AddComponent<Spin>().speed = new Vector3(0, 140, 0);
            var bob = spin.gameObject.AddComponent<Bob>();
            bob.amp = 0.12f; bob.speed = 2.5f;
            // 金色硬幣＋紅色印面＋白色外圈
            TM.Prim(PrimitiveType.Cylinder, "Coin", spin, Vector3.zero, new Vector3(0.62f, 0.05f, 0.62f), TM.Hex("#FFC93C"), 0.7f, new Vector3(90, 0, 0));
            TM.Prim(PrimitiveType.Cylinder, "Seal", spin, Vector3.zero, new Vector3(0.38f, 0.06f, 0.38f), TM.Hex("#E2463B"), 0.4f, new Vector3(90, 0, 0));
            TM.Prim(PrimitiveType.Sphere, "Dot", spin, Vector3.zero, new Vector3(0.14f, 0.14f, 0.1f), Color.white, 0.8f);
            // 地上的小光圈
            TM.MeshObj("Glow", root, TM.Torus(0.45f, 0.03f, 24, 4), new Vector3(0, 0.04f, 0), TM.Hex("#FFC93C"), 1.2f);
            c.coin = spin;
            All.Add(c);
            return c;
        }

        void Update()
        {
            if (Taken || Player == null) return;
            Vector3 d = Player.position - transform.position;
            if (Mathf.Abs(d.y) > 2f) return;
            d.y = 0;
            if (d.sqrMagnitude < 1.3f * 1.3f) StartCoroutine(Take());
        }

        IEnumerator Take()
        {
            Taken = true;
            Collected++;
            bool hidden = !string.IsNullOrEmpty(hiddenPoi);
            Accessories.Wallet += hidden ? 3 : 1;   // 存起來，主選單的趣趣商店可以換配件
            if (hidden) { Accessories.UnlockHidden(hiddenPoi); Sfx.Fanfare(); }
            else Sfx.Pop(1f + Collected * 0.06f);
            Fx.Sparkle(coin.position, hidden ? 50 : 26);
            if (DialogueUI.Instance) DialogueUI.Instance.OnStamp(hidden ? hiddenPoi : null);
            if (Collected == Total)
            {
                Sfx.Fanfare();
                Fx.Confetti(Player.position + Vector3.up * 1.5f);
            }
            // 往上飛、變小、消失
            Vector3 start = coin.localPosition;
            for (float t = 0; t < 1f; t += Time.deltaTime * 3f)
            {
                coin.localPosition = start + Vector3.up * t * 1.5f;
                coin.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.5f) * (1f - t);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        public static Collectible Nearest(Vector3 from)
        {
            Collectible best = null;
            float bd = float.MaxValue;
            foreach (var c in All)
            {
                if (c == null || c.Taken) continue;
                float d = (c.transform.position - from).sqrMagnitude;
                if (d > 70f * 70f) continue; // 只找同一站裡的
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }
    }
}
