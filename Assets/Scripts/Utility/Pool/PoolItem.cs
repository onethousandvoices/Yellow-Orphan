using UnityEngine;

namespace YellowOrphan.Utility
{
    public class PoolItem : MonoBehaviour, IPoolable
    {
        public GameObject GO { get; private set; }

        public void Init(GameObject go)
            => GO = go;

        public void OnSpawn()
            => GO.SetActive(true);

        public void OnDespawn()
            => GO.SetActive(false);
    }
}