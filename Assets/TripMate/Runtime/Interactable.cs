using System.Collections.Generic;
using UnityEngine;

namespace TripMate
{
    /// <summary>
    /// 場景裡可以互動的地方（泡溫泉、拍照、搶整理券⋯）。走近時畫面下方出現「E 泡溫泉」，按 E 觸發。
    /// 真正要做什麼寫在 DialogueUI.Activities.cs，用 kind 對應。
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public string kind;
        public string prompt;
        public float radius = 2.4f;
        public bool once = true;
        public bool used;
        GameObject icon;

        public static Interactable Create(Transform parent, Vector3 localPos, string kind, string prompt, float radius = 2.4f, bool once = true)
        {
            var go = new GameObject("Interact_" + kind);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var it = go.AddComponent<Interactable>();
            it.kind = kind;
            it.prompt = prompt;
            it.radius = radius;
            it.once = once;
            // 頭上的小圖示：藍色圓點＋白色光環，表示「這裡可以做事」
            it.icon = new GameObject("Icon");
            it.icon.transform.SetParent(go.transform, false);
            it.icon.transform.localPosition = new Vector3(0, 2.6f, 0);
            TM.Prim(PrimitiveType.Sphere, "Dot", it.icon.transform, Vector3.zero, Vector3.one * 0.42f, TM.Hex("#5B8DEF"), 1.2f);
            TM.Text3D("E", it.icon.transform, new Vector3(0, -0.02f, -0.24f), 0.3f, Color.white);
            it.icon.AddComponent<FaceCamera>();
            it.icon.AddComponent<Bob>().amp = 0.1f;
            All.Add(it);
            return it;
        }

        public void MarkUsed()
        {
            if (!once) return;
            used = true;
            if (icon) icon.SetActive(false);
        }

        public static void ResetAll() { All.Clear(); }

        /// <summary>玩家附近最近、可用的互動點。</summary>
        public static Interactable Nearest(Vector3 pos)
        {
            Interactable best = null;
            float bd = float.MaxValue;
            foreach (var it in All)
            {
                if (it == null || it.used || !it.gameObject.activeInHierarchy) continue;
                Vector3 d = it.transform.position - pos; d.y = 0;
                float m = d.magnitude;
                if (m < it.radius && m < bd) { bd = m; best = it; }
            }
            return best;
        }
    }
}
