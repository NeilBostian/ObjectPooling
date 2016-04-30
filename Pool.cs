using UnityEngine;
using System;
using System.Collections.Generic;

namespace ObjectPooling
{
    public enum PoolType { Local = 0x01, PhotonView = 0x02, RPC = 0x03 };

    // Base class for Pool, implements storing and retrieving objects
    // along with helper functions for finding specific objects
    // created by this pool
    public abstract class Pool : MonoBehaviour
    {
        private Dictionary<int, List<GameObject>> playerObjects;

        public string PrefabString { get; private set; }
        public PoolType PoolType { get; private set; }
        public ObjectPool PoolRoot { get; private set; }
        public int MaxPoolSize { get; private set; }

        private GameObject prefab;
        private bool prefabDefaultActive;

        public static Pool CreatePool(ObjectPool poolRoot, string poolPrefabString, PoolType poolType, int maxPoolSize)
        {
            GameObject poolGO = new GameObject();
            return InitializePool(poolRoot, poolPrefabString, poolType, maxPoolSize, 0, poolGO);
        }
        public static Pool InitializePool(ObjectPool poolRoot, string poolPrefabString, PoolType poolType, int maxPoolSize, int preloadSize, GameObject poolGameObject)
        {
            Pool p;

            switch (poolType)
            {
                case PoolType.Local:
                    p = poolGameObject.AddComponent<LocalPool>();
                    break;
                case PoolType.PhotonView:
                    p = poolGameObject.AddComponent<PhotonPool>();
                    break;
                case PoolType.RPC:
                    p = poolGameObject.AddComponent<RPCPool>();
                    break;
                default:
                    throw new Exception("Pool.CreatePool received invalid poolType=[" + poolType + "]");
            }

            p.transform.parent = poolRoot.transform;
            p.PoolRoot = poolRoot;
            p.PrefabString = poolPrefabString;
            p.PoolType = poolType;
            p.playerObjects = new Dictionary<int, List<GameObject>>();
            p.MaxPoolSize = maxPoolSize;
            p.prefab = p.getPrefabGameObject();
            p.prefabDefaultActive = p.prefab.activeSelf;
            p.RenameGameObject();
            for (int i = 0; i < preloadSize; i++)
            {
                p.LoadInactiveObject();
            }
            return p;
        }

        /// <summary>
        /// gets an object from the pool, instantiating a new gameobject if necessary
        /// </summary>
        public GameObject GetObject(InstantiationData i)
        {
            GameObject g;

            bool newObj = false;

            if (transform.childCount > 0)
            {
                int ind = 0;
                while (ind < transform.childCount)
                {
                    GameObject go = transform.GetChild(ind).gameObject;
                    PoolToken pt = go.GetComponent<PoolToken>();
                    if (pt == null || pt.usable == true)
                    {
                        g = go;
                        break;
                    }
                    ind++;
                }
                if (ind >= transform.childCount)
                {
                    throw new Exception("No usable objects in pool. This should never happen.");
                }
                g = transform.GetChild(ind).gameObject;
            }
            else
            {
                g = LoadInactiveObject();
                newObj = true;
            }
            i.GameObject = g;

            SendMessage("PreObjectActivate", i, SendMessageOptions.DontRequireReceiver);

            g.transform.parent = null;
            g.transform.position = i.Position;
            g.transform.rotation = i.Rotation;

            var rigidbody = g.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            var token = PoolToken.AddPoolToken(g, PrefabString, i);
            token.firstInstantiate = newObj;
            if (ObjectPool.objectPool.AutoNameObjects)
            {
                g.name = getPoolNameFromPrefabPath(PrefabString) + "." + i.OwnerId + "." + token.tokenId;
            }

            g.SetActive(prefabDefaultActive);
            SendMessage("PostObjectActivate", i, SendMessageOptions.DontRequireReceiver);

            if (!newObj && prefabDefaultActive)
            {
                g.BroadcastMessage("Awake", SendMessageOptions.DontRequireReceiver);
                g.BroadcastMessage("Start", SendMessageOptions.DontRequireReceiver);
            }

            addPlayerObject(i.OwnerId, g);
            ManagedObjects.AddManagedObject(g);
            g.BroadcastMessage("OnObjectPoolInstantiate", SendMessageOptions.DontRequireReceiver);
            return g;
        }
        /// <summary>
        /// stores an object in the pool, removing it from the game world
        /// </summary>
        public void StoreObject(GameObject g)
        {
            PoolToken pt = ObjectPool.GetPoolToken(g);
            SendMessage("PreObjectDeactivate", g, SendMessageOptions.DontRequireReceiver);
            g.BroadcastMessage("OnObjectPoolDestroy", SendMessageOptions.DontRequireReceiver);
            g.BroadcastMessage("OnObjectPoolDelete", SendMessageOptions.DontRequireReceiver);
            if (transform.childCount > MaxPoolSize)
            {
                pt.usable = false;
                Destroy(g);
            }
            else
            {
                g.BroadcastMessage("OnDestroy", SendMessageOptions.DontRequireReceiver);
                g.SetActive(false);
                g.transform.parent = transform;
                g.transform.position = Vector3.zero;
                g.transform.rotation = Quaternion.identity;
            }
            deletePlayerObject(g);
            ManagedObjects.RemoveManagedObject(g);
            SendMessage("PostObjectDeactivate", g, SendMessageOptions.DontRequireReceiver);
        }
        public List<GameObject> GetPlayerObjects(int ownerId)
        {
            if (playerObjects.ContainsKey(ownerId))
            {
                return playerObjects[ownerId];
            }
            return new List<GameObject>();
        }
        public void RenameGameObject()
        {
            transform.name = getPoolNameFromPrefabPath(PrefabString) + "." + PoolType;
        }

        protected GameObject LoadInactiveObject()
        {
            GameObject prefab = getPrefabGameObject();

            prefab.SetActive(false);
            GameObject instg = Instantiate(prefab);
            prefab.SetActive(prefabDefaultActive);
            instg.transform.parent = transform;
            return instg;
        }

        private void addPlayerObject(int ownerId, GameObject g)
        {
            if (!playerObjects.ContainsKey(ownerId))
            {
                playerObjects[ownerId] = new List<GameObject>();
            }
            List<GameObject> myObjects = playerObjects[ownerId];
            myObjects.Add(g);
        }
        private void deletePlayerObject(GameObject g)
        {
            PoolToken t = ObjectPool.GetPoolToken(g);
            if (playerObjects.ContainsKey(t.ownerId))
            {
                bool listContains = playerObjects[t.ownerId].Contains(g);
                if (listContains)
                {
                    playerObjects[t.ownerId].Remove(g);
                }
                else
                {
                    Debug.LogError("Tried to delete object from List but there was no GameObject g=[" + g.name + "]");
                }
            }
            else
            {
                Debug.LogError("Tried to delete object from Dictionary but there was no Key for player with ownerId=[" + t.ownerId + "]");
            }
        }
        private GameObject getPrefabGameObject()
        {
            if (prefab == null)
            {
                prefab = ((GameObject)Resources.Load(PrefabString, typeof(GameObject)));
            }
            if (prefab == null)
            {
                Debug.Log("Failed loading prefab from path. [path=" + PrefabString + "]");
            }
            return prefab;
        }

        public static string getPoolNameFromPrefabPath(string path)
        {
            return path.Substring(path.LastIndexOf("/") + 1);
        }
    }
}