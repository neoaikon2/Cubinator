// UNCOMMENT ONE OR THE OTHER BUT NOT BOTH!
//#define BONEWORKS
#define BONELAB

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using MelonLoader;
using BW_Cubinator;
using UnityEngine;

[assembly: MelonInfo(typeof(Cubinator), "Boneworks Cubinator", "1.0.0", "Crypto_Neo")]
#if BONEWORKS
[assembly: MelonGame("Stress Level Zero", "BONEWORKS")]
#endif
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
namespace BW_Cubinator
{    
    public class Cubinator : MelonMod
    {        
        public int cubeMapSize = 1024*4; // Resolution of exported cubemap
		public string outputDir = MelonUtils.BaseDirectory + "\\Output"; 

        private bool Capturing = false;
		private MelonPreferences_Category cubemapSettings;
		private MelonPreferences_Entry<int> cfgResolution;
		private MelonPreferences_Entry<string> cfgOutputDir;

		// Initialize preferences if they aren't already
		public override void OnInitializeMelon()
		{
			cubemapSettings = MelonPreferences.CreateCategory("Cubemap Settings");
			cfgResolution = MelonPreferences.CreateEntry<int>("Cubemap Settings", "Resolution", 2048);
			cfgOutputDir = MelonPreferences.CreateEntry<string>("Cubemap Settings", "Output Directory", MelonUtils.BaseDirectory + "/Output");
			base.OnInitializeMelon();
		}

		// Set cubeMapSize and outputDir based on cfg preferences
		public override void OnPreferencesLoaded()
		{
			cubeMapSize = cfgResolution.Value;
			outputDir = cfgOutputDir.Value;
			base.OnPreferencesLoaded();
		}


		public override void OnApplicationStart()
		{
#if BONEWORKS
			LoggerInstance.Msg(Info.Name + " " + Info.Version + " built for Boneworks");
#endif
#if BONELAB
			LoggerInstance.Msg(Info.Name + " " + Info.Version + " built for Bonelab");
#endif
#if DEBUG
			LoggerInstance.Msg("Cubemap Resolution: " + cubeMapSize.ToString());
			LoggerInstance.Msg("Output Directory: " + outputDir);
#endif
			base.OnApplicationStart();
		}


		public override void OnUpdate()
        {			
#if BONEWORKS && BONELAB
			if(!capturing) {
				LoggerInstance.Error("Both pragmas enabled! Please enable either BONEWORKS or BONELAB and rebuild!");
				capturing = true;
			}
			return;
#endif
			// Capture the cube map when the user presses CTRL+P
			if ((Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.P))
            {
                // Begin capture coroutine
                MelonCoroutines.Start(CaptureCubemap());
            }            
            base.OnUpdate();
        }

        IEnumerator CaptureCubemap()
        {
            if (Capturing) yield return null;
            LoggerInstance.Msg("Capturing local environment, please wait...");
            Capturing = true;
            
			// Check if output directory exists and create it if neccessary
            if (!System.IO.Directory.Exists(outputDir))
                System.IO.Directory.CreateDirectory(outputDir);

			// Hide the player model
#if BONEWORKS
			GameObject art = GameObject.Find("[RigManager (Default Brett)]");            
#endif
#if BONELAB
			GameObject art = GameObject.Find("[RigManager (Blank)]");
#endif
			// Create a new game object and add a camera to it
			GameObject go = new GameObject();
            Camera cam = go.AddComponent<Camera>();

            // Copy the position and settings from the main camera
            cam.transform.position = Camera.main.transform.position;

            cam.transform.rotation.SetEulerAngles(new Vector3(0, Camera.main.transform.rotation.eulerAngles.y));
            cam.farClipPlane = cam.farClipPlane;
            cam.nearClipPlane = cam.nearClipPlane;

            // Set as inactive so it doesn't interfere with the Main Camera
            go.SetActive(false);
            art.SetActive(false);

            // Create a new cubemap
            Cubemap cm = new Cubemap(cubeMapSize, TextureFormat.RGB24, false);
            RenderTexture er = new RenderTexture(cubeMapSize * 2, cubeMapSize, 0);
            
            // Wait for the end of the current frame
            yield return new WaitForEndOfFrame();
            
            // Render to cubemap (63 is all faces)
            cam.RenderToCubemap(cm, 63);
            
            // Export all the faces
            for (int i = 0; i < 6; i++)
            {
                SaveFace(i, cm);
            }

            // Renabled Brett, sexy man thing that he is
            art.SetActive(true);
            Capturing = false;
            LoggerInstance.Msg("Environmental capturing complete, raw data saved to " + outputDir);
        }

        public void SaveFace(int face, Cubemap cum)
        {
            // Create a temporary output texture
            Texture2D output = new Texture2D(cubeMapSize, cubeMapSize, TextureFormat.RGB24, false);
            
            // Copy the requested face from the cubemap to the output texture
            switch (face)
            {
                case 0:
                    output.SetPixels(cum.GetPixels(CubemapFace.PositiveZ, 0));
                    break;
                case 1:
                    output.SetPixels(cum.GetPixels(CubemapFace.NegativeZ, 0));
                    break;
                case 2:
                    output.SetPixels(cum.GetPixels(CubemapFace.PositiveX, 0));
                    break;
                case 3:
                    output.SetPixels(cum.GetPixels(CubemapFace.NegativeX, 0));
                    break;
                case 4:
                    output.SetPixels(cum.GetPixels(CubemapFace.PositiveY, 0));
                    break;
                case 5:
                default:
                    output.SetPixels(cum.GetPixels(CubemapFace.NegativeY, 0));
                    break;
            }
            
            // Get the bytes of raw texture data
            byte[] pngData = output.GetRawTextureData();
                        
            // Write the file
            switch (face)
            {
                case 0:
                    File.WriteAllBytes(outputDir + "/Front.raw", pngData);                    
                    break;
                case 1:
                    File.WriteAllBytes(outputDir + "/Back.raw", pngData);
                    break;				
                case 2:
                    File.WriteAllBytes(outputDir + "/Right.raw", pngData);
                    break;
                case 3:
                    File.WriteAllBytes(outputDir + "/Left.raw", pngData);
                    break;
                case 4:
                    File.WriteAllBytes(outputDir + "/Top.raw", pngData);
                    break;
                case 5:
                default:
                    File.WriteAllBytes(outputDir + "/Bottom.raw", pngData);
                    break;
            }
        }
    }
}