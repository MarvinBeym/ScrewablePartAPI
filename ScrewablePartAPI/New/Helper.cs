using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace ScrewablePartAPI.New
{
    public class Helper
    {
        internal static PlayMakerFSM FindFsmOnGameObject(GameObject gameObject, string fsmName)
        {
            foreach (PlayMakerFSM fSM in gameObject.GetComponents<PlayMakerFSM>())
            {
                if (fSM.FsmName == fsmName) { return fSM; }
            }
            return null;
        }
        internal static Vector3 CopyVector3(Vector3 old)
        {
            return new Vector3(old.x, old.y, old.z);
        }
        internal static GameObject SetObjectNameTagLayer(GameObject gameObject, string name, string layer = "Parts", string tag = "PART")
        {
            gameObject.name = name;
            gameObject.tag = tag;

            gameObject.layer = LayerMask.NameToLayer(layer);
            return gameObject;
        }
    }
}
