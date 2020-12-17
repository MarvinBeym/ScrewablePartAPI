using Ionic.Zip;
using MSCLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ScrewablePartAPI.V2
{

    public class ScrewablePartV2Mod : Mod
    {
        private class UpdateCheckResponse
        {
            public string message { get; set; } = "";
            public string available { get; set; } = "";
        }

        public override string ID => "ScrewablePartAPI";
        public override string Name => "ScrewablePartAPI";
        public override string Version => "2.1.0";

        public override string Author => "DonnerPlays";
        public override bool UseAssetsFolder => true;
        public override bool LoadInMenu => true;

        private const string assetsFile = "screwableapi.unity3d";

        private AssetBundle assetBundle;
        internal static Material material;
        internal static AudioClip soundClip;
        internal static GameObject clampModel;
        internal static GameObject nutModel;
        internal static GameObject screw1Model;
        internal static GameObject screw2Model;
        internal static GameObject screw3Model;

        //Updater stuff
        private GameObject interfaceObject;
        private GameObject interfaceActive;
        private GameObject quad7;
        private UpdateCheckResponse updateCheckResponse;
        private string modsFolderFilePath;

        //Current & new file paths
        private string dllFilePath;
        private string xmlFilePath;
        private string assetsBundleFilePath;

        //Old file paths
        private string old_dllFilePath;
        private string old_xmlFilePath;
        private string old_assetsBundleFilePath;

        
        private string getUpdateUrl(string version)
        {
            return $"http://localhost/web/msc/screwablepartapi/public/versions/{version}.zip";
        }
        public override void OnMenuLoad()
        {
            ScrewablePartV2.version = this.Version;
            int errorsDetected = 0;
            modsFolderFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            dllFilePath = Path.Combine(modsFolderFilePath, $"{ID}.dll");
            xmlFilePath = Path.Combine(modsFolderFilePath, $"{ID}.xml");
            assetsBundleFilePath = Path.Combine(ModLoader.GetModAssetsFolder(this), assetsFile);

            old_dllFilePath = dllFilePath.Replace(".dll", ".old_dll");
            old_xmlFilePath = xmlFilePath.Replace(".xml", ".old_xml");
            old_assetsBundleFilePath = assetsBundleFilePath.Replace(".unity3d", ".old_unity3d");
            CheckForOldFiles();
            try
            {
                interfaceObject = GameObject.Find("Interface");
                interfaceActive = GameObject.Find("Quad 7");
                quad7 = GameObject.Find("InterfaceActive");
            }
            catch{}

            
            //Temp
            updateCheckResponse = new UpdateCheckResponse
            {
                message = "out-dated",
                available = "2.1.0"
            };

            switch (updateCheckResponse.message)
            {
                case "out-dated":
                    ModConsole.Warning($"ScrewablePartAPI outdated. version {updateCheckResponse.available} available");
                    SetMenuVisibility(false);
                    Helper.ShowCustom2ButtonMessage($"ScrewablePartAPI is outdated\n" +
                        $"version {updateCheckResponse.available} is available on GitHub\n" +
                        $"Do you want to update automatically?\n" +
                        $"(Restart will be required)\n" +
                        $"This can break mods using outdated versions", "ScrewablePartAPI is outdated", UpdateMessageNoClicked, UpdateMessageYesClicked);
                    break;
            }

            if(errorsDetected == 0)
            {
                assetBundle = Helper.LoadAssetBundle(this, assetsFile);
                material = assetBundle.LoadAsset<Material>("Screw-Material.mat");
                soundClip = assetBundle.LoadAsset<AudioClip>("screwable_sound.wav");
                clampModel = assetBundle.LoadAsset<GameObject>("Tube_Clamp.prefab");

                nutModel = assetBundle.LoadAsset<GameObject>("screwable_nut.prefab");
                screw1Model = assetBundle.LoadAsset<GameObject>("screwable_screw1.prefab");
                screw2Model = assetBundle.LoadAsset<GameObject>("screwable_screw2.prefab");
                screw3Model = assetBundle.LoadAsset<GameObject>("screwable_screw3.prefab");
                assetBundle.Unload(false);
            }

        }

        private void SetMenuVisibility(bool state)
        {
            try
            {
                interfaceObject.SetActive(state);
                interfaceActive.SetActive(state);
                quad7.SetActive(state);
            }
            catch { }
            
        }

        private void CheckForOldFiles()
        {
            int filesDeletedCounter = 0;
            if (File.Exists(old_dllFilePath)) { filesDeletedCounter++; File.Delete(old_dllFilePath); }
            if (File.Exists(old_xmlFilePath)) { filesDeletedCounter++; File.Delete(old_xmlFilePath); }
            if (File.Exists(old_assetsBundleFilePath)) { filesDeletedCounter++; File.Delete(old_assetsBundleFilePath); }

            if(filesDeletedCounter > 0)
            {
                ModUI.ShowMessage(
                    $"Old api files have been deleted\n" +
                    $"{filesDeletedCounter} of {3} have been deleted\n" +
                    $"If you did an update before it is now finished", "ScrewablePartAPI Updater");
            }

        }

        private void UpdateMessageYesClicked()
        {
            string newVersionString = updateCheckResponse.available.Replace(".", "_");
            string downloadFilePath = Path.Combine(modsFolderFilePath, $"{ID}_{newVersionString}_update.zip");

            string oldDllFile = Assembly.GetExecutingAssembly().Location;
            string oldXmlFile = Assembly.GetExecutingAssembly().Location;
            oldXmlFile = oldXmlFile.Replace(".dll", ".xml");
            string oldAssetsFile = Path.Combine(ModLoader.GetModAssetsFolder(this), assetsFile);
            
            

            string newDllFile = oldDllFile;
            string newAssetsFile = oldAssetsFile;
            string newXmlFile = oldXmlFile;

            oldDllFile = oldDllFile.Replace($"{ID}.dll", $"{ID}.old_dll");
            oldAssetsFile = oldAssetsFile.Replace(assetsFile, "screwableapi.old_unity3d");
            oldXmlFile = oldXmlFile.Replace($"{ID}.xml", $"{ID}.old_xml");

            RenameFile(newDllFile, oldDllFile);
            RenameFile(newXmlFile, oldXmlFile);
            RenameFile(newAssetsFile, oldAssetsFile);

            if (!File.Exists(downloadFilePath))
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(getUpdateUrl(newVersionString), downloadFilePath);
                }
            }
            try
            {
                int fileRenameCounter = 0;
                using (ZipFile zip = new ZipFile(downloadFilePath))
                {
                    List<ZipEntry> filesToExtract = new List<ZipEntry>();

                    foreach (ZipEntry zipEntry in zip.Entries)
                    {

                        if (zipEntry.IsDirectory)
                        {
                            continue;
                        }
                        filesToExtract.Add(zipEntry);
                    }

                    
                    foreach (ZipEntry entry in filesToExtract)
                    {
                        if (entry.FileName.EndsWith(assetsFile))
                        {
                            entry.FileName = Path.GetFileName(newAssetsFile);
                            entry.Extract(Path.GetDirectoryName(newAssetsFile), ExtractExistingFileAction.OverwriteSilently);
                            fileRenameCounter++;
                        }
                        else if (entry.FileName.EndsWith($"{ID}.dll"))
                        {
                            entry.FileName = Path.GetFileName(newDllFile);
                            entry.Extract(Path.GetDirectoryName(newDllFile), ExtractExistingFileAction.OverwriteSilently);
                            fileRenameCounter++;
                        }
                        else if (entry.FileName.EndsWith($"{ID}.xml"))
                        {
                            entry.FileName = Path.GetFileName(newXmlFile);
                            entry.Extract(Path.GetDirectoryName(newXmlFile), ExtractExistingFileAction.OverwriteSilently);
                            fileRenameCounter++;
                        }
                    }
                }

                if(fileRenameCounter == 3)
                {
                    ShowYesNoMessage("The game has to be closed and restarted\n" +
                        "Game will close when you click YES\n" +
                        "If you press NO the game won't be playable... !RESTART!\n" +
                        "After the restart, the old files will be removed", "Update downloaded", ExitGame, ExitGame);
                }


            }
            catch (Exception ex)
            {
                ModUI.ShowMessage($"Update failed, please update manually\n ex: {ex.Message}");
            }
            File.Delete(downloadFilePath);
        }

        private void ExitGame()
        {
            Application.Quit();
        }

        private void ShowYesNoMessage(string text, string header, Action YesAction, UnityAction NoAction)
        {
            ModUI.ShowYesNoMessage(text, header, YesAction);
            try
            {
                Button noButton = GameObject.Find("MSCLoader MB(Clone)").transform.FindChild("Content").FindChild("YesNo").FindChild("Button").GetComponent<Button>();
                noButton.onClick.AddListener(NoAction);
            }
            catch
            {
                NoAction.Invoke();
            }
        }

        private void RenameFile(string currentFile, string newFile)
        {
            if(File.Exists(currentFile) && !File.Exists(newFile))
            {
                File.Move(currentFile, newFile);
            }
        }

        private void ExtractSingleZipEntry(ZipEntry zipEntry, string fullPathToFile)
        {
            zipEntry.FileName = Path.GetFileName(fullPathToFile);
            zipEntry.Extract(Path.GetDirectoryName(fullPathToFile), ExtractExistingFileAction.OverwriteSilently);
        }

        private void UpdateMessageNoClicked()
        {
            SetMenuVisibility(true);
        }
    }
}
