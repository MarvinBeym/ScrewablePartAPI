using MSCLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
namespace ScrewablePartAPI.V2
{
    /// <summary>
    /// Class that can be used to define basic information
    /// </summary>
    public class ScrewablePartV2BaseInfo
    {
        /// <summary>
        /// The full path to the screws save.
        /// </summary>
        public Dictionary<string, int[]> save;
        /// <summary>
        /// Defines if the screws created will show there size to the user when user looks at the screw
        /// </summary>
        public bool showScrewSize;
        /// <summary>
        /// Constructor for base info class
        /// </summary>
        /// <param name="savePath">path to screws save</param>
        /// <param name="showScrewSize">Should the screw size be shown to the user</param>
        public ScrewablePartV2BaseInfo(string savePath, bool showScrewSize)
        {
            save = ScrewablePartV2.LoadSave(savePath);
            this.showScrewSize = showScrewSize;
        }
    }

    /// <summary>
    /// The main class that handles everything
    /// </summary>
    public class ScrewablePartV2
    {
        /// <summary>
        /// The version of the api
        /// </summary>
        public static string version = "";

        private string id;
        private int clampsAdded = 0;
        private float rotationStep;
        private float transformStep;
        internal int maxTightness;

        private GameObject parent;
        private ScrewV2[] screws;
        private ScrewablePartLogicV2 logic;
        private string saveFilePath;

        private Dictionary<string, int[]> save;

        /// <summary>
        /// Returns if the part is fixed (all screws have reached the maximum tightness)
        /// </summary>
        public bool partFixed
        {
            get
            {
                return logic.partFixed;
            }
        }

        /// <summary>
        /// The object constructor
        /// </summary>
        /// <param name="baseInfo">Base info containing general setup</param>
        /// <param name="id">The id used for loading of the save. It is recommended to use the parent.name for this</param>
        /// <param name="parent">The parent gameobject the screws will be added to as childs</param>
        /// <param name="screws">An array of all defined screws</param>
        public ScrewablePartV2(ScrewablePartV2BaseInfo baseInfo, string id, GameObject parent, ScrewV2[] screws)
        {
            save = baseInfo.save;

            maxTightness = 8;
            rotationStep = 360 / maxTightness;
            transformStep = 0.0008f;
            this.id = id;
            this.parent = parent;
            this.screws = screws;

            LoadTightness(save, id, screws);

            InitScrewable(parent, screws, baseInfo.showScrewSize);
        }

        /// <summary>
        /// This has to be called when your parts get assembled
        /// It's up to you to call this.
        /// I recommend using ModApi and somehow adding this to your overriden assemble function
        /// </summary>
        public void OnPartAssemble()
        {
            foreach (ScrewV2 screw in screws)
            {
                screw.tightness = 0;
                screw.gameObject.transform.localPosition = Helper.CopyVector3(screw.position);
                screw.gameObject.transform.localRotation = new Quaternion { eulerAngles = Helper.CopyVector3(screw.rotation) };
            }
            logic.partFixed = false;
            logic.parentCollider.enabled = true;
        }

        /// <summary>
        /// This has to be called when your parts get disassembled
        /// It's up to you to call this.
        /// I recommend using ModApi and somehow adding this to your overriden disassemble function
        /// </summary>
        public void OnPartDisassemble()
        {
            foreach(ScrewV2 screw in screws)
            {
                screw.tightness = 0;
                screw.gameObject.transform.localPosition = Helper.CopyVector3(screw.position);
                screw.gameObject.transform.localRotation = new Quaternion { eulerAngles = Helper.CopyVector3(screw.rotation) };
            }
            logic.partFixed = false;
            logic.parentCollider.enabled = true;
        }

        /// <summary>
        /// Small helper function that will load the save as a dictionary or return a new dictionary if the file wasn't found
        /// </summary>
        /// <param name="saveFilePath">The path to the save file</param>
        /// <returns>dictionary containing all the saved values key equals id used for creation</returns>
        public static Dictionary<string, int[]> LoadSave(string saveFilePath)
        {
            if (File.Exists(saveFilePath))
            {
                string serializedData = File.ReadAllText(saveFilePath);
                return JsonConvert.DeserializeObject<Dictionary<string, int[]>>(serializedData);
            }
            return new Dictionary<string, int[]>();
        }

        /// <summary>
        /// This function adds a simple clamp model to the parent calling it {parent name}_CLAMP{clampIndex}
        /// </summary>
        /// <param name="position">The position on the parent to place this clamp</param>
        /// <param name="rotation">The rotation on the parent to place this clamp</param>
        /// <param name="scale">The scale of the model of the clamp</param>
        public void AddClampModel(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            GameObject clamp = GameObject.Instantiate(ScrewablePartV2Mod.clampModel);
            clamp.name = parent.name + "_CLAMP" + (clampsAdded + 1);
            clampsAdded++;
            clamp.transform.SetParent(parent.transform);
            clamp.transform.localPosition = position;
            clamp.transform.localScale = scale;
            clamp.transform.localRotation = new Quaternion { eulerAngles = rotation };
        }

        /// <summary>
        /// Loads the tightness and incase something goes wrong on loading 
        /// (like difference between defined screws and loaded screws will reset the values)
        /// </summary>
        /// <param name="save">The loaded dictionary of save information</param>
        /// <param name="id">The id of the screw</param>
        /// <param name="screws">An array of all the screws</param>
        private void LoadTightness(Dictionary<string, int[]> save, string id, ScrewV2[] screws)
        {
            int[] loadedTightness;
            try
            {
                loadedTightness = save[id];
            }
            catch { loadedTightness = null; }

            if(loadedTightness == null || loadedTightness.Length != screws.Length)
            {
                loadedTightness = new int[screws.Length];
                for(int i = 0; i < screws.Length; i++)
                {
                    loadedTightness[i] = 0;
                }
            }

            for(int i = 0; i < screws.Length; i++)
            {
                screws[i].tightness = loadedTightness[i];
            }
        }

