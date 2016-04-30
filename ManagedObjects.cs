using UnityEngine;
using System;
using System.Collections.Generic;

namespace ObjectPooling
{
    // ManagedObjects keeps track of objects created
    // through ObjectPool. It provides helper functions
    // to easily access objects owned by a specific 
    // client, or objects created from a certain prefab
    internal static class ManagedObjects
    {
        private static Dictionary<int, GameObject> AllObjects = new Dictionary<int, GameObject>();
        private static Dictionary<string, List<GameObject>> AllObjectsByType = new Dictionary<string, List<GameObject>>();
        private static Dictionary<int, List<GameObject>> PlayerObjects = new Dictionary<int, List<GameObject>>();
        private static Dictionary<int, GameObject> PlayerControllers = new Dictionary<int, GameObject>();

        public static void Initialize()
        {
            AllObjects = new Dictionary<int, GameObject>();
            AllObjectsByType = new Dictionary<string, List<GameObject>>();
            PlayerObjects = new Dictionary<int, List<GameObject>>();
            PlayerControllers = new Dictionary<int, GameObject>();
        }
        public static void Uninitialize()
        {
            AllObjects.Clear();
            AllObjectsByType.Clear();
            PlayerObjects.Clear();
            PlayerControllers.Clear();
        }

        internal static void AddManagedObject(GameObject g)
        {
            PoolToken token = ObjectPool.GetPoolToken(g);

            //all
            if (AllObjects.ContainsKey(token.tokenId))
            {
                throw new Exception("Trying to add object with existing Id. New Object: " + g.name + " | Current Object: " + AllObjects[token.tokenId]);
            }
            AllObjects[token.tokenId] = g;

            //all by type
            if (!AllObjectsByType.ContainsKey(token.poolPrefabString))
            {
                AllObjectsByType[token.poolPrefabString] = new List<GameObject>();
            }
            AllObjectsByType[token.poolPrefabString].Add(g);

            //player objects
            if (!PlayerObjects.ContainsKey(token.ownerId))
            {
                PlayerObjects[token.ownerId] = new List<GameObject>();
            }
            PlayerObjects[token.ownerId].Add(g);

            //player controller
            if (token.IsPlayerController)
            {
                if (PlayerControllers.ContainsKey(token.ownerId))
                {
                    throw new Exception("Trying to add player controller for player ID " + token.ownerId + " but one already exists.");
                }
                else {
                    PlayerControllers[token.ownerId] = g;
                }
            }
        }
        internal static void RemoveManagedObject(GameObject g)
        {
            PoolToken token = ObjectPool.GetPoolToken(g);

            //all
            if (!AllObjects.ContainsKey(token.tokenId))
            {
                throw new Exception("Trying to delete nonexistent object with name: " + g.name);
            }
            AllObjects.Remove(token.tokenId);

            //all by type
            if (AllObjectsByType.ContainsKey(token.poolPrefabString))
            {
                bool containsG = AllObjectsByType[token.poolPrefabString].Contains(g);
                if (containsG)
                {
                    AllObjectsByType[token.poolPrefabString].Remove(g);
                    if (AllObjectsByType[token.poolPrefabString].Count == 0)
                    {
                        AllObjectsByType.Remove(token.poolPrefabString);
                    }
                }
                else {
                    Debug.LogError("Error removing managed game object. This should never happen, make sure you instantiate and delete everything properly.");
                }
            }
            else {
                Debug.LogError("Error removing managed game object. This should never happen, make sure you instantiate and delete everything properly.");
            }

            //player objects
            if (PlayerObjects.ContainsKey(token.ownerId))
            {
                bool containsG = PlayerObjects[token.ownerId].Contains(g);
                if (containsG)
                {
                    PlayerObjects[token.ownerId].Remove(g);
                    if (PlayerObjects[token.ownerId].Count == 0)
                    {
                        PlayerObjects.Remove(token.ownerId);
                    }
                }
                else {
                    Debug.LogError("Error removing managed game object. This should never happen, make sure you instantiate and delete everything properly.");
                }
            }
            else {
                Debug.LogError("Error removing managed game object. This should never happen, make sure you instantiate and delete everything properly.");
            }

            //player controller
            if (token.IsPlayerController)
            {
                if (!PlayerControllers.ContainsKey(token.ownerId))
                {
                    Debug.LogError("Error removing managed game object. This should never happen, make sure you instantiate and delete everything properly.");
                }
                else {
                    PlayerControllers.Remove(token.ownerId);
                }
            }
        }

        public static GameObject Find(int tokenId)
        {
            if (AllObjects.ContainsKey(tokenId))
            {
                return AllObjects[tokenId];
            }
            else {
                return null;
            }
        }
        public static List<GameObject> GetAllObjects()
        {
            List<GameObject> ret = new List<GameObject>();
            foreach (KeyValuePair<int, GameObject> kvp in AllObjects)
            {
                ret.Add(kvp.Value);
            }
            return ret;
        }
        public static List<GameObject> GetObjectsOfType(string name)
        {
            if (AllObjectsByType.ContainsKey(name))
            {
                return AllObjectsByType[name];
            }
            else
            {
                return null;
            }
        }
        public static List<GameObject> GetOwnersObjects(int ownerId)
        {
            if (PlayerObjects.ContainsKey(ownerId))
            {
                return PlayerObjects[ownerId];
            }
            else {
                return new List<GameObject>();
            }
        }
        public static GameObject GetPlayerController(int ownerId)
        {
            if (PlayerControllers.ContainsKey(ownerId))
            {
                return PlayerControllers[ownerId];
            }
            else {
                return null;
            }
        }
        public static List<GameObject> GetOwnersObjectsOfType(int ownerId, string name)
        {
            var g = ObjectPool.GetPool(name, PoolType.Local).GetPlayerObjects(ownerId);
            g.AddRange(ObjectPool.GetPool(name, PoolType.PhotonView).GetPlayerObjects(ownerId));
            g.AddRange(ObjectPool.GetPool(name, PoolType.RPC).GetPlayerObjects(ownerId));
            return g;
        }
    }
}