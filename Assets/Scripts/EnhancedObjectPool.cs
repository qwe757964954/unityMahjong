
// ========== Enhanced Object Pool ==========
using UnityEngine;
using System.Collections.Generic;

namespace MahjongGame
{
    public class EnhancedObjectPool
    {
        private Queue<GameObject> pool = new Queue<GameObject>();
        private HashSet<GameObject> activeObjects = new HashSet<GameObject>();
        private GameObject prefab;
        private Transform parent;
        private int maxSize;

        public int PoolSize => pool.Count;
        public int ActiveCount => activeObjects.Count;

        public EnhancedObjectPool(GameObject prefab, Transform parent, int initialSize, int maxSize = -1)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxSize = maxSize > 0 ? maxSize : initialSize * 2;
            
            // Pre-allocate objects
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        private GameObject CreateNewObject()
        {
            GameObject obj = GameObject.Instantiate(prefab, parent);
            obj.SetActive(false);
            pool.Enqueue(obj);
            return obj;
        }

        public GameObject Get()
        {
            GameObject obj;
            
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else if (activeObjects.Count < maxSize)
            {
                obj = CreateNewObject();
                pool.Dequeue(); // Remove from pool since we're using it
            }
            else
            {
                Debug.LogWarning("Object pool has reached maximum capacity!");
                return null;
            }
            
            obj.SetActive(true);
            activeObjects.Add(obj);
            return obj;
        }

        public bool Return(GameObject obj)
        {
            if (obj == null || !activeObjects.Contains(obj))
            {
                return false;
            }
            
            activeObjects.Remove(obj);
            obj.SetActive(false);
            obj.transform.SetParent(parent);
            pool.Enqueue(obj);
            return true;
        }

        public void ReturnAll()
        {
            var objectsToReturn = new List<GameObject>(activeObjects);
            foreach (var obj in objectsToReturn)
            {
                Return(obj);
            }
        }
    }
}