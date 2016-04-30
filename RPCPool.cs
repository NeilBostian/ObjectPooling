using UnityEngine;
using System.Collections.Generic;

namespace ObjectPooling
{
    public class RPCPool : Pool
    {
        // RPC objects are effectively
        // local objects from Photon's
        // point of view, so they don't
        // need any special functionality
    }
}