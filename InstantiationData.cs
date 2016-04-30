using UnityEngine;
using System.Collections.Generic;
using ExitGames.Client.Photon;

namespace ObjectPooling
{
    // This class is a container of properties built ontop of
    // Photon's Hashtable. Values in this class are used when
    // instantiating a new object, and the use of hashtable
    // allows easy serialization for passing to other
    // multiplayer clients 
    public class InstantiationData
    {
        public GameObject GameObject;

        private Hashtable _hashtable;

        public string PrefabPath { get { return (string)getVal(InstantiationDataKeys.PrefabPath); } set { _hashtable[InstantiationDataKeys.PrefabPath] = value; } }
        public Vector3 Position { get { return (Vector3)getVal(InstantiationDataKeys.Position); } set { _hashtable[InstantiationDataKeys.Position] = value; } }
        public Quaternion Rotation { get { return (Quaternion)getVal(InstantiationDataKeys.Rotation); } set { _hashtable[InstantiationDataKeys.Rotation] = value; } }
        public int PoolGroup { get { return (int)getVal(InstantiationDataKeys.PoolGroup); } set { _hashtable[InstantiationDataKeys.PoolGroup] = value; } }
        public int OwnerId { get { return (int)getVal(InstantiationDataKeys.OwnerId); } set { _hashtable[InstantiationDataKeys.OwnerId] = value; } }
        public int TokenId { get { return (int)getVal(InstantiationDataKeys.TokenId); } set { _hashtable[InstantiationDataKeys.TokenId] = value; } }
        public int[] ViewIds { get { return (int[])getVal(InstantiationDataKeys.ViewIds); } set { _hashtable[InstantiationDataKeys.ViewIds] = value; } }
        public PoolType PoolType { get { return (PoolType)getVal(InstantiationDataKeys.PoolType); } set { _hashtable[InstantiationDataKeys.PoolType] = value; } }
        public object[] Data { get { return (object[])getVal(InstantiationDataKeys.Data); } set { _hashtable[InstantiationDataKeys.Data] = value; } }

        public Hashtable Hashtable { get { return _hashtable; } }

        public InstantiationData()
        {
            _hashtable = new Hashtable();
        }

        public InstantiationData(Hashtable h)
        {
            _hashtable = h;
        }

        public object getVal(byte key)
        {
            if (_hashtable.ContainsKey(key))
                return _hashtable[key];
            return null;
        }

        public static InstantiationData Null(PoolType instantiationType, string prefabString)
        {
            InstantiationData i = new InstantiationData();
            i.Data = null;
            i.OwnerId = 0;
            i.PoolGroup = 0;
            i.PoolType = instantiationType;
            i.Position = Vector3.zero;
            i.Rotation = Quaternion.identity;
            i.PrefabPath = prefabString;
            i.TokenId = 0;
            i.ViewIds = null;
            return i;

        }
    }

    public static class InstantiationDataKeys
    {
        public const byte PrefabPath = 1;
        public const byte Position = 2;
        public const byte Rotation = 3;
        public const byte PoolGroup = 4;
        public const byte OwnerId = 5;
        public const byte TokenId = 6;
        public const byte ViewIds = 7;
        public const byte PoolType = 8;
        public const byte Data = 9;
    }
}
