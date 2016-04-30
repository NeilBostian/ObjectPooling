using UnityEngine;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace ObjectPooling
{
    // This class will take InstantiationData
    // from the creating client and send it to
    // all other clients in the room.
    public static class ObjectPoolRpcManager
    {
        //This is the event code that we use within Photon to implement a custom RPC with PhotonNetwork.RaiseEvent()
        public const byte CustomRpcEventCode = (byte)199;

        private static Dictionary<int, Hashtable> InstantiateDatas;

        public static void Initialize()
        {
            PhotonNetwork.OnEventCall += OnReceiveRpc;
            InstantiateDatas = new Dictionary<int, Hashtable>();
        }
        public static void Uninitialize()
        {
            PhotonNetwork.OnEventCall -= OnReceiveRpc;
        }

        public static void Instantiate(InstantiationData i)
        {
            InstantiateWithMethodId(i, MethodIds.Instantiate, null);
        }
        public static void InstantiateAsSibling(InstantiationData i, int SiblingTokenId)
        {
            InstantiateWithMethodId(i, MethodIds.InstantiateAsSibling, SiblingTokenId);
        }
        public static void InstantiateAsChild(InstantiationData i, int ParentTokenId)
        {
            InstantiateWithMethodId(i, MethodIds.InstantiateAsChild, ParentTokenId);
        }
        private static void InstantiateWithMethodId(InstantiationData i, byte MethodId, object CustomData)
        {
            Hashtable bufferMask = new Hashtable();
            bufferMask[RpcKeys.ObjectId] = i.TokenId;
            InstantiateDatas[i.TokenId] = bufferMask;

            Hashtable data = new Hashtable();
            data[RpcKeys.MethodId] = MethodId;
            data[RpcKeys.ObjectId] = i.TokenId;
            data[RpcKeys.InstantiationData] = i.Hashtable;
            data[RpcKeys.MethodData] = CustomData;

            PhotonNetwork.RaiseEvent(CustomRpcEventCode, data, true, new RaiseEventOptions() { CachingOption = EventCaching.AddToRoomCache, Receivers = ReceiverGroup.Others });
        }

        public static void Delete(GameObject g)
        {
            int objectId = ObjectPool.GetPoolToken(g).tokenId;

            if (!InstantiateDatas.ContainsKey(objectId))
            {
                Debug.LogError("Trying to delete buffered Rpc for object we have no data for. Object Id=[" + objectId + "]");
            }
            else
            {
                Hashtable bufferMask = InstantiateDatas[objectId];
                PhotonNetwork.RaiseEvent(CustomRpcEventCode, bufferMask, true, new RaiseEventOptions() { CachingOption = EventCaching.RemoveFromRoomCache });
                InstantiateDatas.Remove(objectId);
            }

            Hashtable deleteHash = new Hashtable();
            deleteHash[RpcKeys.MethodId] = MethodIds.Delete;
            deleteHash[RpcKeys.ObjectId] = objectId;
            PhotonNetwork.RaiseEvent(CustomRpcEventCode, deleteHash, true, new RaiseEventOptions() { CachingOption = EventCaching.DoNotCache, Receivers = ReceiverGroup.Others });
        }

        private static void OnReceiveRpc(byte code, object content, int senderId)
        {
            if (code == CustomRpcEventCode)
            {
                if (content == null || (content as Hashtable) == null)
                {
                    Debug.LogError("Received malformed ObjectPooling Rpc from sender=[" + senderId + "]. This should never happen, check that all instantiations and deletes are called properly through ObjectPool");
                }

                Hashtable rpcData = (Hashtable)content;
                byte MethodId = (byte)rpcData[RpcKeys.MethodId];
                int ObjectId = (int)rpcData[RpcKeys.ObjectId];
                object MethodData = rpcData[RpcKeys.MethodData];

                switch (MethodId)
                {
                    case MethodIds.Instantiate:
                        {
                            InstantiationData i = new InstantiationData((Hashtable)rpcData[RpcKeys.InstantiationData]);

                            if (ObjectPool.objectPool.LogFull)
                                Debug.Log("Received RPC from " + senderId + " to instantiate object with prefabPath=[" + i.PrefabPath + "]");

                            ObjectPoolManager.Instantiate(i);
                        }
                        break;
                    case MethodIds.Delete:
                        {
                            if (ObjectPool.objectPool.LogFull)
                                Debug.Log("Received RPC from " + senderId + " to delete object with Id=[" + ObjectId + "]");
                            ObjectPoolManager.Delete(ObjectPool.Find(ObjectId));
                        }
                        break;
                    case MethodIds.InstantiateAsChild:
                        {
                            InstantiationData i = new InstantiationData((Hashtable)rpcData[RpcKeys.InstantiationData]);

                            if (ObjectPool.objectPool.LogFull)
                                Debug.Log("Received RPC from " + senderId + " to instantiate object with prefabPath=[" + i.PrefabPath + "]");

                            ObjectPoolManager.InstantiateAsChild(i, (int)MethodData);
                        }
                        break;
                    case MethodIds.InstantiateAsSibling:
                        {
                            InstantiationData i = new InstantiationData((Hashtable)rpcData[RpcKeys.InstantiationData]);

                            if (ObjectPool.objectPool.LogFull)
                                Debug.Log("Received RPC from " + senderId + " to instantiate object with prefabPath=[" + i.PrefabPath + "]");

                            ObjectPoolManager.InstantiateAsSibling(i, (int)MethodData);
                        }
                        break;
                }
            }
        }
    }

    internal static class RpcKeys
    {
        public const byte MethodId = 0;
        public const byte ObjectId = 1;
        public const byte MethodData = 2;
        public const byte InstantiationData = 3;
    }

    internal static class MethodIds
    {
        public const byte Instantiate = 1;
        public const byte Delete = 2;
        public const byte InstantiateAsChild = 3;
        public const byte InstantiateAsSibling = 4;
    }
}
