using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Linq;
using UnityEngine;

namespace ScrewablePartAPI
{
    public class ScrewableLogic : MonoBehaviour
    {
        private ScrewablePart api;

        private bool aimingAtScrew = false;
        private RaycastHit hit;

        private bool toolInHand = false;
        private bool ratchetInHand = false;
        private bool ratchetSwitch = false;

        private GameObject selectedItem;
        private PlayMakerFSM selectedItemFSM;
        private FsmFloat _wrenchSize;
        private FsmFloat _boltingSpeed;
        private GameObject spannerRatchetGameObject;
        private ScrewablePart screwablePart;
        private Screws screws;
        private float screwingTimer;
        private Material screw_material;
        private AudioClip screw_soundClip;
        private GameObject parentGameObject;
        private Collider parentGameObjectCollider;
        private bool partFixed = false;

        private GameObject hitObject;
        private GameObject previousHitObject;

        // Use this for initialization
        void Start()
        {
            this.selectedItem = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SelectItem");
            this.selectedItemFSM = selectedItem.GetComponent<PlayMakerFSM>();

            FsmHook.FsmInject(selectedItem, "Hand", new Action(ChangedToHand));
            FsmHook.FsmInject(selectedItem, "Tools", new Action(ChangedToTools));

            _boltingSpeed = PlayMakerGlobals.Instance.Variables.GetFsmFloat("BoltingSpeed");
            _wrenchSize = PlayMakerGlobals.Instance.Variables.GetFsmFloat("ToolWrenchSize");
        }

        void Init(ScrewablePart api)
        {
            this.api = api;
        }


        // Update is called once per frame
        void Update()
        {
            DetectScrew();
            
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
            int screwSize = this.screws.screwsSize[index];
            if (wrenchSize == screwSize)
            {
                //Highlighting the currently aimed at screw
                MeshRenderer renderer = hitObject.GetComponentInChildren<MeshRenderer>();
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
                            ScrewOut(hitScrew, screws, index);
                        }
                        else
                        {
                            ScrewIn(hitScrew, screws, index);
                        }
                    }
                    else
                    {
                        ScrewIn(hitScrew, screws, index);
                    }
                }
                else if (Input.GetAxis("Mouse ScrollWheel") < 0f && screwingTimer >= _boltingSpeed.Value) // backwards
                {
                    screwingTimer = 0;
                    if (ratchetInHand)
                    {
                        if (!ratchetSwitch)
                        {
                            ScrewOut(hitScrew, screws, index);
                        }
                        else
                        {
                            ScrewIn(hitScrew, screws, index);
                        }
                    }
                    else
                    {
                        ScrewOut(hitScrew, screws, index);
                    }
                }

