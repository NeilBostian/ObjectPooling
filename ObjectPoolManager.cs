using UnityEngine;
using System;
using System.Collections.Generic;

namespace ObjectPooling
{
    // This class is the only interface between
    // ObjectPooling and Pool - Everything
    // above this class in the call stack either
    // initializes InstantiationData or handles
    // network objects

    public static class ObjectPoolManager
    {

        public static void Initialize()
        {

        }

        public static void Uninitialize()
        {

        }

        public static GameObject Instantiate(InstantiationData i)
        {
            if (ObjectPool.objectPool.LogFull)
                Debug.Log("Instantiating " + i.PrefabPath);
            Pool myPool = ObjectPool.GetPool(i.PrefabPath, i.PoolType);
            var g = myPool.GetObject(i);
            return g;
        }
        public static void Delete(GameObject g)
        {
            if (ObjectPool.objectPool.LogFull)
                Debug.Log("Deleting " + g.name);
            PoolToken token = ObjectPool.GetPoolToken(g);
            Pool myPool = ObjectPool.GetPool(token.poolPrefabString, token.poolType);
            myPool.StoreObject(g);
        }

        public static GameObject InstantiateAsChild(InstantiationData i, int parentTokenId)
        {
            var go = Instantiate(i);
            var parent = ManagedObjects.Find(parentTokenId);
            if (parent != null)
            {
                go.transform.parent = parent.transform;
            }
            else
            {
                throw new Exception("Parent with Id " + parentTokenId + " not found.");
            }
            return go;
        }

        public static GameObject InstantiateAsSibling(InstantiationData i, int siblingTokenId)
        {
            var go = Instantiate(i);
            var sibling = ManagedObjects.Find(siblingTokenId);
            if (sibling != null)
            {
                go.transform.parent = sibling.transform.parent;
            }
            else
            {
                throw new Exception("Sibling with Id " + siblingTokenId + " not found.");
            }
            return go;
        }
    }
}
