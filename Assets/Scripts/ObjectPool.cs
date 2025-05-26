using UnityEngine;
using System.Collections.Generic;

namespace MahjongGame
{
    public class ObjectPool
    {
        private Queue<GameObject> pool = new Queue<GameObject>();
        private GameObject prefab;
        private Transform parent;

        public ObjectPool(GameObject prefab, Transform parent, int initialSize)
        {
            this.prefab = prefab;
            this.parent = parent;
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = GameObject.Instantiate(prefab, parent);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        public GameObject Get()
        {
            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                obj.SetActive(true);
                return obj;
            }
            return GameObject.Instantiate(prefab, parent);
        }

        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}