using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YellowOrphan.Utility
{
    public class PoolMaster : MonoBehaviour
    {
        [SerializeField] private PreInstalledPool[] _prePopulatedPools;
        
        private static readonly List<Pool> _pools = new List<Pool>();
        private static PoolMaster _instance;
        
        private void Awake()
        {
            _instance = this;

            foreach (PreInstalledPool pool in _prePopulatedPools)
            {
               Pool newPool = CreateNewPool(pool.Prefab);
               newPool.Populate(pool.Count);
               _pools.Add(newPool);
            }
        }

        public static T Spawn<T>(T component, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion)) where T : Component
            => SpawnInner(component.gameObject, position, rotation, null).GetComponent<T>();

        public static T Spawn<T>(T component, Transform parent, Quaternion rotation = default(Quaternion)) where T : Component
        {
            Vector3 position = parent != null
                                   ? parent.position
                                   : Vector3.zero;

            return SpawnInner(component.gameObject, position, rotation, parent).GetComponent<T>();
        }

        public static GameObject Spawn(GameObject toSpawn, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion))
            => SpawnInner(toSpawn, position, rotation, null);

        public static GameObject Spawn(GameObject toSpawn, Transform parent, Quaternion rotation = default(Quaternion))
        {
            Vector3 position = parent != null
                                   ? parent.position
                                   : Vector3.zero;

            return SpawnInner(toSpawn, position, rotation, parent);
        }

        public static void Despawn(GameObject toDespawn)
        {
            Pool pool = GetPool(toDespawn);
            pool.Add(toDespawn);
        }

        private static GameObject SpawnInner(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            Pool pool = GetPool(prefab);
            PoolItem item = pool.GetFree();
            if (parent != null) 
                item.GO.transform.SetParent(parent);
            item.GO.transform.position = position;
            item.GO.transform.rotation = rotation;

            return item.GO;
        }

        private static Pool GetPool(GameObject obj)
        {
            for (int i = 0; i < _pools.Count; i++)
            {
                Pool pool = _pools[i];
                if (PrefabUtility.GetPrefabInstanceHandle(pool.Prefab) != PrefabUtility.GetPrefabInstanceHandle(obj))
                    continue;
                return pool;
            }

            return CreateNewPool(obj);
        }

        private static Pool CreateNewPool(GameObject obj)
        {
            Pool newPool = new GameObject($"[Pool] {obj.name}").AddComponent<Pool>();
            newPool.Init(obj, _instance.transform);
            _pools.Add(newPool);
            return newPool;
        }
    }

    public interface IPoolable
    {
        public void OnSpawn();
        public void OnDespawn();
    }

    [Serializable]
    public struct PreInstalledPool
    {
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public int Count { get; private set; }
    }
}