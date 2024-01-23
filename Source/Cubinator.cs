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
		private bool Capturing = false;
		private MelonPreferences_Category cubemapSettings;
		private MelonPreferences_Entry<int> cfgResolution;
		private MelonPreferences_Entry<string> cfgOutputDir;

		// Initialize preferences if they aren't already
		public override void OnInitializeMelon()
		{
			// Check if settings file actually exists and if not create it
			if (!Directory.Exists(MelonUtils.UserDataDirectory + "\\Cubinator"))
				Directory.CreateDirectory(MelonUtils.UserDataDirectory + "\\Cubinator");
			if(!File.Exists(MelonUtils.UserDataDirectory + "\\Cubinator\\Cubinator.cfg"))
				File.Create(MelonUtils.UserDataDirectory + "\\Cubinator\\Cubinator.cfg");

			// Select settings file for loading and saving
			cubemapSettings = MelonPreferences.CreateCategory("Cubinator");
			cubemapSettings.SetFilePath(MelonUtils.UserDataDirectory + "\\Cubinator\\Cubinator.cfg");
			cfgResolution = MelonPreferences.CreateEntry<int>("Cubinator", "Resolution", 2048);
			cfgOutputDir = MelonPreferences.CreateEntry<string>("Cubinator", "OutputDirectory", MelonUtils.BaseDirectory + "\\Output");

			// Post build message
#if BONEWORKS
			LoggerInstance.Msg(Info.Name + " " + Info.Version + " built for Boneworks");
#endif
#if BONELAB
			LoggerInstance.Msg(Info.Name + " " + Info.Version + " built for Bonelab");
#endif

			// DEBUG, post resolution
#if DEBUG
			LoggerInstance.Msg("Cubemap Resolution: " + cfgResolution.Value.ToString());
			LoggerInstance.Msg("Output Directory: " + cfgOutputDir.Value.ToString());
#endif
			base.OnInitializeMelon();
		}

		public override void OnDeinitializeMelon()
		{
			// Save settings
			cubemapSettings.SaveToFile();
			base.OnDeinitializeMelon();
		}

		public override void OnUpdate()
		{
// Keeps people who might accidently set both these flags from making that mistake
#if BONEWORKS && BONELAB
			if(!capturing) {
				LoggerInstance.Error("Both pragmas enabled! Please enable either BONEWORKS or BONELAB and rebuild!");
				capturing = true;
			}
			return;
#endif

			// Debug heartbeat
#if DEBUG
			Debug_Heartbeat();
#endif
			// Capture the cube map when the user presses CTRL+P
			if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.P))
			{
#if DEBUG
				MelonLogger.Msg(ConsoleColor.Cyan, "Keypress detected");
#endif
				// Begin capture coroutine
				MelonCoroutines.Start(CaptureCubemap());
			}
			base.OnUpdate();
		}

#if DEBUG
		// Heartbeat in the console to show our mod is alive
		float heartbeat_t = 0;
		private void Debug_Heartbeat()
		{
			heartbeat_t += Time.deltaTime;
			if(heartbeat_t >= 3)
			{
				MelonLogger.Msg(ConsoleColor.Cyan, "Hearbeat <3!");
				heartbeat_t = 0;
			}
		}
#endif

		IEnumerator CaptureCubemap()
        {
			// Begin capture
            if (Capturing) yield return null;
            LoggerInstance.Msg("Capturing local environment, please wait...");
            Capturing = true;
            
			// Check if output directory exists and create it if neccessary
            if (!System.IO.Directory.Exists(cfgOutputDir.Value))
                System.IO.Directory.CreateDirectory(cfgOutputDir.Value);

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
            Cubemap cm = new Cubemap(cfgResolution.Value, TextureFormat.RGB24, false);
            RenderTexture er = new RenderTexture(cfgResolution.Value * 2, cfgResolution.Value, 0);
            
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
            LoggerInstance.Msg("Environmental capturing complete, PNG output saved to " + cfgOutputDir.Value);
        }

        public void SaveFace(int face, Cubemap cum)
        {
            // Create a temporary output texture
            Texture2D output = new Texture2D(cfgResolution.Value, cfgResolution.Value, TextureFormat.RGB24, false);

			// Select the current face
			CubemapFace cubeMapFace = CubemapFace.NegativeY;
            switch (face)
            {
                case 0:
					cubeMapFace = CubemapFace.PositiveZ;
                    break;
                case 1:
					cubeMapFace = CubemapFace.NegativeZ;
                    break;
                case 2:
					cubeMapFace = CubemapFace.PositiveX;
                    break;
                case 3:
					cubeMapFace = CubemapFace.NegativeX;
                    break;
                case 4:
					cubeMapFace = CubemapFace.PositiveY;
                    break;
                case 5:
                default:
					cubeMapFace = CubemapFace.NegativeY;
					break;
            }

			// Select the current file name
			string fileName = "/Bottom.png";
            switch (face)
            {
                case 0:
					fileName = "/Front.png";
                    break;
                case 1:
					fileName = "/Back.png";
					break;				
                case 2:
					fileName = "/Right.png";
					break;
                case 3:
					fileName = "/Left.png";
					break;
                case 4:
					fileName = "/Top.png";
					break;
                case 5:
                default:
					fileName = "/Bottom.png";
					break;
            }

			// Flip the image so it's oriented correctly and save it to the output directory
			output.SetPixels(FlipFlip(cum.GetPixels(cubeMapFace, 0)));
			File.WriteAllBytes(cfgOutputDir.Value + fileName, ImageConversion.EncodeToPNG(output)); // So that's where Texture2D.EncodeToPNG went...
		}

		public UnhollowerBaseLib.Il2CppStructArray<Color> FlipFlip(UnhollowerBaseLib.Il2CppStructArray<Color> pixels)
		{
			// Check if the array is not null
			if (pixels != null)
			{
				// TODO - Check if we actually need to copy the array at this point or if we can just leave it initialized at length
				UnhollowerBaseLib.Il2CppStructArray<Color>  tmp = new Color[pixels.Length];
				pixels.CopyTo(tmp, 0);

				// Flip the pixel data so the image won't be upside down
				for(int j = 0; j < cfgResolution.Value; j++)
					for(int k = 0; k < cfgResolution.Value; k++)
					{						
						int index = ((j * cfgResolution.Value) + k);
						int flipdex = ( (pixels.Length - cfgResolution.Value - j * cfgResolution.Value) + k );

						tmp[index] = pixels[flipdex];
					}
				return tmp;
			}
			return pixels;
		}
    }
}