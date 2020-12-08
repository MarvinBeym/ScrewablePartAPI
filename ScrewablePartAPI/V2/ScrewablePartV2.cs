using MSCLoader;
using ScrewablePartAPI.New;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ScrewablePartAPI.NewScrewablePart;
#pragma warning disable CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
namespace ScrewablePartAPI.V2
{
    public class ScrewableBaseInfo
    {
        public AssetBundle assetBundle;
        public Dictionary<string, int[]> save;
        public Material material;
        public AudioClip soundClip;
        public GameObject clampModel;

        public GameObject nutModel;
        public GameObject screw1Model;
        public GameObject screw2Model;
        public GameObject screw3Model;

        public ScrewableBaseInfo(AssetBundle assetBundle, Dictionary<string, int[]> save)
        {
            this.assetBundle = assetBundle;
            this.save = save;
            material = assetBundle.LoadAsset<Material>("Screw-Material.mat");
            soundClip = assetBundle.LoadAsset<AudioClip>("screwable_sound.wav");
            clampModel = assetBundle.LoadAsset<GameObject>("Tube_Clamp.prefab");

            nutModel = assetBundle.LoadAsset<GameObject>("screwable_nut.prefab");
            screw1Model = assetBundle.LoadAsset<GameObject>("screwable_screw1.prefab");
            screw2Model = assetBundle.LoadAsset<GameObject>("screwable_screw2.prefab");
            screw3Model = assetBundle.LoadAsset<GameObject>("screwable_screw3.prefab");
        }

    }
    public class ScrewablePartV2
    {
        private string id;
        private ScrewableBaseInfo baseInfo;
        private int clampsAdded = 0;
        private float rotationStep;
        private float transformStep;
        public int maxTightness;

        private GameObject parent;
        private ScrewV2[] screws;
        private ScrewablePartLogicV2 logic;

        private static Settings showScrewSize = new Settings("showScrewSize", "Show screw size", false);
        public bool partFixed
        {
            get
            {
                return logic.partFixed;
            }
        }

        public static void SetupModSettings(Mod mod)
        {
            Settings.AddHeader(mod, "ScrewablePartAPI");
            Settings.AddCheckBox(mod, showScrewSize);
        }

        public ScrewablePartV2(ScrewableBaseInfo baseInfo, string id, GameObject parent, ScrewV2[] screws)
        {
            maxTightness = 8;
            rotationStep = 360 / maxTightness;
            transformStep = 0.0008f;
            this.baseInfo = baseInfo;
            this.id = id;
            this.parent = parent;
            this.screws = screws;

            LoadTightness(baseInfo.save, id, screws);

            InitScrewable(baseInfo, id, parent, screws);
        }


        public void OnPartAssemble()
        {

        }

        public void OnPartDisassemble()
        {
            foreach(ScrewV2 screw in screws)
            {
                screw.tightness = 0;
                screw.gameObject.transform.localPosition = Helper.CopyVector3(screw.position);
                screw.gameObject.transform.localRotation = new Quaternion { eulerAngles = Helper.CopyVector3(screw.rotation) };
            }
            logic.partFixed = false;
        }

        public void AddClampModel(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            GameObject clamp = GameObject.Instantiate(baseInfo.clampModel);
            clamp.name = parent.name + "_CLAMP" + (clampsAdded + 1);
            clampsAdded++;
            clamp.transform.SetParent(parent.transform);
            clamp.transform.localPosition = position;
            clamp.transform.localScale = scale;
            clamp.transform.localRotation = new Quaternion { eulerAngles = rotation };
        }

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

        private void InitScrewable(ScrewableBaseInfo baseInfo, string id, GameObject parent, ScrewV2[] screws)
        {
            for(int i = 0; i < screws.Length; i++)
            {
                ScrewV2 screw = screws[i];
                screw.id = String.Format("{0}_SCREW{1}", parent.name, i + 1);
                screw.gameObject = CreateScrewModel(screw.id, screw.position, screw.rotation, new Vector3(screw.scale, screw.scale, screw.scale), screw.type);
                screw.screwInfo = screw.gameObject.AddComponent<ScrewInfo>();
                screw.screwInfo.tightness = screw.tightness;
                screw.screwInfo.size = screw.size;
                screw.renderer = screw.gameObject.GetComponentInChildren<MeshRenderer>();
                int tmpTigness = screw.tightness;
                screw.tightness = 0;
                for(int j = 0; j < tmpTigness; j++)
                {
                    ScrewIn(screw, false);
                }
            }
            logic = parent.AddComponent<ScrewablePartLogicV2>();
            logic.Init(baseInfo, parent, screws, this);
        }

        internal void ScrewIn(ScrewV2 screw, bool useAudio = true)
        {
            if (screw.tightness < maxTightness)
            {
                if (useAudio)
                {
                    AudioSource.PlayClipAtPoint(baseInfo.soundClip, screw.gameObject.transform.position);
                }
                screw.gameObject.transform.Rotate(0, 0, rotationStep);
                screw.gameObject.transform.Translate(0f, 0f, -transformStep);

                screw.tightness++;
                //UpdateScrewInfoTightness(hitScrew, screw.tightness);
            }
        }

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

        internal void ScrewOut(ScrewV2 screw, bool useAudio = true)
        {
            if (screw.tightness > 0)
            {
                if (useAudio)
                {
                    AudioSource.PlayClipAtPoint(baseInfo.soundClip, screw.gameObject.transform.position);
                }

                screw.gameObject.transform.Rotate(0, 0, -rotationStep);
                screw.gameObject.transform.Translate(0f, 0f, transformStep);

                screw.tightness--;
            }
            logic.partFixed = false;
        }

        private GameObject CreateScrewModel(string name, Vector3 position, Vector3 rotation, Vector3 scale, ScrewV2.Type screwType)
        {
            GameObject screw;
            switch (screwType)
            {
                case ScrewV2.Type.Nut:
                    screw = GameObject.Instantiate(baseInfo.nutModel);
                    break;
                case ScrewV2.Type.Screw1:
                    screw = GameObject.Instantiate(baseInfo.screw1Model);
                    break;
                case ScrewV2.Type.Screw2:
                    screw = GameObject.Instantiate(baseInfo.screw2Model);
                    break;
                case ScrewV2.Type.Screw3:
                    screw = GameObject.Instantiate(baseInfo.screw3Model);
                    break;
                default:
                    screw = GameObject.Instantiate(baseInfo.screw2Model);
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
#pragma warning restore CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element