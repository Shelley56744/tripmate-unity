using UnityEngine;

namespace TripMate
{
    /// <summary>目前的目標：畫面上方的指引箭頭、小地圖的星星都讀這裡。</summary>
    public static class Objective
    {
        public static string Label = "";
        public static Vector3? Target;

        public static void Clear()
        {
            Label = "";
            Target = null;
        }
    }
}
