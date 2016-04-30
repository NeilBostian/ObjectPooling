using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using ObjectPooling;

// This is the primary class users will deal with
// when using ObjectPooling. It takes necessary
// user data, constructs an InstantiationData 
// container, syncs these over the network if necessary
// and finally returns an object of the requested type

public sealed class ObjectPool : UnityEngine.MonoBehaviour
{
    //ObjectPool
    //---ObjectType1
    //-------Object1, Object1, Object1
    //---ObjectType2
    //-------Object2, Object2, Object2

    public static ObjectPool objectPool { get; private set; }
    public static readonly int MAX_ITEMS = 10000;
    private static Dictionary<string, Pool> LocalPools;
    private static Dictionary<string, Pool> PhotonViewPools;
    private static Dictionary<string, Pool> RPCPools;
    private static int itemsCreated = 0;

    public bool PersistThroughLoad = true; //If true, this game object will persist through scene changes, including any inactive objects in the pool
    public bool AutoNameObjects = true; //If true, objects created using object pool will be automatically named
    public bool LogFull = false; //If true, debug info will be logged to the console
    public int DefaultMaxPoolSize = 100; //Objects added to a pool beyond its max size will be permanently deleted

    void Awake()
    {
        if (PersistThroughLoad)
            DontDestroyOnLoad(this);
        
        ObjectPoolRpcManager.Initialize();
        ObjectPoolManager.Initialize();
        ManagedObjects.Initialize();

        itemsCreated = 0;
        LocalPools = new Dictionary<string, Pool>();
        PhotonViewPools = new Dictionary<string, Pool>();
        RPCPools = new Dictionary<string, Pool>();
        objectPool = this;

        PreloadedPool[] poolsArray = GetComponentsInChildren<PreloadedPool>();

        if (LogFull)
            Debug.Log("Found " + poolsArray.Length + " pools to preload.");

        List<PreloadedPool> pools = new List<PreloadedPool>(poolsArray);
        while (pools.Count > 0)
        {
            PreloadedPool p = pools[0];
            Pool pool = Pool.InitializePool(this, p.PrefabString, p.PoolType, p.MaxPoolSize, p.PreloadSize, p.gameObject);
            PoolFromPoolType(p.PoolType)[p.PrefabString] = pool;
            pool.RenameGameObject();
            pools.Remove(p);
            Destroy(p);
        }
    }
    void OnDestroy()
    {
        ObjectPoolRpcManager.Uninitialize();
        ObjectPoolManager.Uninitialize();
        ManagedObjects.Uninitialize();
    }

    public static GameObject LocalInstantiate(string prefabPath, Vector3 position, Quaternion rotation)
    {
        return LocalInstantiate(prefabPath, position, rotation, null);
    }
    public static GameObject LocalInstantiate(string prefabPath, Vector3 position, Quaternion rotation, object[] data)
    {
        InstantiationData i = new InstantiationData();
        i.PrefabPath = prefabPath;
        i.Position = position;
        i.Rotation = rotation;
        i.PoolType = PoolType.Local;
        i.TokenId = AllocateObjectId();
        i.OwnerId = PhotonNetwork.player.ID;
        i.ViewIds = null;
        i.Data = data;

        return ObjectPoolManager.Instantiate(i);
    }

    public static GameObject PhotonRpcInstantiate(string prefabPath, Vector3 position, Quaternion rotation)
    {
        return PhotonRpcInstantiate(prefabPath, position, rotation, null);
    }
    public static GameObject PhotonRpcInstantiate(string prefabPath, Vector3 position, Quaternion rotation, object[] data)
    {
        InstantiationData i = InstantiationData(prefabPath, position, rotation, data);
        i.PoolType = PoolType.RPC;

        var g = ObjectPoolManager.Instantiate(i);
        ObjectPoolRpcManager.Instantiate(i);
        return g;
    }
    public static GameObject PhotonRpcInstantiateAsChild(string prefabPath, Vector3 position, Quaternion rotation, object[] data, int parentTokenId)
    {
        InstantiationData i = InstantiationData(prefabPath, position, rotation, data);
        i.PoolType = PoolType.RPC;

        var g = ObjectPoolManager.Instantiate(i);
        g.transform.parent = ManagedObjects.Find(parentTokenId).transform;
        ObjectPoolRpcManager.InstantiateAsChild(i, parentTokenId);
        return g;
    }
    public static GameObject PhotonRpcInstantiateAsSibling(string prefabPath, Vector3 position, Quaternion rotation, object[] data, int siblingTokenId)
    {
        InstantiationData i = InstantiationData(prefabPath, position, rotation, data);
        i.PoolType = PoolType.RPC;

        var g = ObjectPoolManager.InstantiateAsSibling(i, siblingTokenId);
        ObjectPoolRpcManager.InstantiateAsSibling(i, siblingTokenId);
        return g;
    }

    public static GameObject PhotonViewInstantiate(string prefabPath, Vector3 position, Quaternion rotation)
    {
        return PhotonViewInstantiate(prefabPath, position, rotation, null);
    }
    public static GameObject PhotonViewInstantiate(string prefabPath, Vector3 position, Quaternion rotation, object[] data)
    {
        InstantiationData i = InstantiationData(prefabPath, position, rotation, data);
        i.PoolType = PoolType.PhotonView;

        var g = ObjectPoolManager.Instantiate(i);
        PhotonView[] pvs = g.GetComponents<PhotonView>();
        List<int> viewIds = new List<int>();
        foreach (PhotonView pv in pvs)
        {
            viewIds.Add(pv.viewID);
        }
        i.ViewIds = viewIds.ToArray();
        ObjectPoolRpcManager.Instantiate(i);
        return g;
    }

