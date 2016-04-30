using UnityEngine;
using System.Collections.Generic;
using System;

namespace ObjectPooling
{
    // Objects using PhotonInstantiate are expected to have
    // PhotonViews. Typically, PhotonNetwork.Instantiate will
    // assign view ids, but we have to do it manually since
    // you can't use PhotonNetwork.Instantiate with an object
    // pool as it creates a new object on its own

    public class PhotonPool : Pool
    {
        void PreObjectActivate(InstantiationData i)
        {
            var g = i.GameObject;

            if (g == null)
                Debug.LogError("Prefab not found at prefabPath=[" + i.PrefabPath + "]");

            PhotonView[] pvs = g.GetComponents<PhotonView>();

            foreach (PhotonView pv in pvs)
            {
                pv.enabled = false;
            }

            PhotonView[] photonViews = g.GetComponents<PhotonView>();

            if (photonViews.Length < 1)
            {
                throw new Exception("No PhotonViews found on prefab at prefabPath=[" + i.PrefabPath + "]");
            }

            if (i.ViewIds != null) //set appropriate view ids for the components in this object
            {
                if (photonViews.Length < i.ViewIds.Length)
                {
                    Debug.LogWarning("Received more viewIds for object instantiation than there were photonViews");
                }
                else
                {
                    for (int ind = 0; ind < i.ViewIds.Length; ind++)
                    {
                        if (photonViews.Length > i.ViewIds.Length)
                        {
                            Debug.LogError("Can't assign null viewId to photon view");
                        }
                        else
                        {
                            PhotonView pv = photonViews[ind];
                            pv.ownerId = i.OwnerId;
                            pv.viewID = i.ViewIds[ind];
                            pv.enabled = true;
                        }
                    }
                }
            }
            else if (i.OwnerId == PhotonNetwork.player.ID) //no view ids provided, create them for ourself
            {
                for (int ind = 0; ind < photonViews.Length; ind++)
                {
                    PhotonView pv = photonViews[ind];
                    pv.ownerId = i.OwnerId;
                    pv.viewID = PhotonNetwork.AllocateViewID();
                    pv.enabled = true;
                }
            }
        }

        void PreObjectDeactivate(GameObject g)
        {
            PhotonView[] pvs = g.GetComponents<PhotonView>();
            foreach (PhotonView pv in pvs)
            {
                PhotonNetwork.networkingPeer.LocalCleanPhotonView(pv);
            }
        }
    }
}