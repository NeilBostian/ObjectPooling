using UnityEngine;
using System.Collections;

namespace ObjectPooling
{
    // Scripts on ObjectPool objects can inherit from
    // ObjectPooling.Monobehaviour to get easy access
    // to its PoolToken
    public class MonoBehaviour : UnityEngine.MonoBehaviour
    {
        private PoolToken _pt;
        public PoolToken poolToken
        {
            get
            {
                if (_pt == null)
                {
                    _pt = GetComponent<PoolToken>();
                    if (_pt == null)
                        Debug.LogError("Trying to access poolToken on object without one! This should never happen if an object is created through ObjectPool");
                }
                return _pt;
            }
        }

        private PhotonView _pv;
        public PhotonView photonView
        {
            get
            {
                if (_pv == null)
                {
                    _pv = GetComponent<PhotonView>();
                    if (_pv == null)
                        Debug.LogError("Trying to access photonView on object without one! Object name: " + name);
                }
                return _pv;
            }
        }
    }
}