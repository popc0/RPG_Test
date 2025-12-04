// File: ObjectPool.cs (新增檔案)
using UnityEngine;
using System.Collections.Generic;

namespace RPG
{
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        // Pool Map: 儲存不同 Prefab 對應的物件池
        private Dictionary<GameObject, List<GameObject>> poolDictionary;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                poolDictionary = new Dictionary<GameObject, List<GameObject>>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 獲取一個物件。如果池子裡有閒置的，就拿出來；沒有則新創建。
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(prefab))
            {
                // 如果是第一次使用這個 Prefab，先創建一個空列表
                poolDictionary.Add(prefab, new List<GameObject>());
            }

            List<GameObject> pool = poolDictionary[prefab];
            GameObject objToSpawn = null;

            // 1. 尋找閒置的物件
            for (int i = 0; i < pool.Count; i++)
            {
                if (!pool[i].activeInHierarchy)
                {
                    objToSpawn = pool[i];
                    break;
                }
            }

            // 2. 如果沒有閒置的，則創建一個新的
            if (objToSpawn == null)
            {
                objToSpawn = Instantiate(prefab, transform); // 將新物件設為 Pool Manager 的子物件
                pool.Add(objToSpawn);
            }

            // 3. 設定並啟用物件
            objToSpawn.transform.position = position;
            objToSpawn.transform.rotation = rotation;
            objToSpawn.SetActive(true);

            return objToSpawn;
        }

        /// <summary>
        /// 回收物件。將其禁用並放回池子中。
        /// </summary>
        public void Despawn(GameObject obj)
        {
            if (obj == null || !obj.activeInHierarchy) return;

            // 禁用並重設父級 (如果需要)
            obj.SetActive(false);
            // 由於 Spawn 時已設為子物件，通常不需要額外重設父級
        }
    }
}