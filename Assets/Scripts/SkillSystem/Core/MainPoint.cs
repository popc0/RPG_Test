using UnityEngine;

namespace RPG
{
    /// <summary>純資料結構：主屬性數值集合</summary>
    [System.Serializable]
    public struct MainPoint
    {
        public float Attack;
        public float Defense;
        public float Agility;
        public float Technique;
        public float HPStat;
        public float MPStat;

        public static MainPoint Zero => new MainPoint();
    }
}
