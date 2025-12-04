using UnityEngine;
using System; // [修改] 為了 Action 事件

namespace RPG
{
    /// <summary>掛在角色或敵人身上的主屬性容器</summary>
    public class MainPointComponent : MonoBehaviour
    {
        // [新增] 屬性變更事件，讓 PlayerStats 等外部組件可以訂閱
        public event Action OnStatChanged;

        // [新增] 定義基礎值常數
        public const float BASE_VALUE = 10f;

        [Header("主屬性")]
        public float Attack = 0f;
        public float Defense = 0f;
        public float Agility = 0f;
        public float Technique = 0f;
        public float HPStat = 0f;
        public float MPStat = 0f;

        public MainPoint MP => new MainPoint
        {
            Attack = Attack + BASE_VALUE,
            Defense = Defense + BASE_VALUE,
            Agility = Agility + BASE_VALUE,
            Technique = Technique + BASE_VALUE,
            HPStat = HPStat + BASE_VALUE,
            MPStat = MPStat + BASE_VALUE
        };

        // [新增] 供存檔管理器呼叫：一次覆寫所有屬性並觸發更新
        public void LoadStats(float atk, float def, float agi, float tec, float hpSt, float mpSt)
        {
            Attack = atk;
            Defense = def;
            Agility = agi;
            Technique = tec;
            HPStat = hpSt;
            MPStat = mpSt;

            // 讀檔完畢後，通知 PlayerStats 更新血魔上限
            OnStatChanged?.Invoke();
        }

        // [新增] 判斷門檻用的純加點屬性 (不含基礎值) -> 用於 SkillData.MeetsRequirement
        public MainPoint AddedPoints => new MainPoint
        {
            Attack = Attack,
            Defense = Defense,
            Agility = Agility,
            Technique = Technique,
            HPStat = HPStat,
            MPStat = MPStat
        };

        // [新增] 供 PlayerStats 讀取的總血/魔量屬性
        public float TotalHPStat => HPStat + BASE_VALUE;
        public float TotalMPStat => MPStat + BASE_VALUE;

        /// <summary>依防禦減傷（線性反向，或改 Balance）</summary>
        // ... (AfterDefense 方法保持不變，注意如果它用到 Defense 欄位，請改用 MP.Defense 或是 (Defense + BASE_VALUE))
        public float AfterDefense(float outgoingDamage)
        {
            // [修正] 這裡要用總防禦力 (加點 + 基礎)
            float totalDef = Defense + BASE_VALUE;
            float reduceRatio = Balance.DefenseReduction(totalDef);
            return Mathf.Max(0f, outgoingDamage * (1f - reduceRatio));
        }
        /// <summary>
        /// 嘗試增加指定屬性 1 點。
        /// </summary>
        // ... (TryIncrementStat 方法保持不變，它會增加 Attack 等欄位，也就是增加分配點數)
        public bool TryIncrementStat(string statName, float amount = 1f)
        {
            switch (statName.ToLower())
            {
                case "attack": Attack += amount; break;
                case "defense": Defense += amount; break;
                case "agility": Agility += amount; break;
                case "technique": Technique += amount; break;
                case "hpstat": HPStat += amount; break;
                case "mpstat": MPStat += amount; break;
                default: return false;
            }
            OnStatChanged?.Invoke();
            return true;
        }
    }
}
