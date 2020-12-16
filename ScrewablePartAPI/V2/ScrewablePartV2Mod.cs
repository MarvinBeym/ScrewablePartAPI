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

        public override void OnMenuLoad()
        {
            ScrewablePartV2.version = this.Version;
            try
            {
                interfaceObject = GameObject.Find("Interface");
                interfaceActive = GameObject.Find("Quad 7");
                quad7 = GameObject.Find("InterfaceActive");
            }
            catch{}

            CheckForOldFiles();

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
                    ShowYesNoMessage($"ScrewablePartAPI is outdated\n" +
                        $"version {updateCheckResponse.available} is available on GitHub\n" +
                        $"Do you want to update automatically?\n" +
                        $"(Restart will be required)\n" +
                        $"This can break mods using outdated versions", "ScrewablePartAPI is outdated", UpdateMessageYesClicked, UpdateMessageNoClicked);
                    break;
            }

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
            string oldDllFile = Assembly.GetExecutingAssembly().Location;
            string oldXmlFile = Assembly.GetExecutingAssembly().Location;
            oldXmlFile = oldXmlFile.Replace(".dll", ".xml");
            string oldAssetsFile = Path.Combine(ModLoader.GetModAssetsFolder(this), assetsFile);

            oldDllFile = oldDllFile.Replace($"{ID}.dll", $"{ID}.old_dll");
            oldAssetsFile = oldAssetsFile.Replace(assetsFile, "screwableapi.old_unity3d");
            oldXmlFile = oldXmlFile.Replace($"{ID}.xml", $"{ID}.old_xml");

            if (File.Exists(oldDllFile)) { File.Delete(oldDllFile); }
            if (File.Exists(oldAssetsFile)) { File.Delete(oldAssetsFile); }
            if (File.Exists(oldXmlFile)) { File.Delete(oldXmlFile); }
        }

        private void UpdateMessageYesClicked()
        {
            string oldDllFile = Assembly.GetExecutingAssembly().Location;
            string oldXmlFile = Assembly.GetExecutingAssembly().Location;
            oldXmlFile = oldXmlFile.Replace(".dll", ".xml");
            string oldAssetsFile = Path.Combine(ModLoader.GetModAssetsFolder(this), assetsFile);

            string newVersion = updateCheckResponse.available.Replace(".", "_");
            string updateFileUrl = $"http://localhost/web/msc/screwablepartapi/public/versions/{newVersion}.zip";

            string downloadFilePath = Path.Combine(Path.GetDirectoryName(oldDllFile), $"{ID}_{newVersion}_update.zip");

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
                    client.DownloadFile(updateFileUrl, downloadFilePath);
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