                if (this.screws.screwsTightness.All(element => element == 8) && !partFixed)
                {
                    this.parentGameObjectCollider.enabled = false;
                    partFixed = true;
                    screwablePart.SetPartFixed(true);
                }
                else if (!partFixed)
                {
                    this.parentGameObjectCollider.enabled = true;
                }
            }
        }

        /// <summary>
        /// Detects the screw object and makes sure only the correct and current screw is getting highlighted
        /// </summary>
        private void DetectScrew()
        {
            if (toolInHand && Camera.main != null)
            {
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1f, 1 << LayerMask.NameToLayer("DontCollide")) != false)
                {
                    hitObject = hit.collider?.gameObject;
                    if (!hitObject.name.Contains("SCREW") || !hitObject.name.Contains(parentGameObject.name))
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
                        renderer.material = screw_material;
                        hitObject = null;
                    }
                    if (previousHitObject != null)
                    {
                        MeshRenderer renderer = previousHitObject.GetComponentInChildren<MeshRenderer>();
                        renderer.material = screw_material;
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
                            renderer.material = screw_material;
                        }
                        previousHitObject = hitObject;
                    }
                    else
                    {
                        if ((bool)ScrewablePart.showScrewSize.Value)
                        {
                            PlayMakerFSM screwFsm = hitObject.GetComponent<PlayMakerFSM>();
                            if (screwFsm.FsmName == "Screw")
                            {
                                try
                                {
                                    FsmFloat sizeFsmFloat = screwFsm.FsmVariables.FindFsmFloat("size");
                                    if (sizeFsmFloat != null)
                                    {
                                        ScrewablePart.GuiInteraction = "Screw size: " + sizeFsmFloat.Value;
                                    }

                                }
                                catch
                                {

                                }
                            }
                        }
                        DetectScrewing(hitObject);
                    }
                }
            }
        }

        /// <summary>
        /// This loads the current (either loaded save or empty information about the screw into the MonoBehaviour class.
        /// This is needed so that the MonoBehaviour can work independet from the main class.
        /// </summary>
        /// <param name="screws">This is the screws save where all the information for a part is stored</param>
        /// <param name="screw_material">The screw material</param>
        /// <param name="screw_soundClip">The soundclip to be played when screwing in/out</param>
        /// <param name="parentGameObject">The parent gameObject</param>
        /// <param name="parentGameObjectCollider">The parent gameObjects collider</param>
        /// <param name="screwablePart">The screwable part object</param>
        public void SetSavedInformation(Screws screws, Material screw_material, AudioClip screw_soundClip, GameObject parentGameObject, Collider parentGameObjectCollider, ScrewablePart screwablePart)
        {
            this.screwablePart = screwablePart;
            this.screws = screws;
            this.screw_material = screw_material;
            this.screw_soundClip = screw_soundClip;
            this.parentGameObject = parentGameObject;
            this.parentGameObjectCollider = parentGameObjectCollider;
        }

        /// <summary>
        /// Returns the current and latest screw save to be needed when the static save method is called.
        /// </summary>
        /// <returns></returns>
        public Screws GetSaveInformation()
        {
            return screws;
        }

        /// <summary>
        /// Is called by the FsmHook on "Hand"
        /// Detects if player changed from tools hand to normal hand
        /// </summary>
        private void ChangedToHand()
        {
            toolInHand = false;
        }

        /// <summary>
        /// Is called by the FsmHook on "Tools"
        /// Detects if player changed from normal hand to tools hand
        /// </summary>
        private void ChangedToTools()
        {
            toolInHand = true;
        }

        private void ScrewIn(GameObject hitScrew, Screws screws, int screwIndex)
        {
            if (screws.screwsTightness[screwIndex] >= 0 && screws.screwsTightness[screwIndex] <= 7)
            {
                PlayMakerFSM screwFsm = hitScrew.GetComponent<PlayMakerFSM>();
                FsmFloat tightnessFsmFloat = new FsmFloat();
                if (screwFsm.FsmName == "Screw")
                {
                    tightnessFsmFloat = screwFsm.FsmVariables.FindFsmFloat("tightness");
                }
                AudioSource.PlayClipAtPoint(this.screw_soundClip, hitScrew.transform.position);
                hitScrew.transform.Rotate(0, 0, 45);
                hitScrew.transform.Translate(0f, 0f, -0.0008f); //Has to be adjustable

                screws.screwsPositionsLocal[screwIndex] = hitScrew.transform.localPosition;
                screws.screwsRotationLocal[screwIndex] = hitScrew.transform.localRotation.eulerAngles;
                screws.screwsTightness[screwIndex]++;
                tightnessFsmFloat.Value = screws.screwsTightness[screwIndex];
            }
        }

        private void ScrewOut(GameObject hitScrew, Screws screws, int screwIndex)
        {
            if (screws.screwsTightness[screwIndex] > 0 && screws.screwsTightness[screwIndex] <= 8)
            {
                PlayMakerFSM screwFsm = hitScrew.GetComponent<PlayMakerFSM>();
                FsmFloat tightnessFsmFloat = new FsmFloat();
                if (screwFsm.FsmName == "Screw")
                {
                    tightnessFsmFloat = screwFsm.FsmVariables.FindFsmFloat("tightness");
                }
                AudioSource.PlayClipAtPoint(this.screw_soundClip, hitScrew.transform.position);
                hitScrew.transform.Rotate(0, 0, -45);
                hitScrew.transform.Translate(0f, 0f, 0.0008f); //Has to be adjustable

                screws.screwsPositionsLocal[screwIndex] = hitScrew.transform.localPosition;
                screws.screwsRotationLocal[screwIndex] = hitScrew.transform.localRotation.eulerAngles;
                screws.screwsTightness[screwIndex]--;
                tightnessFsmFloat.Value = screws.screwsTightness[screwIndex];
            }
            partFixed = false;
            screwablePart.SetPartFixed(false);
        }

        /// <summary>
        /// Sets the logics partFixed to the parameter
        /// </summary>
        /// <param name="partFixed"></param>
        public void SetPartFixed(bool partFixed)
        {
            this.partFixed = partFixed;
        }
    }
}