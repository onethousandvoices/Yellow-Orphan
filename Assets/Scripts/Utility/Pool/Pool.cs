using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace YellowOrphan.Utility
{
    public class Pool : MonoBehaviour
    {
        [field: SerializeField, ReadOnly] public List<PoolItem> Items { get; private set; }
        public GameObject Prefab { get; private set; }
        
        private static readonly List<Component> _components = new List<Component>();

        public void Init(GameObject prefab, Transform poolMaster)
        {
            Prefab = prefab;
            Items = new List<PoolItem>(64);
            transform.SetParent(poolMaster);
        }

        public PoolItem GetFree()
        {
            PoolItem free;
            if (Items.Count <= 0)
                free = CreateNew();
            else
            {
                free = Items[0];
                Items.RemoveAt(0);
            }
            
            free.GO.GetComponentsInChildren(_components);
            foreach (Component component in _components)
            {
                if (component is IPoolable poolable)
                    poolable.OnSpawn();
            }
            
            return free;
        }

        public void Add(GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogError($"{obj.name} was null");
                return;
            }

            PoolItem item = obj.GetComponent<PoolItem>();
            item ??= obj.AddComponent<PoolItem>();
            item.Init(obj);
            
            item.GetComponentsInChildren(_components);
            foreach (Component component in _components)
            {
                if (component is IPoolable poolable)
                    poolable.OnDespawn();
            }
            
            Items.Add(item);
        }

        public void Populate(int count)
        {
            for (int i = 0; i < count; i++)
                Add(CreateNew().GO);
        }

        private PoolItem CreateNew()
        {
            if (Prefab == null)
            {
                Debug.LogError($"{name} prefab wasn't set");
                return null;
            }

            GameObject newGo = Instantiate(Prefab, transform);
            PoolItem item = newGo.AddComponent<PoolItem>();
            item.Init(newGo);
            return item;
        }
    }
}