    public static void Delete(GameObject g)
    {
        PoolToken token = GetPoolToken(g);
        PoolType poolType = token.poolType;
        if (poolType == PoolType.RPC || poolType == PoolType.PhotonView)
        {
            ObjectPoolRpcManager.Delete(g);
        }
        ObjectPoolManager.Delete(g);
    }

    public static Pool GetPool(string poolPrefabPath, PoolType poolType)
    {
        Dictionary<string, Pool> PoolsCollection = PoolFromPoolType(poolType);
        if (PoolsCollection.ContainsKey(poolPrefabPath))
        {
            return PoolsCollection[poolPrefabPath];
        }
        else
        {
            var newPool = Pool.CreatePool(objectPool, poolPrefabPath, poolType, objectPool.DefaultMaxPoolSize);
            PoolsCollection[poolPrefabPath] = newPool;
            return newPool;
        }
    }
    public static PoolToken GetPoolToken(GameObject g)
    {
        PoolToken gToken = g.GetComponent<PoolToken>();
        if (gToken == null)
        {
            throw new Exception("Can't find PoolToken on gameObject=[" + g.name + "] Are you sure you instantiate and delete all objects properly?");
        }
        return gToken;
    }
    public static GameObject Find(int tokenId)
    {
        return ManagedObjects.Find(tokenId);
    }
    public static GameObject GetMyPlayerController()
    {
        return ManagedObjects.GetPlayerController(PhotonNetwork.player.ID);
    }
    public static List<GameObject> GetMyObjectsOfType(string prefabPath)
    {
        return ManagedObjects.GetOwnersObjectsOfType(PhotonNetwork.player.ID, prefabPath);
    }

    public static Dictionary<string, Pool> PoolFromPoolType(PoolType poolType)
    {
        Dictionary<string, Pool> PoolsCollection;
        switch (poolType)
        {
            case PoolType.Local:
                PoolsCollection = LocalPools;
                break;
            case PoolType.PhotonView:
                PoolsCollection = PhotonViewPools;
                break;
            case PoolType.RPC:
                PoolsCollection = RPCPools;
                break;
            default:
                throw new Exception("ObjectPool.getPool received invalid poolType=[" + poolType + "]");
        }
        return PoolsCollection;
    }
    public static bool PoolExists(string poolPrefabPath, PoolType poolType)
    {
        return PoolFromPoolType(poolType).ContainsKey(poolPrefabPath);
    }

    private static int AllocateObjectId()
    {
        int tries = 0;

        int returnId;
        while (!TryGetId(out returnId))
        {
            tries++;
            if (tries >= MAX_ITEMS)
                break;
        }

        if (tries >= MAX_ITEMS)
        {
            Debug.LogError("Player with Id=[" + PhotonNetwork.player.ID + "] has instantiated > " + ObjectPool.MAX_ITEMS + "(ObjectPool.MAX_ITEMS) items at once, this is not allowed.");
        }

        return returnId;
    }
    private static bool TryGetId(out int Id)
    {
        bool foundVal = false;
        int returnId = PhotonNetwork.player.ID * MAX_ITEMS + itemsCreated % (MAX_ITEMS);

        if (ManagedObjects.Find(returnId) == null)
        {
            Id = returnId;
            foundVal = true;
        }
        else
        {
            Id = -1;
            foundVal = false;
        }

        if (++itemsCreated >= MAX_ITEMS)
        {
            itemsCreated = 0;
        }
        return foundVal;
    }
    private static InstantiationData InstantiationData(string prefabPath, Vector3 position, Quaternion rotation, object[] data)
    {
        InstantiationData i = new InstantiationData();
        i.PrefabPath = prefabPath;
        i.Position = position;
        i.Rotation = rotation;
        i.TokenId = AllocateObjectId();
        i.OwnerId = PhotonNetwork.player.ID;
        i.ViewIds = null;
        i.Data = data;
        return i;
    }

    /// <summary>
    /// Don't mistake this with UnityEngine.Object.Destroy.
    /// </summary>
    public static void Destroy(GameObject g) { Delete(g); }

    public void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        while (ManagedObjects.GetOwnersObjects(otherPlayer.ID).Count > 0)
        {
            ObjectPoolManager.Delete(ManagedObjects.GetOwnersObjects(otherPlayer.ID)[0]);
        }
    }
    
    public static void LoadLevel(string LevelName)
    {
        if (objectPool != null && objectPool.PersistThroughLoad)
            CleanUpObjects();
        PhotonNetwork.LoadLevel(LevelName);
    }
    public static void LoadLevel(int LevelId)
    {
        if (objectPool != null && objectPool.PersistThroughLoad && PhotonNetwork.inRoom)
            CleanUpObjects();
        PhotonNetwork.LoadLevel(LevelId);
    }

    private static void CleanUpObjects()
    {
        List<GameObject> items = ManagedObjects.GetAllObjects();
        while (items.Count > 0)
        {
            Delete(items[0]);
            items.Remove(items[0]);
        }
    }
}