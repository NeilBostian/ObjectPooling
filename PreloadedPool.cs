using UnityEngine;
using System.Collections;
using ObjectPooling;

[AddComponentMenu("ObjectPooling/Preloaded Pool")]
public class PreloadedPool : UnityEngine.MonoBehaviour
{
    // PreloadedPool is a monobehaviour intended to be
    // manually applied to game objects in the scene.
    // On awake, ObjectPool searches for any child
    // with a PreloadedPool script(s) and creates a
    // pool with properties using these values.

    public int MaxPoolSize;
    public int PreloadSize;
    public PoolType PoolType;
    public string PrefabString;
}
