using BepInEx;
using Cinemachine;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace GorillaScreenShotCamera
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private float lastScreenshotTime = 0f;
        private const float debounceTime = 1f;
        public bool moveThirdPersonCamera = false;

        public AssetBundle LoadAssetBundle(string path)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            AssetBundle bundle = AssetBundle.LoadFromStream(stream);
            stream.Close();
            Debug.Log("sigma camera loading");
            return bundle;
        }

        void Start()
        {
            GorillaTagger.OnPlayerSpawned(new Action(Init));
        }

        GameObject asset;
        void Init()
        {

            Debug.Log("sigma camera loading");
            var bundle = LoadAssetBundle("GorillaScreenShotCamera.camera");
            asset = Instantiate(bundle.LoadAsset<GameObject>("Camera"));
            asset.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            asset.transform.position = new Vector3(-68.4015f, 12.406f, -83.699f);

            GameObject.Find("CameraGrabCollider").AddComponent<DevHoldable>();
            GameObject.Find("CameraGrabCollider").layer = 18;
        }

        public void Update()
        {
            if (moveThirdPersonCamera)
            {
                GorillaTagger.Instance.thirdPersonCamera.GetComponentInChildren<CinemachineVirtualCamera>().Follow = asset.transform;
                GorillaTagger.Instance.thirdPersonCamera.GetComponentInChildren<CinemachineVirtualCamera>().LookAt = asset.transform;

            }
            if (GameObject.Find("CameraGrabCollider").GetComponent<DevHoldable>().InHand)
            {
                bool isTriggerPressed = false;

                if (GameObject.Find("CameraGrabCollider").GetComponent<DevHoldable>().InLeftHand)
                {
                    isTriggerPressed = ControllerInputPoller.instance.leftControllerIndexFloat >= 0.3f;
                }
                else
                {
                    isTriggerPressed = ControllerInputPoller.instance.rightControllerIndexFloat >= 0.3f;
                }

                if (isTriggerPressed && Time.time - lastScreenshotTime >= debounceTime)
                {
                    lastScreenshotTime = Time.time;
                    TakeScreenshot();
                }
            }
        }

        private void TakeScreenshot()
        {
            Camera actualCamera = GameObject.Find("ActualCamera").GetComponent<Camera>();
            if (actualCamera == null || actualCamera.targetTexture == null)
            {
                Debug.LogError("ActualCamera or its RenderTexture is not set!");
                return;
            }

            string directoryPath = Path.Combine(Paths.PluginPath, "Camera ScreenShots");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string screenshotPath = Path.Combine(directoryPath + DateTime.Now.ToString("MM-dd"), $"Screenshot_{DateTime.Now:ss}.png");

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = actualCamera.targetTexture;

            Texture2D screenshot = new Texture2D(actualCamera.targetTexture.width, actualCamera.targetTexture.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, actualCamera.targetTexture.width, actualCamera.targetTexture.height), 0, 0);
            screenshot.Apply();

            Color[] pixels = screenshot.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = pixels[i].gamma;      
            }
            screenshot.SetPixels(pixels);
            screenshot.Apply();

            RenderTexture.active = currentRT;

            byte[] bytes = screenshot.EncodeToPNG();
            File.WriteAllBytes(screenshotPath, bytes);
            Debug.Log($"Screenshot saved at: {screenshotPath}");

            Destroy(screenshot);
        }

    }
}
