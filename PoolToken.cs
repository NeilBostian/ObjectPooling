using UnityEngine;
using System;
using System.Collections.Generic;

namespace ObjectPooling
{
    // PoolToken is accessable on any object created using ObjectPooling
    // It contains properties about the object and helper functions
    [AddComponentMenu("ObjectPooling/Pool Token")]
    public class PoolToken : MonoBehaviour
    {
        public bool IsPlayerController = false;

        //Unique ID for this item
        public int tokenId { get; private set; }
        
        //Unique ID for this object's owner
        public int ownerId { get; private set; }
        
        //Object pool type - network object, local, etc
        public PoolType poolType { get; private set; }
        
        //Resources path to the prefab which created this object
        public string poolPrefabString { get; private set; }

        //True if the current instantiation was the first time this object is used
        public bool firstInstantiate { get; internal set; }

        //Raw data used to instantiate this object
        public object[] instantiationData { get; private set; }
        
        public bool isMine { get { return ownerId == PhotonNetwork.player.ID; } }
        public GameObject playerController { get { return ManagedObjects.GetPlayerController(ownerId); } }

        internal bool usable = true;

        public static PoolToken AddPoolToken(GameObject g, string prefabString, InstantiationData i)
        {
            PoolToken token = g.GetComponent<PoolToken>();
            if (token == null)
                token = g.AddComponent<PoolToken>();

            token.ownerId = i.OwnerId;
            token.tokenId = i.TokenId;
            token.poolType = i.PoolType;
            token.poolPrefabString = prefabString;
            token.instantiationData = i.Data;
            token.usable = true;
            return token;
        }

        public List<GameObject> GetMyObjectsOfType(string prefabPath)
        {
            return ManagedObjects.GetOwnersObjectsOfType(ownerId, prefabPath);
        }

        public List<GameObject> GetMyObjectsOfType()
        {
            return ManagedObjects.GetOwnersObjectsOfType(ownerId, poolPrefabString);
        }
    }
}