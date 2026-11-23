using UnityEngine;

namespace TripMate
{
    /// <summary>名牌永遠面向鏡頭。</summary>
    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var c = Camera.main;
            if (c) transform.rotation = c.transform.rotation;
        }
    }
}
