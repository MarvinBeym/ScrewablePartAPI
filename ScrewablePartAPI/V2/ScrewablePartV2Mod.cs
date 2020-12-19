using Ionic.Zip;
using MSCLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private class LastXReleasesRespons
        {
            public string message { get; set; } = "";
            public List<string> data { get; set; }
        }

        public override string ID => "ScrewablePartAPI";
        public override string Name => "ScrewablePartAPI";
        public override string Version => "2.1.0";

        public override string Author => "DonnerPlays";
        public override bool UseAssetsFolder => true;
        public override bool LoadInMenu => true;

        private static Settings showBoltSizeSetting = new Settings("showBoltSizeSetting", "Show screw size", false);
        private Settings ignoreUpdatesSetting = new Settings("ignoreUpdatesSetting", "Ignore api updates", false);
        public static bool showScrewSize { get { return (bool)showBoltSizeSetting.Value; } }

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
        private bool serverReachable;
        private GameObject interfaceObject;
        private GameObject interfaceActive;
        private GameObject quad7;
        private string modsFolderFilePath;

        //Current & new file paths
        private string dllFilePath;
        private string xmlFilePath;
        private string assetsBundleFilePath;

        //Old file paths
        private string old_dllFilePath;
        private string old_xmlFilePath;
        private string old_assetsBundleFilePath;

        private const string host = "localhost";
        private string GetLatestReleaseVersionUrl(string currentVersion)
        {
            return $"http://{host}/web/msc/screwablepartapi/public/checkUpdateAvailable.php?currentVersion={currentVersion}";
        }
        private string GetUpdateDownloadUrl(string version)
        {
            return $"http://{host}/web/msc/screwablepartapi/public/versions/{version}.zip";
        }
        private string GetLastXReleasesUrl(int nReleases)
        {
            return $"http://{host}/web/msc/screwablepartapi/public/getLatestVersions.php?lastVersions={nReleases}";
        }
        private string GetVersionDownloadPath(string version)
        {
            return Path.Combine(modsFolderFilePath, $"{ID}_{version}_update.zip");
        }

        public override void OnMenuLoad()
        {
            ScrewablePartV2.version = this.Version;
            string availableVersion = this.Version;
            modsFolderFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            dllFilePath = Path.Combine(modsFolderFilePath, $"{ID}.dll");
            xmlFilePath = Path.Combine(modsFolderFilePath, $"{ID}.xml");
            assetsBundleFilePath = Path.Combine(ModLoader.GetModAssetsFolder(this), assetsFile);

            old_dllFilePath = dllFilePath.Replace(".dll", ".old_dll");
            old_xmlFilePath = xmlFilePath.Replace(".xml", ".old_xml");
            old_assetsBundleFilePath = assetsBundleFilePath.Replace(".unity3d", ".old_unity3d");

            LoadSettingsSave();

            CheckForOldFiles();
            try
            {
                interfaceObject = GameObject.Find("Interface");
                interfaceActive = GameObject.Find("Quad 7");
                quad7 = GameObject.Find("InterfaceActive");
            }
            catch{}

            if (!serverReachable)
            {
                ModConsole.Warning($"{ID} could not reach the update server");
                LoadAssets();
                return;
            }

            if(!(bool)ignoreUpdatesSetting.Value)
            {
                string responseJson = Helper.MakeGetRequest(GetLatestReleaseVersionUrl(Version));
                UpdateCheckResponse updateCheckResponse = JsonConvert.DeserializeObject<UpdateCheckResponse>(responseJson);
                availableVersion = updateCheckResponse.available;
                switch (updateCheckResponse.message)
                {
                    case "out-dated":
                        ModConsole.Warning($"ScrewablePartAPI outdated. version {updateCheckResponse.available} available");
                        SetMenuVisibility(false);
                        Helper.ShowCustom2ButtonMessage($"ScrewablePartAPI is outdated\n" +
                            $"version {updateCheckResponse.available} is available on GitHub\n" +
                            $"Do you want to update automatically?\n" +
                            $"(Restart will be required)\n" +
                            $"This can break mods using outdated versions", "ScrewablePartAPI is outdated", 
                            new UnityAction(delegate()
                            {
                                SetMenuVisibility(true);
                                LoadAssets();
                            }), 
                            new UnityAction(delegate() 
                            {
                                InstallVersion(updateCheckResponse.available);
                            }));
                        break;
                    default:
                        LoadAssets();
                        break;
                }
            }
            else
            {
                LoadAssets();
            }
            GameObject mscLoaderInfo = GameObject.Find("MSCLoader Info");

            try
            {
                GameObject screwablePartMenuInfoTextObject = GameObject.Instantiate(GameObject.Find("MSCLoader Info Text"));
                screwablePartMenuInfoTextObject.name = "ScrewablePartAPI Info Text";
                screwablePartMenuInfoTextObject.transform.SetParent(mscLoaderInfo.transform);
                Text screwablePartMenuInfoText = screwablePartMenuInfoTextObject.GetComponent<Text>();
                screwablePartMenuInfoText.text = 
                    $"{Name} <color=cyan>v{Version}</color> loaded! " +
                    $"({(Version == availableVersion ? "<color=lime>Up to date</color>" : "<color=red>Outdated</color>")})";
            }
            catch { }
            
        }

        private void LoadAssets()
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

        private void ExtractNewFiles(string version)
        {
            
            using (ZipFile zip = new ZipFile(GetVersionDownloadPath(version)))
            {
                int filesExtracted = 0;
                List<ZipEntry> filesToExtract = new List<ZipEntry>();
                foreach (ZipEntry zipEntry in zip.Entries)
                {
                    if (zipEntry.IsDirectory)
                    {
                        continue;
                    }
                    filesToExtract.Add(zipEntry);
                }
                

                foreach (ZipEntry zipEntry in filesToExtract)
                {
                    if (zipEntry.FileName.EndsWith(assetsFile))
                    {
                        filesExtracted += ExtractSingleZipEntry(zipEntry, assetsBundleFilePath);
                    }
                    else if (zipEntry.FileName.EndsWith($"{ID}.dll"))
                    {
                        filesExtracted += ExtractSingleZipEntry(zipEntry, dllFilePath);
                    }
                    else if (zipEntry.FileName.EndsWith($"{ID}.xml"))
                    {
                        filesExtracted += ExtractSingleZipEntry(zipEntry, xmlFilePath);
                    }
                }
                
                if(filesExtracted == filesToExtract.Count)
                {
                    Helper.ShowCustom2ButtonMessage("The updater has replaced the required files\n" +
                        "For the installation to finish, the game has to be restarted/closed",
                        "Update downloaded", Helper.ExitGame, Helper.ExitGame, "Close game", "Close game");
                }
            };

        }

        private void LoadSettingsSave()
        {
            string modSettingsFile = Helper.CombinePaths(modsFolderFilePath, "Config", "Mod Settings", ID, "settings.json");
            if (File.Exists(modSettingsFile))
            { 
                string jsonContent = File.ReadAllText(modSettingsFile);
                SettingsList list = JsonConvert.DeserializeObject<SettingsList>(jsonContent);
                showBoltSizeSetting.Value = list.settings[0].Value;
                ignoreUpdatesSetting.Value = list.settings[1].Value;
            }
        }

        public override void ModSettings()
        {
            serverReachable = Helper.ServerReachable(host);
            Settings.AddCheckBox(this, showBoltSizeSetting);
            Settings.AddCheckBox(this, ignoreUpdatesSetting);
            Settings.AddHeader(this, $"Change version - Current: {Version}");

            if (!serverReachable)
            {
                Settings.AddText(this, "Server couldn't be reached");
                return;
            }
            string responseJson = Helper.MakeGetRequest(GetLastXReleasesUrl(5));
            List<string> lastXVersions = JsonConvert.DeserializeObject<LastXReleasesRespons>(responseJson).data;
            List<string> filteredXVersions = new List<string>();

            for (int i = 0; i < lastXVersions.Count; i++)
            {
                string version = lastXVersions[i];
                Version versionCheckObj = new Version(version);
                if (versionCheckObj.CompareTo(new Version("2.1.0")) <= 0)
                {
                    continue;
                }
                filteredXVersions.Add(version);
            }

            if (filteredXVersions.Count == 0)
            {
                Settings.AddText(this, "No newer versions available or latest already installed");
            }
            else
            {
                Settings.AddText(this,
                "This will also disable the auto updater!\n" +
                "The checkbox will only update when closing the settings window!");

                for (int i = 0; i < filteredXVersions.Count; i++)
                {
                    string version = filteredXVersions[i];
                    Version versionCheckObj = new Version(version);
                    Action buttonAction = new Action(delegate ()
                    {
                        InstallVersion(version, true);
                    });

                    Settings buttonSetting = new Settings($"changeVersion{version}", "Install version", buttonAction);

                    if (i == 0)
                    {
                        Settings.AddButton(this, buttonSetting, new Color32(43, 191, 38, 255), new Color32(75, 255, 69, 255), new Color32(0, 255, 0, 255), $"{version} (latest)");
                    }
                    else
                    {
                        Settings.AddButton(this, buttonSetting, version);
                    }
                }
            }
        }

        private void InstallVersion(string version, bool disableAutoUpdater = false)
        {
            if(ModLoader.GetCurrentScene() != CurrentScene.MainMenu)
            {
                ModUI.ShowMessage("Updating/Changing version is only supported in main menu.", "Update installation stopped");
                return;
            }
            if (disableAutoUpdater)
            {
                ignoreUpdatesSetting.Value = true;
            }

            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(GetUpdateDownloadUrl(version), GetVersionDownloadPath(version));
                }
            }
            catch
            {
                ModConsole.Error($"Error occurred while trying to download the update file for v{version}");
            }

            if (File.Exists(GetVersionDownloadPath(version)))
            {
                RenameFileToOld(dllFilePath, old_dllFilePath);
                RenameFileToOld(xmlFilePath, old_xmlFilePath);
                RenameFileToOld(assetsBundleFilePath, old_assetsBundleFilePath);
                ExtractNewFiles(version);
            }
            else
            {
                ModConsole.Error("Update file was downloaded but not found in the expected folder");
            }
            File.Delete(GetVersionDownloadPath(version));
        }

        private void RenameFileToOld(string currentPath, string newPath)
        {
            try
            {
                if (File.Exists(newPath))
                {
                    try
                    {
                        File.Delete(newPath);
                    }
                    catch
                    {
                        ModConsole.Error("New file already exists and deletion before renaming failed");
                    }
                }
                if (File.Exists(currentPath))
                {
                    try
                    {
                        File.Move(currentPath, newPath);
                    }
                    catch
                    {
                        ModConsole.Error("Unable to rename current file to new name");
                    }
                }
            }
            catch
            {
                ModConsole.Error("Unable to rename current file to old name");
            }
        }


        private void RenameFile(string currentFile, string newFile)
        {
            if(File.Exists(currentFile) && !File.Exists(newFile))
            {
                File.Move(currentFile, newFile);
            }
        }

        private int ExtractSingleZipEntry(ZipEntry zipEntry, string fullPathToFile)
        {
            try
            {
                zipEntry.FileName = Path.GetFileName(fullPathToFile);
                zipEntry.Extract(Path.GetDirectoryName(fullPathToFile), ExtractExistingFileAction.OverwriteSilently);
                if (File.Exists(fullPathToFile))
                {
                    return 1;
                }
            }
            catch
            {
                return 0;
            }
            return 0;

        }
    }
}
