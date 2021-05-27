using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using BepInEx;
using Bounce.Unmanaged;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace LordAshes
{
    [BepInPlugin(Guid, "Mirror Mini Plug-In", Version)]
    [BepInDependency(LordAshes.CustomMiniPlugin.Guid)]
    public class MirrorMiniPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Guid = "org.lordashes.plugins.mirrormini";
        public const string Version = "1.1.0.0";

        // Configuration
        private ConfigEntry<KeyboardShortcut> triggerKeyBasic { get; set; }
        private ConfigEntry<KeyboardShortcut> triggerKeyAdvanced { get; set; }

        // Content directory
        private string dir = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("/")) + "/TaleSpire_CustomData/";

        // Speech font name
        private bool properInitialization = false;

        // Mirror style
        private string style = "Mirror01";

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            UnityEngine.Debug.Log("Lord Ashes Handouts Plugin Active.");

            if (!System.IO.Directory.Exists(dir + "Minis/" + style))
            {
                UnityEngine.Debug.LogWarning("Lord Ashes Mirror Plugin: Custom folder '" + dir + "Minis' must contain the '"+style+"' folder and contents.");
                UnityEngine.Debug.LogWarning("Lord Ashes Mirror Plugin: Looks like you did not complete the manual portion of the install instructions.");
            }
            else
            {
                properInitialization = true;
            }

            triggerKeyBasic = Config.Bind("Hotkeys", "Open Mirror Content Dialog Shortcut", new KeyboardShortcut(KeyCode.M, KeyCode.LeftControl));
            triggerKeyAdvanced = Config.Bind("Hotkeys", "Open Mirror Style Dialog Shortcut", new KeyboardShortcut(KeyCode.N, KeyCode.LeftControl));
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
            if(isBoardLoaded())
            {
                if (properInitialization && triggerKeyBasic.Value.IsUp())
                {
                    SystemMessage.AskForTextInput("Mirror, Mirror on the wall...", "Make me look like...", "OK", doMagic, null, "Cancel", null, "");
                }
                else if (properInitialization && triggerKeyAdvanced.Value.IsUp())
                {
                    SystemMessage.AskForTextInput("Mirror, Mirror on the wall...", "Make my stytle: (Mirror##)", "OK", setStyle, null, "Cancel", null, "Mirror");                    
                }
            }
        }

        private void setStyle(string style)
        {
            this.style = style;
            SystemMessage.DisplayInfoText("Mirror Style Set To " + this.style);
        }

        private void doMagic(string transformation)
        {
            if((NGuid)LocalClient.SelectedCreatureId.Value!=null)
            {
                string[] matches = System.IO.Directory.EnumerateFiles(dir + "Images/", transformation + "*.*").ToArray();
                if (matches.Count()>0)
                {
                    // Replace mirror texture with the desired texture
                    System.Drawing.Image image = new System.Drawing.Bitmap(matches[0]);
                    if(System.IO.File.Exists(dir + "Minis/" + style + "/" + style + ".BMP")) { System.IO.File.Delete(dir + "Minis/" + style + "/" + style + ".BMP"); }
                    UnityEngine.Debug.Log("Saving '"+ matches[0] + "' as '"+ dir + "Minis/" + style + "/" + style + ".BMP" + "'");
                    image.Save(dir + "Minis/" + style + "/" + style + ".BMP", System.Drawing.Imaging.ImageFormat.Bmp);
                    // Call magic transformation
                    ChatManager.SendChatMessage("<size=0> ! " + LocalClient.SelectedCreatureId.ToString() + " </size> Make me a "+style, (NGuid)LocalClient.SelectedCreatureId.Value);
                }
                else
                {
                    // Make an empty plane version of the asset
                    if (!System.IO.Directory.Exists(dir + "Minis/" + style + "(Empty)/")) { System.IO.Directory.CreateDirectory(dir + "Minis/" + style + "(Empty)/"); }
                    UnityEngine.Debug.Log("Creating '" + dir + "Minis/" + style + "(Empty)/" + style + "(Empty).OBJ'");
                    string[] obj = System.IO.File.ReadAllLines(dir + "Minis/" + style + "/" + style + ".OBJ");
                    for(int l=0; l<obj.Count(); l++)
                    {
                        if(obj[l].ToUpper().StartsWith("MTLLIB ")) { obj[l]="mtllib "+style+"(Empty).mtl"; }
                    }
                    System.IO.File.WriteAllLines( dir + "Minis/" + style + "(Empty)/" + style + "(Empty).OBJ", obj);
                    UnityEngine.Debug.Log("Creating '" + dir + "Minis/" + style + "(Empty)/" + style + ".MTL'");
                    string[] mtl = System.IO.File.ReadAllLines(dir + "Minis/" + style + "/" + style + ".MTL");
                    // Find the material section which contains the plane texture
                    string targetSection = "";
                    foreach(string line in mtl)
                    {
                        if(line.ToUpper().StartsWith("NEWMTL ")) { targetSection = line.Substring(line.IndexOf(" ") + 1).Trim(); }
                        if (line.ToUpper().Contains(style.ToUpper() + ".BMP")) { break; }
                    }
                    // Run through the material file and replace any existing alpha (d) references to alpha 0
                    string section = "";
                    for(int l=0; l<mtl.Count(); l++)
                    {
                        if (mtl[l].ToUpper().StartsWith("NEWMTL ")) { section = mtl[l].Substring(mtl[l].IndexOf(" ") + 1).Trim(); }
                        if (section==targetSection)
                        {
                            if (mtl[l].ToUpper().StartsWith("D ")){ mtl[l] = "d 0.0"; }
                            if (mtl[l].ToUpper().StartsWith("TR ")) { mtl[l] = "Tr 0.0"; }
                        }
                        else
                        {
                            if (mtl[l].ToUpper().StartsWith("MAP"))
                            {
                                string fileName = mtl[l].Substring(mtl[l].IndexOf(" ") + 1);
                                if(System.IO.File.Exists(dir+"Minis/"+style+"(Empty)/"+fileName)) { System.IO.File.Delete(dir + "Minis/" + style + "(Empty)/" + fileName); }
                                System.IO.File.Copy(dir + "Minis/" + style + "/" + fileName, dir + "Minis/" + style + "(Empty)/" + fileName);
                            }
                        }
                    }
                    System.IO.File.WriteAllLines(dir + "Minis/" + style + "(Empty)/" + style + "(Empty).MTL", mtl);
                    // Call magic transformation
                    ChatManager.SendChatMessage("<size=0> ! " + LocalClient.SelectedCreatureId.ToString() + " </size> Make me a " + style+"(Empty)", (NGuid)LocalClient.SelectedCreatureId.Value);
                }
            }
        }

        /// <summary>
        /// Function to check if the board is loaded
        /// </summary>
        /// <returns></returns>
        public bool isBoardLoaded()
        {
            return CameraController.HasInstance && BoardSessionManager.HasInstance && !BoardSessionManager.IsLoading;
        }
    }
}