        /// <summary>
        /// Initializes the screwable part
        /// </summary>
        /// <param name="parent">The parent where the screws are supposed to be placed on</param>
        /// <param name="screws">The array of screws to initialize</param>
        /// <param name="showScrewSize">Auto set. This is used by the logic to detect if the screw size can be shown to the user</param>
        private void InitScrewable(GameObject parent, ScrewV2[] screws, bool showScrewSize)
        {
            for(int i = 0; i < screws.Length; i++)
            {
                ScrewV2 screw = screws[i];
                screw.id = String.Format("{0}_SCREW{1}", parent.name, i + 1);
                screw.gameObject = CreateScrewModel(screw.id, screw.position, screw.rotation, new Vector3(screw.scale, screw.scale, screw.scale), screw.type);
                screw.renderer = screw.gameObject.GetComponentsInChildren<MeshRenderer>(true)[0];
                int tmpTigness = screw.tightness;
                screw.tightness = 0;
                screw.showSize = showScrewSize;
                for(int j = 0; j < tmpTigness; j++)
                {
                    ScrewIn(screw, false);
                }
            }
            
            logic = parent.AddComponent<ScrewablePartLogicV2>();
            logic.Init(parent, screws, this);
            logic.CheckAllScrewsTight(screws);
        }

        /// <summary>
        /// Screw in the passed screw by one tightness
        /// </summary>
        /// <param name="screw">The screw to screw in once</param>
        /// <param name="useAudio">Should audio be played</param>
        internal void ScrewIn(ScrewV2 screw, bool useAudio = true)
        {
            if (screw.tightness < maxTightness)
            {
                if (useAudio)
                {
                    AudioSource.PlayClipAtPoint(ScrewablePartV2Mod.soundClip, screw.gameObject.transform.position);
                }
                screw.gameObject.transform.Rotate(0, 0, rotationStep);
                screw.gameObject.transform.Translate(0f, 0f, -transformStep);

                screw.tightness++;
                //UpdateScrewInfoTightness(hitScrew, screw.tightness);
            }
        }

        /// <summary>
        /// Screw out the passed screw by one tightness
        /// </summary>
        /// <param name="screw">The screw to screw out once</param>
        /// <param name="useAudio">Should audio be played</param>
        internal void ScrewOut(ScrewV2 screw, bool useAudio = true)
        {
            if (screw.tightness > 0)
            {
                if (useAudio)
                {
                    AudioSource.PlayClipAtPoint(ScrewablePartV2Mod.soundClip, screw.gameObject.transform.position);
                }

                screw.gameObject.transform.Rotate(0, 0, -rotationStep);
                screw.gameObject.transform.Translate(0f, 0f, transformStep);

                screw.tightness--;
            }
            logic.partFixed = false;
        }

        /// <summary>
        /// This function saves the screws into the config folder of your mod
        /// </summary>
        /// <param name="mod">Your mod object (usually "this")</param>
        /// <param name="screwableParts">An array of all the screwable parts in your mod you want to save</param>
        /// <param name="saveFile">The save file name</param>
        public static void SaveScrews(Mod mod, ScrewablePartV2[] screwableParts, string saveFile)
        {
            Dictionary<string, int[]> saveDictionary = new Dictionary<string, int[]>();
            foreach(ScrewablePartV2 screwablePart in screwableParts)
            {
                int[] tightnessArr = new int[screwablePart.screws.Length];
                for(int i = 0; i < screwablePart.screws.Length; i++)
                {
                    ScrewV2 screw = screwablePart.screws[i];
                    tightnessArr[i] = screw.tightness;
                }
                saveDictionary[screwablePart.id] = tightnessArr;
            }

            SaveLoad.SerializeSaveFile<Dictionary<string, int[]>>(mod, saveDictionary, saveFile);
        }


        /// <summary>
        /// Creates the screws model based on the passed screw type
        /// </summary>
        /// <param name="name">The name of the screw under which it can be found in the game</param>
        /// <param name="position">The local position on the parent object</param>
        /// <param name="rotation">The local rotation on the parent object</param>
        /// <param name="scale">The local scale on the parent object</param>
        /// <param name="screwType">The type this screw should be</param>
        /// <returns></returns>
        private GameObject CreateScrewModel(string name, Vector3 position, Vector3 rotation, Vector3 scale, ScrewV2.Type screwType)
        {
            GameObject screw;
            switch (screwType)
            {
                case ScrewV2.Type.Nut:
                    screw = GameObject.Instantiate(ScrewablePartV2Mod.nutModel);
                    break;
                case ScrewV2.Type.Screw1:
                    screw = GameObject.Instantiate(ScrewablePartV2Mod.screw1Model);
                    break;
                case ScrewV2.Type.Screw2:
                    screw = GameObject.Instantiate(ScrewablePartV2Mod.screw2Model);
                    break;
                case ScrewV2.Type.Screw3:
                    screw = GameObject.Instantiate(ScrewablePartV2Mod.screw3Model);
                    break;
                default:
                    screw = GameObject.Instantiate(ScrewablePartV2Mod.screw2Model);
                    break;
            }
            screw = Helper.SetObjectNameTagLayer(screw, name, "DontCollide");
            screw.transform.SetParent(parent.transform);
            screw.transform.localPosition = Helper.CopyVector3(position);
            screw.transform.localRotation = new Quaternion { eulerAngles = Helper.CopyVector3(rotation) };
            screw.transform.localScale = Helper.CopyVector3(scale);
            screw.SetActive(true);
            return screw;
        }
    }
}