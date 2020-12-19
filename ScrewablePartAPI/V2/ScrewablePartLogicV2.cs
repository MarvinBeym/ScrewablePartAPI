using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Linq;
using UnityEngine;

namespace ScrewablePartAPI.V2
{
    /// <summary>
    /// The logic that handles all the detection logic
    /// </summary>
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
        internal Collider parentCollider;
        private ScrewV2[] screws;
        private ScrewablePartV2 screwablePart;

        private FsmFloat _wrenchSize;
        private FsmFloat _boltingSpeed;

        
        private float screwingTimer;

        internal static bool initAlreadyRun = false;
        internal static bool isToolInHand = false;
        internal static GameObject spanner;
        internal static GameObject ratchet;
        internal static FsmBool ratchetSwitch;
        internal ScrewV2 previousScrew;

        /// <summary>
        /// Further initialization
        /// For example injects hooks to the tool/hand that defines if the user has a tool in his hand
        /// Some of the values are loaded as static and used for each and every instance of the logic to improve performance
        /// </summary>
        void Start()
        {
            if(spanner == null || ratchet == null)
            {
                initAlreadyRun = false;
            }
            if (!initAlreadyRun)
            {
                try
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
                    initAlreadyRun = true;
                }
                catch
                {
                    initAlreadyRun = false;
                }

            }

            _boltingSpeed = PlayMakerGlobals.Instance.Variables.GetFsmFloat("BoltingSpeed");
            _wrenchSize = PlayMakerGlobals.Instance.Variables.GetFsmFloat("ToolWrenchSize");
        }

        /// <summary>
        /// Initializes the logic by passing information required by the logic
        /// </summary>
        /// <param name="parent">The parent (equal to this.gameobject in this case)</param>
        /// <param name="screws">The array of screws this logic is responsible for</param>
        /// <param name="screwablePart">The ScrewablePartV2 object that created this logic object</param>
        internal void Init(GameObject parent, ScrewV2[] screws, ScrewablePartV2 screwablePart)
        {
            screwMaterial = ScrewablePartV2Mod.material;
            screwSound = ScrewablePartV2Mod.soundClip;
            this.parent = parent;
            this.screws = screws;
            this.screwablePart = screwablePart;
            parentCollider = parent.GetComponent<Collider>();
        }


        /// <summary>
        /// Called every frame and detects the screws
        /// </summary>
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
                    if ((bool)ScrewablePartV2Mod.showScrewSize && screw.showSize)
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

                    CheckAllScrewsTight(screws);
                }
            }
        }

        internal void CheckAllScrewsTight(ScrewV2[] screws)
        {
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

        /// <summary>
        /// This detects the screw the user is aiming at
        /// </summary>
        /// <returns>Either a ScrewV2 object or null if nothing is found</returns>
        private ScrewV2 DetectScrew()
        {
            if (previousScrew != null)
            {
                previousScrew.renderer.material = screwMaterial;
                previousScrew = null;
            }
            if (Camera.main == null) { return null; }
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