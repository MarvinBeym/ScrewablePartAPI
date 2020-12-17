using MSCLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ScrewablePartAPI
{
    /// <summary>
    /// A class containing helper functions
    /// </summary>
    internal class Helper
    {
        internal static T LoadSaveOrReturnNew<T>(string saveFilePath) where T : new()
        {
            if (File.Exists(saveFilePath))
            {
                string serializedData = File.ReadAllText(saveFilePath);
                return JsonConvert.DeserializeObject<T>(serializedData);
            }
            return new T();
        }
        internal static AssetBundle LoadAssetBundle(Mod mod, string fileName)
        {
            try
            {
                return LoadAssets.LoadBundle(mod, fileName);
            }
            catch (Exception ex)
            {
                string message = String.Format("AssetBundle file '{0}' could not be loaded", fileName);
                ModConsole.Error(message);
                ModUI.ShowYesNoMessage(message + "\n\nClose Game? - RECOMMENDED", delegate ()
                {
                    Application.Quit();
                });
            }
            return null;
        }
        internal static void ShowCustom2ButtonMessage(string text, string header, UnityAction button1Action, Action button2Action, string button1Text = "Cancel", string button2Text = "Ok")
        {
            ModUI.ShowYesNoMessage(text, header, button2Action);
            try
            {
                Button button1 = GameObject.Find("MSCLoader MB(Clone)").transform.FindChild("Content").FindChild("YesNo").FindChild("Button").GetComponent<Button>();
                Button button2 = GameObject.Find("MSCLoader MB(Clone)").transform.FindChild("Content").FindChild("YesNo").FindChild("Button 1").GetComponent<Button>();

                Text button1TextObject = button1.gameObject.GetComponentInChildren<Text>();
                Text button2TextObject = button2.gameObject.GetComponentInChildren<Text>();

                button1TextObject.text = button1Text;
                button2TextObject.text = button2Text;

                if(button1Action != null)
                {
                    button1.onClick.AddListener(button1Action);
                }
                
            }
            catch { }
        }

        internal static PlayMakerFSM FindFsmOnGameObject(GameObject gameObject, string fsmName)
        {
            foreach (PlayMakerFSM fSM in gameObject.GetComponents<PlayMakerFSM>())
            {
                if (fSM.FsmName == fsmName) { return fSM; }
            }
            return null;
        }
        internal static Vector3 CopyVector3(Vector3 old)
        {
            return new Vector3(old.x, old.y, old.z);
        }
        internal static GameObject SetObjectNameTagLayer(GameObject gameObject, string name, string layer = "Parts", string tag = "PART")
        {
            gameObject.name = name;
            gameObject.tag = tag;

            gameObject.layer = LayerMask.NameToLayer(layer);
            return gameObject;
        }
    }
}
