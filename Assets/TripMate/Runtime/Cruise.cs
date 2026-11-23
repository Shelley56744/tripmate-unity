using UnityEngine;

namespace TripMate
{
    /// <summary>觀光船在河上慢慢來回開，到底時轉頭。</summary>
    public class Cruise : MonoBehaviour
    {
        public float xMin = -30f, xMax = 30f, speed = 1.6f;
        int dir = 1;
        float turn; // 0 = 往 +X，1 = 往 -X

        void Update()
        {
            var p = transform.localPosition;
            p.x += dir * speed * Time.deltaTime;
            if (p.x > xMax) { p.x = xMax; dir = -1; }
            if (p.x < xMin) { p.x = xMin; dir = 1; }
            transform.localPosition = p;
            turn = Mathf.MoveTowards(turn, dir > 0 ? 0f : 1f, Time.deltaTime * 0.8f);
            transform.localEulerAngles = new Vector3(0, turn * 180f, 0);
        }
    }
}
