using HutongGames.PlayMaker;
using MSCLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


namespace ScrewablePartAPI
{
    /// <summary>
    /// The main ScrewablePart class
    /// Handles everything about Detecting screwing, save, load and creating the child GameObjects.
    /// </summary>
    public class ScrewablePart
    {
        /// <summary>
        /// will return if the screwable part is fixed (all screwes tight).
        /// </summary>
        public bool partFixed = false;
        /// <summary>
        /// will return the version of this API in case you need it for something.
        /// </summary>
        public static string apiVersion = "1.2";

        private GameObject parentGameObject;
        private Collider parentGameObjectCollider;
        private GameObject screwModelToUse;
        private Screws screws;
        private AudioClip screw_soundClip;
        
        private float screwingTimer;
        private Vector3[] screwsDefaultPositionLocal;
        private Vector3[] screwsDefaultRotationLocal;


        private GameObject hitScrew = null;
        private bool aimingAtScrew = false;
        private RaycastHit hit;
        private Material screw_material;
        private AssetBundle assets;


        private GameObject selectedItem;
        private PlayMakerFSM selectedItemFSM;
        private FsmFloat _wrenchSize;
        private FsmFloat _boltingSpeed;
        private GameObject spannerRatchetGameObject;

        private bool toolInHand = false;
        private bool ratchetInHand = false;
        private bool ratchetSwitch = false;

        /// <summary>
        /// Generates the Screws for a part and makes them detectable using the DetectScrewing method
        /// <para>Dont ever change the name of the SCREW GameObject that gets created, which is always parentGameObject.name + "_SCREW + screwNumber</para>
        /// <para>example: Racing Turbocharger_SCREW1</para>
        /// <para>the constructor will auto find the correct gameObject to create the screws on (if names did not change)</para>
        /// </summary>
        /// <param name="screwsListSave">SortedList of saved information for ALL Parts!</param>
        /// <param name="mod">Your mod, usually "this" - needed to load scriptapi assets based on your mods asset folder path</param>
        /// <param name="parentGameObject">The "parentGameObject" GameObject on which screws should be placed. This should always be the ModAPI part.rigidPart when using modapi!!!</param>
        /// <param name="screwsPositionsLocal">The position where each screw should be placed on the parentGameObject GameObject</param>
        /// <param name="screwsRotationLocal">The rotation the screws should have when placed on parentGameObject GameObject</param>
        /// <param name="screwsSizeForAll">The size for all screws to be used as a single value if it is set to 8 you need to use the wrench size 8 to install the parts</param>
        /// <param name="screwType">The screw type to use, choose "screwable_nut", "screwable_screw1", "screwable_screw2" or "screwable_screw3" if not written correctly will load "screwable_nut"</param>
        public ScrewablePart(SortedList<String, Screws> screwsListSave, Mod mod, GameObject parentGameObject, Vector3[] screwsPositionsLocal, Vector3[] screwsRotationLocal, int screwsSizeForAll, string screwType)
        {
            this.assets = LoadAssets.LoadBundle(mod, "screwableapi.unity3d");

            this.screwModelToUse = loadscrewModelToUse(screwType);
            this.screw_material = assets.LoadAsset<Material>("Screw-Material.mat");
            this.screw_soundClip = (assets.LoadAsset("screwable_sound.wav") as AudioClip);

            this.selectedItem = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SelectItem");
            this.selectedItemFSM = selectedItem.GetComponent<PlayMakerFSM>();

            FsmHook.FsmInject(selectedItem, "Hand", new Action(ChangedToHand));
            FsmHook.FsmInject(selectedItem, "Tools", new Action(ChangedToTools));

            this._boltingSpeed = PlayMakerGlobals.Instance.Variables.GetFsmFloat("BoltingSpeed");
            this._wrenchSize = PlayMakerGlobals.Instance.Variables.GetFsmFloat("ToolWrenchSize");
            //this._wrenchSize = selectedItemFSM.Fsm.GetFsmFloat("OldWrench");


            this.parentGameObject = parentGameObject;

            this.screwsDefaultPositionLocal = screwsPositionsLocal.Clone() as Vector3[];
            this.screwsDefaultRotationLocal = screwsRotationLocal.Clone() as Vector3[];

            if (screwsListSave != null)
            {
                Screws loadedScrews;
                bool successWhenLoading = screwsListSave.TryGetValue(parentGameObject.name, out loadedScrews);
                if (successWhenLoading)
                {
                    //Save provided and found in file
                    this.screws = loadedScrews;
                }
                else
                {
                    this.screws = new Screws();
                }
            }

            if (this.screws == null)
            {
                //No Save provided
                this.screws = new Screws();


                //Initialize screwSize
                int[] screwSize = new int[screwsPositionsLocal.Length];
                for (int i = 0; i < screwSize.Length; i++)
                {
                    screwSize[i] = screwsSizeForAll;
                }

                for (int i = 0; i < screwSize.Length; i++)
                {
                    if (screwSize[i] < 5)
                    {
                        screwSize[i] = 5;
                    }
                    else if (screwSize[i] > 15)
                    {
                        screwSize[i] = 15;
                    }
                }

                //Initialize screwTightness
                int[] screwTightness = new int[screwsPositionsLocal.Length];
                for (int i = 0; i < screwTightness.Length; i++)
                {
                    screwTightness[i] = 0;
                }

                this.screws.partName = parentGameObject.name;
                this.screws.screwsPositionsLocal = screwsPositionsLocal;
                this.screws.screwsRotationLocal = screwsRotationLocal;
                this.screws.screwsSize = screwSize;
                this.screws.screwsTightness = screwTightness;

            }
            assets.Unload(false);
            MakePartScrewable(this.screws);
        }

