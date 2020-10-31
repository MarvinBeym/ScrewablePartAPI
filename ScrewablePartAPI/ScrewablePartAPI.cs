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
        /// Defines all possible screw types (3D-Models available)
        /// </summary>
        public enum ScrewType
        {
            /// <summary>Nut 3D-Model</summary>
            Nut,
            /// <summary>Screw 1 3D-Model</summary>
            Screw1,
            /// <summary>Screw 2 3D-Model</summary>
            Screw2,
            /// <summary>Screw 3 3D-Model</summary>
            Screw3
        }

        public static Settings showScrewSize = new Settings("showScrewSize", "Show screw size", false);

        /// <summary>
        /// Returns if the current part is fixed/screwed in
        /// </summary>
        public bool partFixed = false;
        /// <summary>
        /// will return the version of this API in case you need it for something.
        /// </summary>
        public static string apiVersion = "1.4.1";

        private GameObject parentGameObject;
        private Collider parentGameObjectCollider;
        private GameObject screwModelToUse;
        private Screws screws;
        private AudioClip screw_soundClip;
        private ScrewableLogic screwableLogic;

        private float screwingTimer;
        private Vector3[] screwsDefaultPositionLocal;
        private Vector3[] screwsDefaultRotationLocal;


        private GameObject hitScrew = null;
        private bool aimingAtScrew = false;
        private RaycastHit hit;
        private Material screw_material;

        //Clamp
        private GameObject clampModel;
        private int clampsAdded = 1;

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
        /// <param name="screwsAssetBundle">The asset bundle (the loaded "screwableapi.unity3d")</param>
        /// <param name="parentGameObject">The "parentGameObject" GameObject on which screws should be placed. This should always be the ModAPI part.rigidPart when using modapi!!!</param>
        /// <param name="screwsPositionLocal">The position where each screw should be placed on the parentGameObject GameObject</param>
        /// <param name="screwsRotationLocal">The rotation the screws should have when placed on parentGameObject GameObject</param>
        /// <param name="screwsScale">The scale the screws should have IF null = all screws have scale 1, 1, 1. IF less than number of screws = all screws will use the scale from screwsScale[0]</param>
        /// <param name="screwsSizeForAll">The size for all screws to be used as a single value if it is set to 8 you need to use the wrench size 8 to install the parts</param>
        /// <param name="screwType">The screw type to use, choose "screwable_nut", "screwable_screw1", "screwable_screw2" or "screwable_screw3" if not written correctly will load "screwable_nut"</param>
        [Obsolete("This constructor is obsolete use the one that requires you to pass a single array of Screw objects", false)]
        public ScrewablePart(SortedList<String, Screws> screwsListSave, AssetBundle screwsAssetBundle, GameObject parentGameObject, Vector3[] screwsPositionLocal, Vector3[] screwsRotationLocal, Vector3[] screwsScale = null, int screwsSizeForAll = 10, ScrewType screwType = ScrewType.Screw1)
        {
            SetAssets(screwsAssetBundle, screwType);
            InitHandDetection();

            this.parentGameObject = parentGameObject;

            screwsDefaultPositionLocal = screwsPositionLocal.Clone() as Vector3[];
            screwsDefaultRotationLocal = screwsRotationLocal.Clone() as Vector3[];

            LoadScrewsSave(screwsListSave, screwsDefaultPositionLocal, screwsDefaultRotationLocal, screwsSizeForAll);

            Vector3[] definedScrewScales = DefineScrewScales(screwsPositionLocal.Length, screwsScale);
            MakePartScrewable(this.screws, definedScrewScales);
        }

        /// <summary>
        /// Generates the Screws for a part and makes them detectable using the DetectScrewing method
        /// <para>Dont ever change the name of the SCREW GameObject that gets created, which is always parentGameObject.name + "_SCREW + screwNumber</para>
        /// <para>example: Racing Turbocharger_SCREW1</para>
        /// <para>the constructor will auto find the correct gameObject to create the screws on (if names did not change)</para>
        /// </summary>
        /// <param name="screwsListSave">SortedList of saved information for ALL Parts!</param>
        /// <param name="screwsAssetBundle">The asset bundle (the loaded "screwableapi.unity3d")</param>
        /// <param name="parentGameObject">The "parentGameObject" GameObject on which screws should be placed. This should always be the ModAPI part.rigidPart when using modapi!!!</param>
        /// <param name="screws">The defined Screws</param>
        public ScrewablePart(SortedList<String, Screws> screwsListSave, AssetBundle screwsAssetBundle, GameObject parentGameObject, Screw[] screws)
        {
            screw_material = screwsAssetBundle.LoadAsset<Material>("Screw-Material.mat");
            screw_soundClip = (screwsAssetBundle.LoadAsset("screwable_sound.wav") as AudioClip);
            clampModel = (screwsAssetBundle.LoadAsset("Tube_Clamp.prefab") as GameObject);

            screwsDefaultPositionLocal = new Vector3[screws.Length];
            screwsDefaultRotationLocal = new Vector3[screws.Length];
            int[] sizes = new int[screws.Length];
            float[] scales = new float[screws.Length];
            for(int i = 0; i < screws.Length; i++)
            {
                Screw screw = screws[i];
                screw.model = LoadScrewModelToUse(screw.type, screwsAssetBundle);
                screwsDefaultPositionLocal[i] = screw.position;
                screwsDefaultRotationLocal[i] = screw.rotation;
                sizes[i] = screw.size;
                scales[i] = screw.scale;
            }

            InitHandDetection();

            this.parentGameObject = parentGameObject;

            LoadScrewsSave(screwsListSave, screwsDefaultPositionLocal, screwsDefaultRotationLocal, sizes);

            Vector3[] definedScrewScales = DefineScrewScales(screws.Length, scales);

            MakePartScrewable(this.screws, definedScrewScales, screws);
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
        /// <param name="screwsPositionLocal">The position where each screw should be placed on the parentGameObject GameObject</param>
        /// <param name="screwsRotationLocal">The rotation the screws should have when placed on parentGameObject GameObject</param>
        /// <param name="screwsSizeForAll">The size for all screws to be used as a single value if it is set to 8 you need to use the wrench size 8 to install the parts</param>
        /// <param name="screwType">The screw type to use, choose "screwable_nut", "screwable_screw1", "screwable_screw2" or "screwable_screw3" if not written correctly will load "screwable_nut"</param>
        [Obsolete("This constructor is obsolete, it has been replaced with one where you pass the already loaded assetsBundle to the constructor. It also improves the scale setting by allowing it to be empty or just one", false)]
        public ScrewablePart(SortedList<String, Screws> screwsListSave, Mod mod, GameObject parentGameObject, Vector3[] screwsPositionLocal, Vector3[] screwsRotationLocal, int screwsSizeForAll, string screwType)
        {
            AssetBundle assets = LoadAssets.LoadBundle(mod, "screwableapi.unity3d");
            SetAssets(assets, StringScrewTypeToEnum(screwType));
            assets.Unload(false);
            InitHandDetection();

            this.parentGameObject = parentGameObject;

            screwsDefaultPositionLocal = screwsPositionLocal.Clone() as Vector3[];
            screwsDefaultRotationLocal = screwsRotationLocal.Clone() as Vector3[];

            LoadScrewsSave(screwsListSave, screwsDefaultPositionLocal, screwsDefaultRotationLocal, screwsSizeForAll);

            Vector3[] definedScrewScales = DefineScrewScales(screwsPositionLocal.Length, new Vector3[0]);

            MakePartScrewable(this.screws, definedScrewScales);
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
        /// <param name="screwsPositionLocal">The position where each screw should be placed on the parentGameObject GameObject</param>
        /// <param name="screwsRotationLocal">The rotation the screws should have when placed on parentGameObject GameObject</param>
        /// <param name="screwsScale">The scale the screw object should have (1 = defaults game scale)</param>
        /// <param name="screwsSizeForAll">The size for all screws to be used as a single value if it is set to 8 you need to use the wrench size 8 to install the parts</param>
        /// <param name="screwType">The screw type to use, choose "screwable_nut", "screwable_screw1", "screwable_screw2" or "screwable_screw3" if not written correctly will load "screwable_nut"</param>
        [Obsolete("This constructor is obsolete, it has been replaced with one where you pass the already loaded assetsBundle to the constructor. It also improves the scale setting by allowing it to be empty or just one", false)]
        public ScrewablePart(SortedList<String, Screws> screwsListSave, Mod mod, GameObject parentGameObject, Vector3[] screwsPositionLocal, Vector3[] screwsRotationLocal, Vector3[] screwsScale, int screwsSizeForAll, string screwType)
        {
            AssetBundle assets = LoadAssets.LoadBundle(mod, "screwableapi.unity3d");
            SetAssets(assets, StringScrewTypeToEnum(screwType));
            assets.Unload(false);
            InitHandDetection();


            this.parentGameObject = parentGameObject;

            this.screwsDefaultPositionLocal = screwsPositionLocal.Clone() as Vector3[];
            this.screwsDefaultRotationLocal = screwsRotationLocal.Clone() as Vector3[];

            LoadScrewsSave(screwsListSave, screwsDefaultPositionLocal, screwsDefaultRotationLocal, screwsSizeForAll);

            Vector3[] definedScrewScales = DefineScrewScales(screwsPositionLocal.Length, screwsScale);

            MakePartScrewable(this.screws, definedScrewScales);
        }

        private void SetAssets(AssetBundle assets, ScrewType screwType)
        {
            screwModelToUse = LoadScrewModelToUse(screwType, assets);
            screw_material = assets.LoadAsset<Material>("Screw-Material.mat");
            screw_soundClip = (assets.LoadAsset("screwable_sound.wav") as AudioClip);
            clampModel = (assets.LoadAsset("Tube_Clamp.prefab") as GameObject);
        }

        /// <summary>
        /// Loads the screw save and defines position, rotation and screw size (ratchet size)
        /// </summary>
        /// <param name="screwsListSave">the loaded screw save for all screws in the mod</param>
        /// <param name="screwsPositionLocal">All the local positions of the screws for one part</param>
        /// <param name="screwsRotationLocal">All the local rotations of the screws for one part</param>
        /// <param name="screwsSizeForAll">The screw size (what ratchet/wrench is needed)</param>
        [Obsolete("Only needed for compatibility with older versions", false)]
        private void LoadScrewsSave(SortedList<String, Screws> screwsListSave, Vector3[] screwsPositionLocal, Vector3[] screwsRotationLocal, int screwsSizeForAll)
        {

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
                    //Save provided but part not found inside
                    this.screws = new Screws();

                    //Initialize screwSize
                    int[] screwSize = new int[screwsPositionLocal.Length];
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
                    int[] screwTightness = new int[screwsPositionLocal.Length];
                    for (int i = 0; i < screwTightness.Length; i++)
                    {
                        screwTightness[i] = 0;
                    }

                    this.screws.partName = parentGameObject.name;
                    this.screws.screwsPositionsLocal = screwsPositionLocal;
                    this.screws.screwsRotationLocal = screwsRotationLocal;
                    this.screws.screwsSize = screwSize;
                    this.screws.screwsTightness = screwTightness;
                }
            }

            if (this.screws == null)
            {
                //No Save provided
                this.screws = new Screws();


                //Initialize screwSize
                int[] screwSize = new int[screwsPositionLocal.Length];
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
                int[] screwTightness = new int[screwsPositionLocal.Length];
                for (int i = 0; i < screwTightness.Length; i++)
                {
                    screwTightness[i] = 0;
                }

                this.screws.partName = parentGameObject.name;
                this.screws.screwsPositionsLocal = screwsPositionLocal;
                this.screws.screwsRotationLocal = screwsRotationLocal;
                this.screws.screwsSize = screwSize;
                this.screws.screwsTightness = screwTightness;

            }
        }
        private void LoadScrewsSave(SortedList<String, Screws> screwsListSave, Vector3[] screwsPositionLocal, Vector3[] screwsRotationLocal, int[] sizes)
        {

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
                    //Save provided but part not found inside
                    this.screws = new Screws();

                    //Initialize screwSize
                    int[] screwSize = new int[screwsPositionLocal.Length];
                    for (int i = 0; i < screwSize.Length; i++)
                    {
                        screwSize[i] = sizes[i];
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
                    int[] screwTightness = new int[screwsPositionLocal.Length];
                    for (int i = 0; i < screwTightness.Length; i++)
                    {
                        screwTightness[i] = 0;
                    }

                    this.screws.partName = parentGameObject.name;
                    this.screws.screwsPositionsLocal = screwsPositionLocal;
                    this.screws.screwsRotationLocal = screwsRotationLocal;
                    this.screws.screwsSize = screwSize;
                    this.screws.screwsTightness = screwTightness;
                }
            }

            if (this.screws == null)
            {
                //No Save provided
                this.screws = new Screws();


                //Initialize screwSize
                int[] screwSize = new int[screwsPositionLocal.Length];
                for (int i = 0; i < screwSize.Length; i++)
                {
                    screwSize[i] = sizes[i];
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
                int[] screwTightness = new int[screwsPositionLocal.Length];
                for (int i = 0; i < screwTightness.Length; i++)
                {
                    screwTightness[i] = 0;
                }

                this.screws.partName = parentGameObject.name;
                this.screws.screwsPositionsLocal = screwsPositionLocal;
                this.screws.screwsRotationLocal = screwsRotationLocal;
                this.screws.screwsSize = screwSize;
                this.screws.screwsTightness = screwTightness;

            }
        }

        [Obsolete("Only needed for compatibility with older versions", false)]
        private Vector3[] DefineScrewScales(int numberOfScrews, Vector3[] nonDefinedScales)
        {
            Vector3[] definedScrewScales = new Vector3[numberOfScrews];
            Vector3 scale;
            if(nonDefinedScales.Length == 0)
            {
                //Has not been set when creating object
                scale = new Vector3(1, 1, 1);
            }
            else if(nonDefinedScales.Length > 0 && nonDefinedScales.Length < numberOfScrews)
            {
                scale = nonDefinedScales[0];

            }
            else if(nonDefinedScales.Length == numberOfScrews)
            {
                return nonDefinedScales;
            }
            else
            {
                scale = new Vector3(1, 1, 1);
            }

            for (int i = 0; i < definedScrewScales.Length; i++)
            {
                definedScrewScales[i] = scale;
            }
            return definedScrewScales;
        }

        private Vector3[] DefineScrewScales(int numberOfScrews, float[] scales)
        {
            Vector3[] definedScrewScales = new Vector3[numberOfScrews];

            for (int i = 0; i < numberOfScrews; i++)
            {
                definedScrewScales[i] = new Vector3(scales[i], scales[i], scales[i]);
            }
            return definedScrewScales;
        }

        /// <summary>
        /// Sets the part to be fixed (won't make the screws screwed in)
        /// Don't recommend using this.
        /// </summary>
        /// <param name="value"></param>
        public void SetPartFixed(bool value)
        {
            this.partFixed = value;
        }
        private ScrewType StringScrewTypeToEnum(string screwType)
        {
            switch (screwType)
            {
                case "screwable_nut":
                    return ScrewType.Nut;
                case "screwable_screw1":
                    return ScrewType.Screw1;
                case "screwable_screw2":
                    return ScrewType.Screw2;
                case "screwable_screw3":
                    return ScrewType.Screw3;
                default:
                    return ScrewType.Screw1;
            }
        }
        private void InitHandDetection()
        {
            selectedItem = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SelectItem");
            selectedItemFSM = selectedItem.GetComponent<PlayMakerFSM>();

            FsmHook.FsmInject(selectedItem, "Hand", new Action(ChangedToHand));
            FsmHook.FsmInject(selectedItem, "Tools", new Action(ChangedToTools));

            _boltingSpeed = PlayMakerGlobals.Instance.Variables.GetFsmFloat("BoltingSpeed");
            _wrenchSize = PlayMakerGlobals.Instance.Variables.GetFsmFloat("ToolWrenchSize");
        }

        private GameObject LoadScrewModelToUse(ScrewType screwType, AssetBundle assets)
        {
            switch (screwType)
            {
                case ScrewType.Nut:
                    return (assets.LoadAsset("screwable_nut.prefab") as GameObject);
                case ScrewType.Screw1:
                    return (assets.LoadAsset("screwable_screw1.prefab") as GameObject);
                case ScrewType.Screw2:
                    return (assets.LoadAsset("screwable_screw2.prefab") as GameObject);
                case ScrewType.Screw3:
                    return (assets.LoadAsset("screwable_screw3.prefab") as GameObject);
                default:
                    return (assets.LoadAsset("screwable_nut.prefab") as GameObject);
            }
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
        /// Adds a clamp model to the parent gameObject. This can be used for tubes to have a realistic connector
        /// </summary>
        /// <param name="position">The position of the clamp on the parent</param>
        /// <param name="rotation">The rotation of the clamp on the parent</param>
        /// <param name="scale">The scale of the clamp on the parent</param>
        public void AddClampModel(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            GameObject clamp = GameObject.Instantiate(clampModel);
            clamp.name = parentGameObject.name + "_CLAMP" + clampsAdded;
            clampsAdded++;
            clamp.transform.SetParent(parentGameObject.transform);
            clamp.transform.localPosition = position;
            clamp.transform.localScale = scale;
            clamp.transform.localRotation = new Quaternion { eulerAngles = rotation };
        }


        /// <summary>
        /// makes the part screwable by adding the screw gameObjects to the parent gameObject
        /// </summary>
        /// <param name="screws">Screws values needed</param>
        /// <param name="screwsScale">The scale to apply to the screw when creating it</param>
        [Obsolete("Only needed for compatibility with older versions", false)]
        private void MakePartScrewable(Screws screws, Vector3[] screwsScale)
        {
            if(screws != null && screws.screwsPositionsLocal != null && screws.screwsRotationLocal != null && screws.screwsTightness != null && screwsScale != null)
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
            }
            screwableLogic = parentGameObject.AddComponent<ScrewableLogic>();
            screwableLogic.SetSavedInformation(screws, screw_material, screw_soundClip, parentGameObject, parentGameObjectCollider, this);

            if (screws.screwsTightness.All(element => element == 8))
            {
                //All Screws tight. Make part fixed
                this.parentGameObjectCollider.enabled = false;
                partFixed = true;
                screwableLogic.SetPartFixed(partFixed);
            }
        }

        /// <summary>
        /// makes the part screwable by adding the screw gameObjects to the parent gameObject
        /// </summary>
        /// <param name="screws">Screws values needed</param>
        /// <param name="screwsScale">The scale to apply to the screw when creating it</param>
        /// <param name="screwArr">The array of screw objects</param>
        private void MakePartScrewable(Screws screws, Vector3[] screwsScale, Screw[] screwArr)
        {
            if (screws != null && screws.screwsPositionsLocal != null && screws.screwsRotationLocal != null && screws.screwsTightness != null && screwsScale != null)
            {
                for (int i = 0; i < screwArr.Length; i++)
                {
                    GameObject screw = GameObject.Instantiate(screwArr[i].model);
                    screw.name = (parentGameObject.name + "_SCREW" + (i + 1));
                    screw.transform.SetParent(parentGameObject.transform);
                    screw.transform.localPosition = screws.screwsPositionsLocal[i];
                    screw.transform.localScale = screwsScale[i];
                    screw.transform.localRotation = new Quaternion { eulerAngles = screws.screwsRotationLocal[i] };
                    screw.layer = LayerMask.NameToLayer("DontCollide");
                    screw.SetActive(true);

                    ScrewInfo screwInfo = screw.AddComponent<ScrewInfo>();

                    screwInfo.tightness = screws.screwsTightness[i];
                    screwInfo.size= screws.screwsSize[i];
                }

                this.parentGameObjectCollider = this.parentGameObject.GetComponent<Collider>();
            }
            screwableLogic = parentGameObject.AddComponent<ScrewableLogic>();
            screwableLogic.SetSavedInformation(screws, screw_material, screw_soundClip, parentGameObject, parentGameObjectCollider, this);

            if (screws.screwsTightness.All(element => element == 8))
            {
                //All Screws tight. Make part fixed
                this.parentGameObjectCollider.enabled = false;
                partFixed = true;
                screwableLogic.SetPartFixed(partFixed);
            }
        }


        [ObsoleteAttribute("This method is obsolete. This is now handled by a Component on each part. No need to call this anymore", true)]
        /// <summary>
        /// This is now obsolete. DO NOT USE THIS.
        /// </summary>
        public void DetectScrewing()
        {
            if (Camera.main != null)
            {
                if (toolInHand == true)
                {
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 8f, 1 << LayerMask.NameToLayer("DontCollide")) != false)
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
            screwableLogic.SetPartFixed(false);
        }

        /// <summary>
        /// You can call this in your ModSettings function.
        /// This will add an optional checkbox allowing users to have the size of the currently looked at screw displayed to them.
        /// </summary>
        public static void ScrewablePartApiSettingsShowSize(Mod mod)
        {
            Settings.AddHeader(mod, "ScrewablePartAPI");
            Settings.AddCheckBox(mod, showScrewSize);
        }

        /// <summary>
        /// <para>Call this in ModApi.Attachable part function "assemble(bool startUp = false) on the static made screwable part AFTER base.assemble(startUp);</para>
        /// <para>call this after checking the screwable part for != null</para>
        /// </summary>
        public void setScrewsOnAssemble()
        {
            if (parentGameObject != null)
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

                for (int i = 0; i < this.screws.screwsPositionsLocal.Length; i++)
                {
                    GameObject tmpScrew = GameObject.Find(parentGameObject.name + "_SCREW" + (i + 1));
                    tmpScrew.transform.localPosition = this.screws.screwsPositionsLocal[i];

                    tmpScrew.transform.localRotation = Quaternion.Euler(this.screws.screwsRotationLocal[i]);
                }
                this.parentGameObjectCollider.enabled = true;
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
                    screwableParts[i].screws = screwableParts[i].screwableLogic.GetSaveInformation();
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

        /// <summary>
        /// Used to display text to the user as a gui text
        /// </summary>
        public static string GuiInteraction
        {
            get
            {
                return PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction").Value;
            }
            set
            {
                PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction").Value = value;
            }
        }

    }
}