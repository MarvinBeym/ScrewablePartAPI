using HutongGames.PlayMaker;
using MSCLoader;
using ScrewablePartAPI.New;
using System;
using System.Linq;
using UnityEngine;

namespace ScrewablePartAPI
{
    public class NewScrewableLogic : MonoBehaviour
    {
        private NewScrew[] screws;
        private GameObject parent;
        private Collider parentCollider;
        private NewScrewablePart screwablePart;

        private FsmFloat _wrenchSize;
        private FsmFloat _boltingSpeed;

        private bool toolInHand = false;
        private bool ratchetInHand = false;
        private bool ratchetSwitch = false;

        private GameObject previousHitObject;
        private GameObject spannerRatchetGameObject;
        private float screwingTimer;

        public bool partFixed = false;

        public void Init(NewScrew[] screws, NewScrewablePart.ScrewableInitBaseInfo baseInfo, GameObject parent, NewScrewablePart screwablePart)
        {
            this.screws = screws;
            this.parent = parent;
            parentCollider = parent.GetComponent<Collider>();
            this.screwablePart = screwablePart;
        }
        // Use this for initialization
        void Start()
        {
            GameObject selectedItem = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SelectItem");

            FsmHook.FsmInject(selectedItem, "Hand", new Action(delegate () { toolInHand = false; }));
            FsmHook.FsmInject(selectedItem, "Tools", new Action(delegate () { toolInHand = true; }));

            _boltingSpeed = PlayMakerGlobals.Instance.Variables.GetFsmFloat("BoltingSpeed");
            _wrenchSize = PlayMakerGlobals.Instance.Variables.GetFsmFloat("ToolWrenchSize");
        }

        // Update is called once per frame
        void Update()
        {
            GameObject screw = DetectScrew();
            if(screw != null)
            {
                DetectScrewing(screw);
            }
        }

        private void DetectScrewing(GameObject hitScrew)
        {
            if (spannerRatchetGameObject == null)
            {
                spannerRatchetGameObject = GameObject.Find("2Spanner");
            }

            if (spannerRatchetGameObject != null)
            {
                Component[] comps = spannerRatchetGameObject.GetComponentsInChildren<Transform>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i].name == "Spanner")
                    {
                        ratchetInHand = false;
                        break;

                    }
                    else if (comps[i].name == "Ratchet")
                    {
                        ratchetInHand = true;

                        ratchetSwitch = PlayMakerFSM.FindFsmOnGameObject(GameObject.Find("Ratchet"), "Switch").FsmVariables.GetFsmBool("Switch").Value;

                        break;
                    }
                }
            }

            string screwName = hitScrew.name.Substring(hitScrew.name.LastIndexOf("_SCREW"));
            int index = Convert.ToInt32(screwName.Replace("_SCREW", "")) - 1;

            int wrenchSize = Mathf.RoundToInt(this._wrenchSize.Value * 10f);

            if (wrenchSize < 0)
            {
                return;
            }

            try
            {
                if ((bool)ScrewablePart.showScrewSize.Value)
                {
                    ScrewInfo screwInfo = hitScrew.GetComponent<ScrewInfo>();
                    if (screwInfo == null)
                    {
                        return;
                    }
                    ScrewablePart.GuiInteraction = "Screw size: " + screwInfo.size;
                }
            }
            catch { }


            int screwSize = screws[index].size;
            if(wrenchSize != screwSize)
            {
                return;
            }

            //Highlighting the currently aimed at screw
            MeshRenderer renderer = hitScrew.GetComponentInChildren<MeshRenderer>();
            renderer.material.shader = Shader.Find("GUI/Text Shader");
            renderer.material.SetColor("_Color", Color.green);

            screwingTimer += Time.deltaTime;

            if (Input.GetAxis("Mouse ScrollWheel") > 0f && screwingTimer >= _boltingSpeed.Value) // forward
            {
                screwingTimer = 0;
                if (ratchetInHand)
                {
                    if (!ratchetSwitch)
                    {
                        screwablePart.ScrewOut(hitScrew, index);
                    }
                    else
                    {
                        screwablePart.ScrewIn(hitScrew, index);
                    }
                }
                else
                {
                    screwablePart.ScrewIn(hitScrew, index);
                }
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f && screwingTimer >= _boltingSpeed.Value) // backwards
            {
                screwingTimer = 0;
                if (ratchetInHand)
                {
                    if (!ratchetSwitch)
                    {
                        screwablePart.ScrewOut(hitScrew, index);
                    }
                    else
                    {
                        screwablePart.ScrewIn(hitScrew, index);
                    }
                }
                else
                {
                    screwablePart.ScrewOut(hitScrew, index);
                }
            }

            if (screws.All(screw => screw.tightness == NewScrewablePart.maxTightness) && !partFixed)
            {
                parentCollider.enabled = false;
                partFixed = true;
            }
            else if (!partFixed)
            {
                parentCollider.enabled = true;
            }
        }

        private GameObject DetectScrew()
        {
            RaycastHit hit;
            GameObject hitObject = null;
            if (toolInHand && Camera.main != null && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1f, 1 << LayerMask.NameToLayer("DontCollide")) != false)
            {
                hitObject = hit.collider?.gameObject;
                if (!hitObject.name.Contains("SCREW") || !hitObject.name.Contains(screwablePart.parent.name))
                {
                    hitObject = null;
                }
            }
            else
            {
                //Resetting color when no longer aiming at screw
                if (hitObject != null)
                {
                    MeshRenderer renderer = previousHitObject.GetComponentInChildren<MeshRenderer>();
                    renderer.material = screwablePart.baseInfo.material;
                    hitObject = null;
                }
                if (previousHitObject != null)
                {
                    MeshRenderer renderer = previousHitObject.GetComponentInChildren<MeshRenderer>();
                    renderer.material = screwablePart.baseInfo.material;
                    previousHitObject = null;
                }

            }

            if (hitObject != null)
            {

                if (previousHitObject != hitObject)
                {
                    //Resetting the previous aimed at screw
                    if (previousHitObject != null)
                    {
                        MeshRenderer renderer = previousHitObject.GetComponentInChildren<MeshRenderer>();
                        renderer.material = screwablePart.baseInfo.material;
                    }
                    previousHitObject = hitObject;
                }
                else
                {
                    return hitObject;
                }
            }
            return null;
        }
    }
}