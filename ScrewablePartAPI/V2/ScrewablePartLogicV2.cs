using HutongGames.PlayMaker;
using MSCLoader;
using ScrewablePartAPI.New;
using System;
using System.Linq;
using UnityEngine;

namespace ScrewablePartAPI.V2
{
    public class ScrewablePartLogicV2 : MonoBehaviour
    {
        private enum Tool
        {
            None,
            Spanner,
            Ratchet,
        };

        internal bool partFixed = false;

        private Material screwMaterial;
        private AudioClip screwSound;

        private GameObject parent;
        private Collider parentCollider;
        private ScrewV2[] screws;
        private ScrewablePartV2 screwablePart;

        private FsmFloat _wrenchSize;
        private FsmFloat _boltingSpeed;
        private bool isToolInHand = false;
        private GameObject spanner;
        private GameObject ratchet;
        private FsmBool ratchetSwitch;
        private ScrewV2 previousScrew;
        private float screwingTimer;
        void Start()
        {
            GameObject selectedItem = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SelectItem");
            GameObject twoSpanner = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera").transform.Find("2Spanner").gameObject;
            GameObject twoSpannerPivot = twoSpanner.transform.Find("Pivot").gameObject;
            GameObject twoSpannerPick = twoSpanner.transform.Find("Pick").gameObject;

            spanner = twoSpannerPivot.transform.Find("Spanner").gameObject;
            ratchet = twoSpannerPivot.transform.Find("Ratchet").gameObject;

            ratchetSwitch = Helper.FindFsmOnGameObject(ratchet, "Switch").FsmVariables.FindFsmBool("Switch");

            FsmHook.FsmInject(selectedItem, "Hand", new Action(delegate () { isToolInHand = false; }));
            FsmHook.FsmInject(selectedItem, "Tools", new Action(delegate () { isToolInHand = true; }));

            _boltingSpeed = PlayMakerGlobals.Instance.Variables.GetFsmFloat("BoltingSpeed");
            _wrenchSize = PlayMakerGlobals.Instance.Variables.GetFsmFloat("ToolWrenchSize");
        }

        internal void Init(ScrewableBaseInfo baseInfo, GameObject parent, ScrewV2[] screws, ScrewablePartV2 screwablePart)
        {
            screwMaterial = baseInfo.material;
            screwSound = baseInfo.soundClip;
            this.parent = parent;
            this.screws = screws;
            this.screwablePart = screwablePart;
            parentCollider = parent.GetComponent<Collider>();
        }


        // Update is called once per frame
        void Update()
        {
            if(!isToolInHand || (!spanner.activeSelf && !ratchet.activeSelf)) { return; }
            ScrewV2 screw = DetectScrew();
            if(screw != null)
            {
                int wrenchSize = Mathf.RoundToInt(_wrenchSize.Value * 10f);
                if (wrenchSize < 0) { return; }

                try
                {
                    if ((bool)ScrewablePart.showScrewSize.Value)
                    {
                        ScrewablePart.GuiInteraction = "Screw size: " + screw.size;
                    }
                }
                catch { }

                if(wrenchSize != screw.size) { return; }

                screw.renderer.material.shader = Shader.Find("GUI/Text Shader");
                screw.renderer.material.SetColor("_Color", Color.green);

                screwingTimer += Time.deltaTime;
                if(screwingTimer >= _boltingSpeed.Value) 
                {
                    screwingTimer = 0;
                    if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                    {
                        if (ratchet.activeSelf)
                        {
                            if (ratchetSwitch.Value)
                            {
                                screwablePart.ScrewIn(screw);
                            }
                            else
                            {
                                screwablePart.ScrewOut(screw);
                            }
                        }
                        else { screwablePart.ScrewIn(screw); }


                    }
                    if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                    {
                        if (ratchet.activeSelf)
                        {
                            if (ratchetSwitch.Value)
                            {
                                screwablePart.ScrewIn(screw);
                                return;
                            }
                            else
                            {
                                screwablePart.ScrewOut(screw);
                                return;
                            }
                        }
                        else { screwablePart.ScrewOut(screw); }
                    }

                    if (screws.All(screwOfArr => screwOfArr.tightness == screwablePart.maxTightness) && !partFixed)
                    {
                        parentCollider.enabled = false;
                        partFixed = true;
                    }
                    else if (!partFixed)
                    {
                        parentCollider.enabled = true;
                    }
                }


            }
        }

        private ScrewV2 DetectScrew()
        {
            if (previousScrew != null)
            {
                previousScrew.renderer.material = screwMaterial;
                previousScrew = null;
            }
            if (!isToolInHand || Camera.main == null) { return null; }
            RaycastHit hit;
            GameObject hitObject;
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1f, 1 << LayerMask.NameToLayer("DontCollide"))) { return null; }
                
            hitObject = hit.collider?.gameObject;
            if(!hitObject.name.Contains("SCREW") || !hitObject.name.Contains(parent.name)) { return null; }

            for(int i = 0; i < screws.Length; i++)
            {
                ScrewV2 screw = screws[i];
                if(hitObject.name == screw.id) 
                { 
                    if(previousScrew != null)
                    {
                        previousScrew.renderer.material = screwMaterial;
                    }
                    previousScrew = screw;
                    return screw; 
                }
            }
            return null;
        }
    }
}