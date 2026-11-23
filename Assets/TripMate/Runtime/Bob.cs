using UnityEngine;

namespace TripMate
{
    /// <summary>上下輕飄（車頂燈、提示圖示用）。</summary>
    public class Bob : MonoBehaviour
    {
        public float amp = 0.04f, speed = 2.5f;
        Vector3 basePos;
        void Start() { basePos = transform.localPosition; }
        void Update() { transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * speed) * amp; }
    }
}
