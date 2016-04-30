# ObjectPooling
A Unity object pool with support for multiplayer pooling using Photon Unity Networking (PUN)

This library was originally created out of necessity for an object pool that worked with PUN. Objects can be created by calling the appropriate ObjectPool.Instantiate function, and delted with ObjectPool.delete. In addition to being an object pool, this library provides many utility functions for managing or getting objects - especially in a network environment.

All objects have the following:
- A unique integer ID
- A "type" - denoted by the prefab path they were instantiated with
- PoolToken, a MonoBehaviour which contains all ObjectPool related information

Additionally, ObjectPooling.ManagedObjects can return specific GameObjects or groups of them:
- Find by ID
- Find all by type
- Find all by owner
- Find all by type and owner

Finally, users can mark a specific prefab as a "PlayerController". There is expected to be no more than 1 player controller per-user per-client, and if a player controller exists it can be easily referenced through PoolToken.playerController or ManagedObjects.GetPlayerController()