        /// <summary>
        /// Generates the Screws for a part and makes them detectable using the DetectScrewing method
        /// <para>Dont ever change the name of the SCREW GameObject that gets created, which is always parentGameObject.name + "_SCREW + screwNumber</para>
        /// <para>example: Racing Turbocharger_SCREW1</para>
        /// <para>the constructor will auto find the correct gameObject to create the screws on (if names did not change)</para>
        /// </summary>
        /// <param name="screwsListSave">SortedList of saved information for ALL Parts!</param>
        /// <param name="mod">Your mod, usually "this" - needed to load scriptapi assets based on your mods asset folder path</param>
        /// <param name="parentGameObject">The "parentGameObject" GameObject on which screws should be placed. This should always be the ModAPI part.rigidPart when using modapi!!!</param>
        /// <param name="screwsPositionsLocal">The position where each screw should be placed on the parentGameObject GameObject</param>
        /// <param name="screwsRotationLocal">The rotation the screws should have when placed on parentGameObject GameObject</param>
        /// <param name="screwsScale">The scale the screw object should have (1 = defaults game scale)</param>
        /// <param name="screwsSizeForAll">The size for all screws to be used as a single value if it is set to 8 you need to use the wrench size 8 to install the parts</param>
        /// <param name="screwType">The screw type to use, choose "screwable_nut", "screwable_screw1", "screwable_screw2" or "screwable_screw3" if not written correctly will load "screwable_nut"</param>
        public ScrewablePart(SortedList<String, Screws> screwsListSave, Mod mod, GameObject parentGameObject, Vector3[] screwsPositionsLocal, Vector3[] screwsRotationLocal, Vector3[] screwsScale, int screwsSizeForAll, string screwType)
        {
            this.assets = LoadAssets.LoadBundle(mod, "screwableapi.unity3d");
            
            this.screwModelToUse = loadscrewModelToUse(screwType);
            this.screw_material = assets.LoadAsset<Material>("Screw-Material.mat");
            this.screw_soundClip = (assets.LoadAsset("screwable_sound.wav") as AudioClip);

            this.selectedItem = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SelectItem");
            this.selectedItemFSM = selectedItem.GetComponent<PlayMakerFSM>();

            FsmHook.FsmInject(selectedItem, "Hand", new Action(ChangedToHand));
            FsmHook.FsmInject(selectedItem, "Tools", new Action(ChangedToTools));

            this._boltingSpeed = PlayMakerGlobals.Instance.Variables.GetFsmFloat("BoltingSpeed");
            this._wrenchSize = PlayMakerGlobals.Instance.Variables.GetFsmFloat("ToolWrenchSize");
            //this._wrenchSize = selectedItemFSM.Fsm.GetFsmFloat("OldWrench");


            this.parentGameObject = parentGameObject;

            this.screwsDefaultPositionLocal = screwsPositionsLocal.Clone() as Vector3[];
            this.screwsDefaultRotationLocal = screwsRotationLocal.Clone() as Vector3[];

            if (screwsListSave != null)
            {
                Screws loadedScrews;
                bool successWhenLoading = screwsListSave.TryGetValue(parentGameObject.name, out loadedScrews);
                if (successWhenLoading)
                {
                    //Save provided and found in file
                    this.screws = loadedScrews;
                }
                else
                {
                    this.screws = new Screws();
                }
            }

            if (this.screws == null)
            {
                //No Save provided
                this.screws = new Screws();


                //Initialize screwSize
                int[] screwSize = new int[screwsPositionsLocal.Length];
                for (int i = 0; i < screwSize.Length; i++)
                {
                    screwSize[i] = screwsSizeForAll;
                }

                for (int i = 0; i < screwSize.Length; i++)
                {
                    if (screwSize[i] < 5)
                    {
                        screwSize[i] = 5;
                    }
                    else if (screwSize[i] > 15)
                    {
                        screwSize[i] = 15;
                    }
                }

                //Initialize screwTightness
                int[] screwTightness = new int[screwsPositionsLocal.Length];
                for (int i = 0; i < screwTightness.Length; i++)
                {
                    screwTightness[i] = 0;
                }

                this.screws.partName = parentGameObject.name;
                this.screws.screwsPositionsLocal = screwsPositionsLocal;
                this.screws.screwsRotationLocal = screwsRotationLocal;
                this.screws.screwsSize = screwSize;
                this.screws.screwsTightness = screwTightness;

                

            }
            assets.Unload(false);
            MakePartScrewable(this.screws, screwsScale);
        }


