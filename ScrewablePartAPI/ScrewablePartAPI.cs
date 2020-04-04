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
        private GameObject parentGameObject;
        private Collider parentGameObjectCollider;
        private GameObject boltModelToUse;
        private Screws screws;
        private AudioSource screw_sound;
        public bool partFixed = false;

        private Vector3[] screwsDefaultPositionLocal;
        private Vector3[] screwsDefaultRotationLocal;


        private GameObject hitBolt = null;
        private bool aimingAtBolt = false;
        private RaycastHit hit;
        private Material screw_material;
        private AssetBundle assets;

        private GameObject selectedItem;
        private PlayMakerFSM selectedItemFSM;
        private FsmFloat _wrenchSize;

        private bool toolInHand = false;

        [Obsolete("Do not use this anymore, switch to the constructor where it asks you to supply a boltType to use instead of AssetBundle")]
        /// <summary>
        /// Generates the Screws for a part and makes them detectable using the DetectBolting method
        /// <para>Dont ever change the name of the BOLT GameObject that gets created, which is always parentGameObject.name + "_BOLT + boltNumber</para>
        /// <para>example: Racing Turbocharger_BOLT1</para>
        /// <para>the constructor will auto find the correct gameObject to create the screws on (if names did not change)</para>
        /// </summary>
        /// <param name="screwsListSave">SortedList of saved information for ALL Parts!</param>
        /// <param name="parentGameObject">The "parentGameObject" GameObject on which bolts should be placed. This should always be the ModAPI part.rigidPart when using modapi!!!</param>
        /// <param name="screwsPositionsLocal">The position where each screw should be placed on the parentGameObject GameObject</param>
        /// <param name="screwsRotationLocal">The rotation the screws should have when placed on parentGameObject GameObject</param>
        /// <param name="screwsSizeForAll">The size for all screws to be used as a single value if it is set to 8 you need to use the wrench size 8 to install the parts</param>
        /// <param name="assets">The assets bundle to use. this will by default load the model as 'bolt_nut.prefab' and the material as 'bolt-texture.mat make sure those are inside your prefab</param>
        public ScrewablePart(SortedList<String, Screws> screwsListSave, GameObject parentGameObject, Vector3[] screwsPositionsLocal, Vector3[] screwsRotationLocal, int screwsSizeForAll, AssetBundle assets)
        {
            this.assets = assets;
            this.selectedItem = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SelectItem");
            this.selectedItemFSM = selectedItem.GetComponent<PlayMakerFSM>();
            FsmHook.FsmInject(selectedItem, "Hand", new Action(ChangedToHand));
            FsmHook.FsmInject(selectedItem, "Tools", new Action(ChangedToTools));

            this._wrenchSize = selectedItemFSM.Fsm.GetFsmFloat("OldWrench");
            this.screw_material = assets.LoadAsset<Material>("bolt-texture.mat");
            this.boltModelToUse = (assets.LoadAsset("bolt_nut.prefab") as GameObject);
            this.parentGameObject = parentGameObject;

            this.screwsDefaultPositionLocal = screwsPositionsLocal;
            this.screwsDefaultRotationLocal = screwsRotationLocal;
            

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


                //Initialize boltSize
                int[] boltSize = new int[screwsPositionsLocal.Length];
                for (int i = 0; i < boltSize.Length; i++)
                {
                    boltSize[i] = screwsSizeForAll;
                }

                for (int i = 0; i < boltSize.Length; i++)
                {
                    if (boltSize[i] < 5)
                    {
                        boltSize[i] = 5;
                    }
                    else if (boltSize[i] > 15)
                    {
                        boltSize[i] = 15;
                    }
                }

                //Initialize boltTightness
                int[] boltTightness = new int[screwsPositionsLocal.Length];
                for (int i = 0; i < boltTightness.Length; i++)
                {
                    boltTightness[i] = 0;
                }

                this.screws.partName = parentGameObject.name;
                this.screws.screwsPositionsLocal = screwsPositionsLocal;
                this.screws.screwsRotationLocal = screwsRotationLocal;
                this.screws.screwsSize = boltSize;
                this.screws.screwsTightness = boltTightness;

            }
            MakePartScrewable(this.screws);
        }

        /// <summary>
        /// Generates the Screws for a part and makes them detectable using the DetectBolting method
        /// <para>Dont ever change the name of the BOLT GameObject that gets created, which is always parentGameObject.name + "_BOLT + boltNumber</para>
        /// <para>example: Racing Turbocharger_BOLT1</para>
        /// <para>the constructor will auto find the correct gameObject to create the screws on (if names did not change)</para>
        /// </summary>
        /// <param name="screwsListSave">SortedList of saved information for ALL Parts!</param>
        /// <param name="mod">Your mod, usually "this" - needed to load scriptapi assets based on your mods asset folder path</param>
        /// <param name="parentGameObject">The "parentGameObject" GameObject on which bolts should be placed. This should always be the ModAPI part.rigidPart when using modapi!!!</param>
        /// <param name="screwsPositionsLocal">The position where each screw should be placed on the parentGameObject GameObject</param>
        /// <param name="screwsRotationLocal">The rotation the screws should have when placed on parentGameObject GameObject</param>
        /// <param name="screwsSizeForAll">The size for all screws to be used as a single value if it is set to 8 you need to use the wrench size 8 to install the parts</param>
        /// <param name="screwType">The screw type to use, choose "screwable_nut", "screwable_screw1", "screwable_screw2" or "screwable_screw3" if not written correctly will load "screwable_nut"</param>
        public ScrewablePart(SortedList<String, Screws> screwsListSave, Mod mod, GameObject parentGameObject, Vector3[] screwsPositionsLocal, Vector3[] screwsRotationLocal, int screwsSizeForAll, string screwType)
        {
            AssetBundle assets = LoadAssets.LoadBundle(mod, "screwableapi.unity3d");

            switch (screwType)
            {
                case "screwable_nut":
                    this.boltModelToUse = (assets.LoadAsset("screwable_nut.prefab") as GameObject);
                    break;
                case "screwable_screw1":
                    this.boltModelToUse = (assets.LoadAsset("screwable_screw1.prefab") as GameObject);
                    break;
                case "screwable_screw2":
                    this.boltModelToUse = (assets.LoadAsset("screwable_screw2.prefab") as GameObject);
                    break;
                case "screwable_screw3":
                    this.boltModelToUse = (assets.LoadAsset("screwable_screw3.prefab") as GameObject);
                    break;
                default:
                    this.boltModelToUse = (assets.LoadAsset("screwable_nut.prefab") as GameObject);
                    break;
            }
            this.screw_material = assets.LoadAsset<Material>("Screw-Material.mat");

            this.selectedItem = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SelectItem");
            this.selectedItemFSM = selectedItem.GetComponent<PlayMakerFSM>();
            FsmHook.FsmInject(selectedItem, "Hand", new Action(ChangedToHand));
            FsmHook.FsmInject(selectedItem, "Tools", new Action(ChangedToTools));

            this._wrenchSize = selectedItemFSM.Fsm.GetFsmFloat("OldWrench");
            
            
            this.parentGameObject = parentGameObject;

            this.screwsDefaultPositionLocal = screwsPositionsLocal;
            this.screwsDefaultRotationLocal = screwsRotationLocal;


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


                //Initialize boltSize
                int[] boltSize = new int[screwsPositionsLocal.Length];
                for (int i = 0; i < boltSize.Length; i++)
                {
                    boltSize[i] = screwsSizeForAll;
                }

                for (int i = 0; i < boltSize.Length; i++)
                {
                    if (boltSize[i] < 5)
                    {
                        boltSize[i] = 5;
                    }
                    else if (boltSize[i] > 15)
                    {
                        boltSize[i] = 15;
                    }
                }

                //Initialize boltTightness
                int[] boltTightness = new int[screwsPositionsLocal.Length];
                for (int i = 0; i < boltTightness.Length; i++)
                {
                    boltTightness[i] = 0;
                }

                this.screws.partName = parentGameObject.name;
                this.screws.screwsPositionsLocal = screwsPositionsLocal;
                this.screws.screwsRotationLocal = screwsRotationLocal;
                this.screws.screwsSize = boltSize;
                this.screws.screwsTightness = boltTightness;

            }
            assets.Unload(false);
            MakePartScrewable(this.screws);
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
        /// makes part that got created using the Constructor boltable by creating child GameObjects using the bolt_model loaded
        /// </summary>
        /// <param name="screws">The screws of the single part</param>
        private void MakePartScrewable(Screws screws)
        {
            for (int i = 0; i < screws.screwsPositionsLocal.Length; i++)
            {
                GameObject bolt = GameObject.Instantiate(boltModelToUse);
                bolt.name = (parentGameObject.name + "_BOLT" + (i + 1));
                bolt.transform.SetParent(parentGameObject.transform);
                bolt.transform.localPosition = screws.screwsPositionsLocal[i];
                bolt.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                bolt.transform.localRotation = new Quaternion { eulerAngles = screws.screwsRotationLocal[i] };
                bolt.layer = LayerMask.NameToLayer("DontCollide");
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
        public void DetectBolting()
        {
            if (Camera.main != null)
            {
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1f, 1 << LayerMask.NameToLayer("DontCollide")) != false)
                {
                    if (toolInHand == true)
                    {
                        hitBolt = hit.collider?.gameObject;

                        if (hitBolt != null && hitBolt.name.Contains("BOLT"))
                        {
                            int index = (Convert.ToInt32(hitBolt.name.Substring(hitBolt.name.Length - 1)) - 1);
                            if (Mathf.RoundToInt(this._wrenchSize.Value * 10f) == screws.screwsSize[index])
                            {
                                aimingAtBolt = true;
                                MeshRenderer renderer = hitBolt.GetComponentInChildren<MeshRenderer>();
                                renderer.material.shader = Shader.Find("GUI/Text Shader");
                                renderer.material.SetColor("_Color", Color.green);

                                if (Input.GetAxis("Mouse ScrollWheel") > 0f) // forward
                                {
                                    if (screws.screwsTightness[index] >= 0 && screws.screwsTightness[index] <= 7)
                                    {
                                        hitBolt.transform.Rotate(0, 0, 45);
                                        hitBolt.transform.Translate(0f, 0f, -0.0008f); //Has to be adjustable

                                        screws.screwsPositionsLocal[index] = hitBolt.transform.localPosition;
                                        screws.screwsRotationLocal[index] = hitBolt.transform.localRotation.eulerAngles;
                                        screws.screwsTightness[index]++;
                                    }
                                }
                                else if (Input.GetAxis("Mouse ScrollWheel") < 0f) // backwards
                                {
                                    if (screws.screwsTightness[index] > 0 && screws.screwsTightness[index] <= 8)
                                    {
                                        hitBolt.transform.Rotate(0, 0, -45);
                                        hitBolt.transform.Translate(0f, 0f, 0.0008f); //Has to be adjustable

                                        screws.screwsPositionsLocal[index] = hitBolt.transform.localPosition;
                                        screws.screwsRotationLocal[index] = hitBolt.transform.localRotation.eulerAngles;
                                        screws.screwsTightness[index]--;
                                    }
                                }

                                if ((this.screws.screwsTightness.All(element => element == 8)) && this.parentGameObjectCollider.enabled)
                                {
                                    this.parentGameObjectCollider.enabled = false;
                                    partFixed = true;
                                }
                                else if (!this.parentGameObjectCollider.enabled)
                                {
                                    this.parentGameObjectCollider.enabled = true;
                                    partFixed = false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    aimingAtBolt = false;
                }
                if (hitBolt != null && hitBolt.name.Contains("BOLT") && aimingAtBolt == false && screw_material != null)
                {
                    MeshRenderer renderer = hitBolt.GetComponentInChildren<MeshRenderer>();
                    renderer.material = screw_material;
                    hitBolt = null;
                }
            }
        }

        /// <summary>
        /// <para>Call this in ModApi.Attachable part function "disassemble(bool startUp = false) on the static made screwable part AFTER base.disassemble(startUp);</para>
        /// <para>call this after checking the screwable part for != null</para>
        /// </summary>
        public void resetBoltsOnDisassemble()
        {
            int[] boltTightness = new int[this.screws.screwsPositionsLocal.Length];
            for (int i = 0; i < boltTightness.Length; i++)
            {
                boltTightness[i] = 0;

            }

            this.screws.screwsTightness = boltTightness;

            for (int i = 0; i < boltTightness.Length; i++)
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
        public void setBoltsOnAssemble()
        {
            for (int i = 0; i < this.screws.screwsPositionsLocal.Length; i++)
            {
                GameObject tmpBolt = GameObject.Find(parentGameObject.name + "_BOLT" + (i + 1));
                tmpBolt.transform.localPosition = this.screws.screwsPositionsLocal[i];

                tmpBolt.transform.localRotation = Quaternion.Euler(this.screws.screwsRotationLocal[i]);
            }
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
                    filename = "BOLTSAVE_DEFAULT_CHANGE-FILENAME IN SaveBolts.txt";
                    ModConsole.Warning(
                        "You set an empty filename when you used Bolts.SaveBolts(...).\n" +
                        "The name of the file was set as 'BOLTSAVE_DEFAULT_CHANGE-FILENAME IN SaveBolts.txt'.\n" +
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
                    filename = "BOLTSAVE_DEFAULT_CHANGE-FILENAME IN SaveBolts.txt";
                    ModConsole.Warning(
                        "You set an empty filename when you used Bolts.SaveBolts(...).\n" +
                        "The name of the file was set as 'BOLTSAVE_DEFAULT_CHANGE-FILENAME IN SaveBolts.txt'.\n" +
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