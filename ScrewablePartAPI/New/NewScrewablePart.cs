using HutongGames.PlayMaker;
using MSCLoader;
using ScrewablePartAPI.New;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScrewablePartAPI
{
    public class NewScrewablePart
    {
        /// <summary>
        /// Can be used to reduce code size when creating multiple screwablePart by defining once and just passing it
        /// </summary>
        public class ScrewableInitBaseInfo
        {
            public AssetBundle assetBundle;
            public Dictionary<string, int> save;
            public Material material;
            public AudioClip soundClip;
            public GameObject clampModel;

            public ScrewableInitBaseInfo(AssetBundle assetBundle, Dictionary<string, int> save)
            {
                this.assetBundle = assetBundle;
                this.save = save;
                material = assetBundle.LoadAsset<Material>("Screw-Material.mat");
                soundClip = assetBundle.LoadAsset<AudioClip>("screwable_sound.wav");
                clampModel = assetBundle.LoadAsset<GameObject>("Tube_Clamp.prefab");
            }

        }

        public GameObject parent;
        public NewScrew[] screws;
        private NewScrewableLogic screwableLogic;
        private bool ratchetInHand = false;
        private bool ratchetSwitch = false;
        public ScrewableInitBaseInfo baseInfo;

        private int clampsAdded = 0;

        public const int maxTightness = 8;
        private float rotationStep { get { return (360 / maxTightness); } }
        private float transformStep = 0.0008f;

        public bool partFixed
        {
            get
            {
                return screwableLogic.partFixed;
            }
        }

        public static Settings showScrewSize = new Settings("showScrewSize", "Show screw size", false);

        public NewScrewablePart(ScrewableInitBaseInfo baseInfo, string id, GameObject parent, NewScrew[] screws)
        {
            this.parent = parent;
            this.baseInfo = baseInfo;
            this.screws = screws;
            foreach (NewScrew screw in screws)
            {
                screw.model = LoadScrewModelToUse(screw.type, baseInfo.assetBundle);
            }
            MakeScrewable(parent, screws, baseInfo, id);
        }

        public static void SetupModSettings(Mod mod)
        {
            Settings.AddHeader(mod, "ScrewablePartAPI");
            Settings.AddCheckBox(mod, showScrewSize);
        }

        public void resetScrewsOnDisassemble()
        {
            foreach(NewScrew screw in screws)
            {
                screw.tightness = 0;
                screw.gameObject.transform.localPosition = new Vector3(screw.position.x, screw.position.y, screw.position.z);
                screw.gameObject.transform.localRotation = new Quaternion { eulerAngles = screw.rotation };
            }

            screwableLogic.partFixed = false;
        }
        public void setScrewsOnAssemble()
        {
            if (parent != null)
            {
                foreach (NewScrew screw in screws)
                {
                    screw.tightness = 0;
                    screw.gameObject.transform.localPosition = new Vector3(screw.position.x, screw.position.y, screw.position.z);
                    screw.gameObject.transform.localRotation = new Quaternion { eulerAngles = new Vector3(screw.rotation.x, screw.rotation.y, screw.rotation.z) };
                }

                screwableLogic.partFixed = false;
                this.parent.GetComponent<Collider>().enabled = true;
            }
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

        private void MakeScrewable(GameObject parent, NewScrew[] screws, ScrewableInitBaseInfo baseInfo, string id)
        {
            for(int i = 0; i < screws.Length; i++)
            {
                string screwId = id + "_screw" + 1;
                int loadedTightness;
                try
                {
                    loadedTightness = baseInfo.save[screwId];
                }
                catch
                {
                    loadedTightness = 0;
                }

                NewScrew screw = screws[i];
                screw.gameObject = GameObject.Instantiate(screw.model);
                screw.gameObject = Helper.SetObjectNameTagLayer(screw.gameObject, String.Format("{0}_SCREW{1}", parent.name, i + 1), "DontCollide");
                screw.gameObject.transform.SetParent(parent.transform);
                screw.gameObject.transform.localPosition = new Vector3(screw.position.x, screw.position.y, screw.position.z);
                screw.gameObject.transform.localRotation = new Quaternion { eulerAngles = new Vector3(screw.rotation.x, screw.rotation.y, screw.rotation.z) };
                screw.gameObject.SetActive(true);

                ScrewInfo screwInfo = screw.gameObject.AddComponent<ScrewInfo>();

                //Set screw tightness loaded from save


                screwInfo.tightness = loadedTightness;
                screwInfo.size = screw.size;
            }
            screwableLogic = parent.AddComponent<NewScrewableLogic>();
            screwableLogic.Init(screws, baseInfo, parent, this);
        }

        public void ScrewIn(GameObject hitScrew, int index, bool useAudio = true)
        {
            NewScrew screw = screws[index];
            if (screw.tightness >= 0 && screw.tightness <= maxTightness - 1)
            {
                if (useAudio)
                {
                    AudioSource.PlayClipAtPoint(baseInfo.soundClip, hitScrew.transform.position);
                }
                hitScrew.transform.Rotate(0, 0, rotationStep);
                hitScrew.transform.Translate(0f, 0f, -transformStep);

                screw.tightness++;
                UpdateScrewInfoTightness(hitScrew, screw.tightness);
            }
        }

        public void UpdateScrewInfoTightness(GameObject screw, int tightness)
        {
            ScrewInfo screwInfo = screw.GetComponent<ScrewInfo>();
            if (screwInfo != null)
            {
                screwInfo.tightness = tightness;
            }
        }

        public void ScrewOut(GameObject hitScrew, int index, bool useAudio = true)
        {
            NewScrew screw = screws[index];
            if (screw.tightness > 0 && screw.tightness <= maxTightness)
            {
                if (useAudio)
                {
                    AudioSource.PlayClipAtPoint(baseInfo.soundClip, hitScrew.transform.position);
                }
                
                hitScrew.transform.Rotate(0, 0, -rotationStep);
                hitScrew.transform.Translate(0f, 0f, transformStep);

                screw.tightness--;
                UpdateScrewInfoTightness(hitScrew, screw.tightness);
            }
            screwableLogic.partFixed = false;
        }

        private GameObject LoadScrewModelToUse(NewScrew.Type screwType, AssetBundle assets)
        {
            switch (screwType)
            {
                case NewScrew.Type.Nut:
                    return (assets.LoadAsset("screwable_nut.prefab") as GameObject);
                case NewScrew.Type.Screw1:
                    return (assets.LoadAsset("screwable_screw1.prefab") as GameObject);
                case NewScrew.Type.Screw2:
                    return (assets.LoadAsset("screwable_screw2.prefab") as GameObject);
                case NewScrew.Type.Screw3:
                    return (assets.LoadAsset("screwable_screw3.prefab") as GameObject);
                default:
                    return (assets.LoadAsset("screwable_nut.prefab") as GameObject);
            }
        }
    }
}