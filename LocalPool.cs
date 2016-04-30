using UnityEngine;
using System.Collections.Generic;

namespace ObjectPooling
{
    public class LocalPool : Pool
    {
        // Objects instantiated locally don't need to
        // extend any Pool functionality, so this class
        // is only to provide a non-abstract pool
    }
}