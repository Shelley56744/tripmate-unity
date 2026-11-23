using UnityEngine;

namespace TripMate
{
    /// <summary>持續旋轉。</summary>
    public class Spin : MonoBehaviour
    {
        public Vector3 speed = new Vector3(0, 90, 0);
        void Update() { transform.Rotate(speed * Time.deltaTime, Space.Self); }
    }
}
