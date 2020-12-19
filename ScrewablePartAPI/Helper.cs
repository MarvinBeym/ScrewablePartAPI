using MSCLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Ping = System.Net.NetworkInformation.Ping;
namespace ScrewablePartAPI
{
    /// <summary>
    /// A class containing helper functions
    /// </summary>
    internal class Helper
    {

        internal static string MakeGetRequest(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                throw new Exception("Get request failed for some unknown reason.");
            }

        }
        internal static bool ServerReachable(string host)
        {
            using (Ping pingSender = new Ping())
            {
                try
                {
                    PingReply reply = pingSender.Send(host);
                    return (reply.Status == IPStatus.Success);
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }
        internal static string CombinePaths(params string[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException("paths");
            }
            return paths.Aggregate(Path.Combine);
        }
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
                ModUI.ShowYesNoMessage(message + "\n\nClose Game? - RECOMMENDED", ExitGame);
            }
            return null;
        }
        
        internal static void ExitGame()
        {
            Application.Quit();
        }

        internal static void ShowCustom2ButtonMessage(string text, string header, UnityAction button1Action, UnityAction button2Action, string button1Text = "Cancel", string button2Text = "Ok")
        {
            //ModUI.ShowYesNoMessage(text, header, button2Action);
            GameObject messageBox = GameObject.Instantiate(ModUI.messageBox);
            messageBox.transform.SetParent(ModUI.GetCanvas().transform);
            messageBox.transform.localPosition = new Vector3(0, 0, 0);
            messageBox.transform.localRotation = new Quaternion { eulerAngles = new Vector3(0, 0, 0) };
            messageBox.transform.localScale = new Vector3(1, 1, 1);
            messageBox.name = "Custom2ButtonMessage";

            //General game objects
            GameObject content = messageBox.transform.FindChild("Content").gameObject;
            GameObject yesNo = content.transform.FindChild("YesNo").gameObject;
            GameObject button1Obj = yesNo.transform.FindChild("Button").gameObject;
            GameObject button2Obj = yesNo.transform.FindChild("Button 1").gameObject;

            //Text objects
            Text headerText = messageBox.transform.FindChild("Title").FindChild("Text").GetComponent<Text>();
            Text messageText = content.transform.FindChild("Text").GetComponent<Text>();
            Text button1TextObj = button1Obj.transform.FindChild("Text").GetComponent<Text>();
            Text button2TextObj = button2Obj.transform.FindChild("Text").GetComponent<Text>();

            //Button objects
            Button button1 = button1Obj.GetComponent<Button>();
            Button button2 = button2Obj.GetComponent<Button>();

            headerText.text = header;
            messageText.text = text;
            button1TextObj.text = button1Text;
            button2TextObj.text = button2Text;

            UnityAction removeMessageBoxAction = new UnityAction(delegate ()
            {
                GameObject.Destroy(messageBox);
            });

            button1.onClick.AddListener(removeMessageBoxAction);
            button2.onClick.AddListener(removeMessageBoxAction);

            button1.onClick.AddListener(button1Action);
            button2.onClick.AddListener(button2Action);

            yesNo.SetActive(true);
            messageBox.SetActive(true);
            /*
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
            */
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