        private GameObject loadscrewModelToUse(string screwType)
        {
            GameObject screwModel;
            switch (screwType)
            {
                case "screwable_nut":
                    screwModel = (this.assets.LoadAsset("screwable_nut.prefab") as GameObject);
                    break;
                case "screwable_screw1":
                    screwModel = (this.assets.LoadAsset("screwable_screw1.prefab") as GameObject);
                    break;
                case "screwable_screw2":
                    screwModel = (this.assets.LoadAsset("screwable_screw2.prefab") as GameObject);
                    break;
                case "screwable_screw3":
                    screwModel = (this.assets.LoadAsset("screwable_screw3.prefab") as GameObject);
                    break;
                default:
                    screwModel = (this.assets.LoadAsset("screwable_nut.prefab") as GameObject);
                    break;
            }
            return screwModel;
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

        /// <summary>
        /// makes part that got created using the Constructor screwable by creating child GameObjects using the screw_model loaded
        /// </summary>
        /// <param name="screws">The screws of the single part</param>
        private void MakePartScrewable(Screws screws)
        {
            for (int i = 0; i < screws.screwsPositionsLocal.Length; i++)
            {
                GameObject screw = GameObject.Instantiate(screwModelToUse);
                screw.name = (parentGameObject.name + "_SCREW" + (i + 1));
                screw.transform.SetParent(parentGameObject.transform);
                screw.transform.localPosition = screws.screwsPositionsLocal[i];
                screw.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                screw.transform.localRotation = new Quaternion { eulerAngles = screws.screwsRotationLocal[i] };
                screw.layer = LayerMask.NameToLayer("DontCollide");
            }

            this.parentGameObjectCollider = this.parentGameObject.GetComponent<Collider>();
            if (screws.screwsTightness.All(element => element == 8))
            {
                //All Screws tight. Make part fixed
                this.parentGameObjectCollider.enabled = false;
                partFixed = true;
            }
        }

        /// <summary>
        /// makes the part screwable by adding the screw gameObjects to the parent gameObject
        /// </summary>
        /// <param name="screws">Screws values needed</param>
        /// <param name="screwsScale">The scale to apply to the screw when creating it</param>
        private void MakePartScrewable(Screws screws, Vector3[] screwsScale)
        {
            for (int i = 0; i < screws.screwsPositionsLocal.Length; i++)
            {
                GameObject screw = GameObject.Instantiate(screwModelToUse);
                screw.name = (parentGameObject.name + "_SCREW" + (i + 1));
                screw.transform.SetParent(parentGameObject.transform);
                screw.transform.localPosition = screws.screwsPositionsLocal[i];
                screw.transform.localScale = screwsScale[i];
                screw.transform.localRotation = new Quaternion { eulerAngles = screws.screwsRotationLocal[i] };
                screw.layer = LayerMask.NameToLayer("DontCollide");
            }

            this.parentGameObjectCollider = this.parentGameObject.GetComponent<Collider>();
            if (screws.screwsTightness.All(element => element == 8))
            {
                //All Screws tight. Make part fixed
                this.parentGameObjectCollider.enabled = false;
                partFixed = true;
            }
        }

        /// <summary>
        /// Call this on the part inside your mods Update() method.
        /// if you want to stop checking, comment this out or handle it using a bool value so it won't be called
        /// </summary>
        public void DetectScrewing()
        {
            if (Camera.main != null)
            {
                if (toolInHand == true)
                {
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1f, 1 << LayerMask.NameToLayer("DontCollide")) != false)
                    {
                        if(spannerRatchetGameObject == null)
                        {
                            spannerRatchetGameObject = GameObject.Find("2Spanner");
                        }

                        if(spannerRatchetGameObject != null)
                        {
                            Component[] comps = spannerRatchetGameObject.GetComponentsInChildren<Transform>();
                            for(int i = 0; i < comps.Length; i++)
                            {
                                if(comps[i].name == "Spanner")
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



                        hitScrew = hit.collider?.gameObject;

                        if (hitScrew != null && hitScrew.name.Contains("SCREW") && hitScrew.name.Contains(parentGameObject.name))
                        {
                            string screwName = hitScrew.name.Substring(hitScrew.name.LastIndexOf("_SCREW"));
                            int index = Convert.ToInt32(screwName.Replace("_SCREW", "")) - 1;

                            int wrenchSize = Mathf.RoundToInt(this._wrenchSize.Value * 10f);
                            int screwSize = this.screws.screwsSize[index];
                            if (wrenchSize == screwSize)
                            {
                                screwingTimer += Time.deltaTime;
                                aimingAtScrew = true;
                                MeshRenderer renderer = hitScrew.GetComponentInChildren<MeshRenderer>();
                                renderer.material.shader = Shader.Find("GUI/Text Shader");
                                renderer.material.SetColor("_Color", Color.green);


                                if (Input.GetAxis("Mouse ScrollWheel") > 0f && screwingTimer >= _boltingSpeed.Value) // forward
                                {
                                    screwingTimer = 0;
                                    if (ratchetInHand)
                                    {
                                        if (!ratchetSwitch)
                                        {
                                            ScrewOut(screws, index);
                                        }
                                        else
                                        {
                                            ScrewIn(screws, index);
                                        }
                                    }
                                    else
                                    {
                                        ScrewIn(screws, index);
                                    }
                                }
                                else if (Input.GetAxis("Mouse ScrollWheel") < 0f && screwingTimer >= _boltingSpeed.Value) // backwards
                                {
                                    screwingTimer = 0;
                                    if (ratchetInHand)
                                    {
                                        if (!ratchetSwitch)
                                        {
                                            ScrewOut(screws, index);
                                        }
                                        else
                                        {
                                            ScrewIn(screws, index);
                                        }
                                    }
                                    else
                                    {
                                        ScrewOut(screws, index);
                                    }
                                }

                                if (this.screws.screwsTightness.All(element => element == 8) && !partFixed)
                                {
                                    this.parentGameObjectCollider.enabled = false;
                                    partFixed = true;
                                }
                                else if (!partFixed)
                                {
                                    this.parentGameObjectCollider.enabled = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        aimingAtScrew = false;
                    }
                }
                if (hitScrew != null && hitScrew.name.Contains("SCREW") && hitScrew.name.Contains(parentGameObject.name) && aimingAtScrew == false && screw_material != null)
                {
                    MeshRenderer renderer = hitScrew.GetComponentInChildren<MeshRenderer>();
                    renderer.material = screw_material;
                    hitScrew = null;
                }
            }
        }

        private void ScrewIn(Screws screws, int screwIndex)
        {
            if (screws.screwsTightness[screwIndex] >= 0 && screws.screwsTightness[screwIndex] <= 7)
            {
                AudioSource.PlayClipAtPoint(this.screw_soundClip, hitScrew.transform.position);
                hitScrew.transform.Rotate(0, 0, 45);
                hitScrew.transform.Translate(0f, 0f, -0.0008f); //Has to be adjustable

                screws.screwsPositionsLocal[screwIndex] = hitScrew.transform.localPosition;
                screws.screwsRotationLocal[screwIndex] = hitScrew.transform.localRotation.eulerAngles;
                screws.screwsTightness[screwIndex]++;
            }
            
        }

        private void ScrewOut(Screws screws, int screwIndex)
        {
            if (screws.screwsTightness[screwIndex] > 0 && screws.screwsTightness[screwIndex] <= 8)
            {
                AudioSource.PlayClipAtPoint(this.screw_soundClip, hitScrew.transform.position);
                hitScrew.transform.Rotate(0, 0, -45);
                hitScrew.transform.Translate(0f, 0f, 0.0008f); //Has to be adjustable

                screws.screwsPositionsLocal[screwIndex] = hitScrew.transform.localPosition;
                screws.screwsRotationLocal[screwIndex] = hitScrew.transform.localRotation.eulerAngles;
                screws.screwsTightness[screwIndex]--;
            }
            partFixed = false;
        }


        /// <summary>
        /// <para>Call this in ModApi.Attachable part function "disassemble(bool startUp = false) on the static made screwable part AFTER base.disassemble(startUp);</para>
        /// <para>call this after checking the screwable part for != null</para>
        /// </summary>
        public void resetScrewsOnDisassemble()
        {
            int[] screwTightness = new int[this.screws.screwsPositionsLocal.Length];
            for (int i = 0; i < screwTightness.Length; i++)
            {
                screwTightness[i] = 0;
            }

            this.screws.screwsTightness = screwTightness;

            for (int i = 0; i < screwTightness.Length; i++)
            {
                this.screws.screwsPositionsLocal[i].x = this.screwsDefaultPositionLocal[i].x;
                this.screws.screwsPositionsLocal[i].y = this.screwsDefaultPositionLocal[i].y;
                this.screws.screwsPositionsLocal[i].z = this.screwsDefaultPositionLocal[i].z;

                this.screws.screwsRotationLocal[i].x = this.screwsDefaultRotationLocal[i].x;
                this.screws.screwsRotationLocal[i].y = this.screwsDefaultRotationLocal[i].y;
                this.screws.screwsRotationLocal[i].z = this.screwsDefaultRotationLocal[i].z;
            }
            partFixed = false;
        }

        /// <summary>
        /// <para>Call this in ModApi.Attachable part function "assemble(bool startUp = false) on the static made screwable part AFTER base.assemble(startUp);</para>
        /// <para>call this after checking the screwable part for != null</para>
        /// </summary>
        public void setScrewsOnAssemble()
        {
            resetScrewsOnDisassemble();
            for (int i = 0; i < this.screws.screwsPositionsLocal.Length; i++)
            {
                GameObject tmpScrew = GameObject.Find(parentGameObject.name + "_SCREW" + (i + 1));
                tmpScrew.transform.localPosition = this.screws.screwsPositionsLocal[i];

                tmpScrew.transform.localRotation = Quaternion.Euler(this.screws.screwsRotationLocal[i]);
            }
            this.parentGameObjectCollider.enabled = true;
        }

        /// <summary>
        /// Saves all Screws for all Parts that should have Screws.
        /// You only call this once for ALL Parts that are in the Screwable[] array
        /// <para>EXAMPLE</para>
        /// <para>Screwable.SaveScrews(this,new Screwable[]{ exhaust_header_screwable, exhaust_muffler_screwable } , "mysave.txt");</para>
        /// </summary>
        /// <param name="mod">Your own mod class (usually "this")</param>
        /// <param name="screwableParts">array of Screwable parts to be save (all screwable from your mod in an array)</param>
        /// <param name="filename">the filename to be used as saveFile (this will be your mods config folder inside Mods + filename) example: Mods\Config\Mod Settings\SatsumaTurboCharger\mysave.txt</param>
        public static void SaveScrews(Mod mod, ScrewablePart[] screwableParts, string filename)
        {
            if (mod != null && screwableParts != null && filename != null)
            {
                SortedList<String, Screws> screwsList = new SortedList<String, Screws>();
                if (filename.Length <= 0)
                {
                    filename = "SCREWSAVE_DEFAULT_CHANGE-FILENAME IN SaveScrews.txt";
                    ModConsole.Warning(
                        "You set an empty filename when you used SaveScrews(...).\n" +
                        "The name of the file was set as 'SCREWSAVE_DEFAULT_CHANGE-FILENAME IN SaveScrews.txt'.\n" +
                        "YOU HAVE TO CHANGE THIS, If you are not the mod maker, contact the mod maker and tell him he is dump!"
                        );
                }
                else if (!filename.EndsWith(".txt"))
                {
                    filename = filename + ".txt";
                }
                string savePath = (ModLoader.GetModConfigFolder(mod) + "\\" + filename);

                for (int i = 0; i < screwableParts.Length; i++)
                {
                    screwsList.Add(screwableParts[i].screws.partName, screwableParts[i].screws);
                }

                SaveLoad.SerializeSaveFile<SortedList<String, Screws>>(mod, screwsList, savePath);
            }
        }

        /// <summary>
        /// Loads all Screws for all Parts that should have Screws.
        /// You only call this once for ALL Parts that are in your mod
        /// <para>EXAMPLE</para>
        /// <para>Screwable.LoadScrews(this, "mysave.txt");</para>
        /// </summary>
        /// <param name="mod">Your own mod class (usually "this")</param>
        /// <param name="filename">the filename to be used as saveFile (this will be your mods config folder inside Mods + filename) example: Mods\Config\Mod Settings\SatsumaTurboCharger\mysave.txt</param>
        /// <returns>Will return SortedList or if no file has been found with "filename" inside ModsConfig folder will return null</returns>
        public static SortedList<String, Screws> LoadScrews(Mod mod, string filename)
        {
            try
            {
                string savePath = "";
                if (filename.Length <= 0)
                {
                    filename = "SCREWSAVE_DEFAULT_CHANGE-FILENAME IN SaveScrews.txt";
                    ModConsole.Warning(
                        "You set an empty filename when you used SaveScrews(...).\n" +
                        "The name of the file was set as 'SCREWSAVE_DEFAULT_CHANGE-FILENAME IN SaveScrews.txt'.\n" +
                        "YOU HAVE TO CHANGE THIS, If you are not the mod maker, contact the mod maker and tell him he is dump!"
                        );
                }
                else if (!filename.EndsWith(".txt"))
                {
                    filename = filename + ".txt";
                }
                savePath = (ModLoader.GetModConfigFolder(mod) + "\\" + filename);
                if (File.Exists(savePath))
                {
                    string serializedData = File.ReadAllText(savePath);
                    SortedList<String, Screws> screwsListSave = JsonConvert.DeserializeObject<SortedList<String, Screws>>(serializedData);
                    return screwsListSave;
                }
            }
            catch (System.NullReferenceException)
            {
                // error while trying to read SaveFile
                return null;
            }
            // no save file exists.. //loading default save data.
            return null;
        }
    }
}