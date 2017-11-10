using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
    public class DeckLinkManager : Singleton<DeckLinkManager>
    {
        protected DeckLinkManager() { }

        public bool _logDeviceEnumeration;
        private Shader _shader_YCbCr_8bpp_422;
        private Shader _shader_YCbCr_10bpp_422;
        private Shader _shader_ARGB_8bpp_444;
        private Shader _shader_BGRA_8bpp_444;
        private Shader _shader_RGB_10bpp_444;

        private Shader _shaderDeinterlace;

        private static ChromaLerp _lerpType;
        public DeinterlaceMethod _deinterlaceMethod = DeinterlaceMethod.Blend;
        
#if UNITY_5 && !UNITY_5_0 && !UNITY_5_1 || UNITY_5_4_OR_NEWER
        private System.IntPtr _renderEventFunctor;
#endif

        public enum ChromaLerp
        {
            Off,
            Lerp,
            Smart,
        }

        public enum DeinterlaceMethod
        {
            None,
            Discard,
            DiscardSmooth,
            Blend,
        }

        private List<Device> _devices;
        private bool _isInitialised;
        private bool _isOpenGL;
        //private long _frameTime;
        

        //-------------------------------------------------------------------------

		public ChromaLerp LerpType
		{
			get { return _lerpType; }
		}

        public bool IsOpenGL
        {
            get { return _isOpenGL; }
        }

        public int NumDevices
        {
            get { if (_devices != null) return _devices.Count; return 0; }
        }

        new void Awake()
        {
            base.Awake();
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (Init())
            {
                Debug.Log("[AVProDeckLink] Initialised (plugin v" + DeckLinkPlugin.GetNativePluginVersion() + " script v" + Helper.Version + ")");

                uint apiVersionCode = DeckLinkPlugin.GetDeckLinkAPIVersion();
                string apiVersionString = "" + ((apiVersionCode >> 24) & 255) + "." + ((apiVersionCode >> 16) & 255) + "." + ((apiVersionCode >> 8) & 255) + "." + ((apiVersionCode >> 0) & 255);
                Debug.Log("[AVProDeckLink] Using DeckLink API version " + apiVersionString);
            }
            else
            {
                Debug.LogError("[AVProDeckLink] failed to initialise.");
                this.enabled = false;
                _isInitialised = false;
            }
#endif
        }

        protected bool Init()
        {
            _shaderDeinterlace = Shader.Find("AVProDeckLink/Deinterlace");
            _shader_ARGB_8bpp_444 = Shader.Find("AVProDeckLink/CompositeARGB");
            _shader_BGRA_8bpp_444 = Shader.Find("AVProDeckLink/CompositeBGRA");
            _shader_RGB_10bpp_444 = Shader.Find("AVProDeckLink/CompositeRGB10bpp");
            _shader_YCbCr_10bpp_422 = Shader.Find("AVProDeckLink/CompositeV210");
            _shader_YCbCr_8bpp_422 = Shader.Find("AVProDeckLink/CompositeUYVY");
#if UNITY_5 && !UNITY_5_0 && !UNITY_5_1 || UNITY_5_4_OR_NEWER
            _renderEventFunctor = DeckLinkPlugin.GetRenderEventFunc();
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            try
            {

                bool unitySupportsExternalTextures = false;
#if AVPRODECKLINK_UNITYFEATURE_EXTERNALTEXTURES
			unitySupportsExternalTextures = true;
#endif
                DeckLinkPlugin.SetUnityFeatures(unitySupportsExternalTextures);
            }
            catch (System.DllNotFoundException e)
            {
                Debug.LogError("[AVProDeckLink] Unity couldn't find the DLL, did you move the 'Plugins' folder to the root of your project?");
                throw e;
            }

            _isOpenGL = SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL");

            bool swapRedBlue = false;
            if (SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D 11"))
                swapRedBlue = true;

            if (swapRedBlue)
            {
                Shader.DisableKeyword("SWAP_RED_BLUE_OFF");
                Shader.EnableKeyword("SWAP_RED_BLUE_ON");
            }
            else
            {
                Shader.DisableKeyword("SWAP_RED_BLUE_ON");
                Shader.EnableKeyword("SWAP_RED_BLUE_OFF");
            }

            SetChromaInterpolation(ChromaLerp.Lerp);

            EnumDevices();

            //_frameTime = GetFrameInterval(Screen.currentResolution.refreshRate);
            //Debug.Log("[AVProDeckLink] Using frame interval " + _frameTime + " for rate of " + Screen.currentResolution.refreshRate.ToString("F3"));

            _isInitialised = true;
            StartCoroutine("FinalRenderCapture");

            return _isInitialised;
#else
            return false;
#endif
        }

        public static void SetChromaInterpolation(ChromaLerp lerp)
        {
            Shader.DisableKeyword("CHROMA_NOLERP");
            Shader.DisableKeyword("CHROMA_LERP");
            Shader.DisableKeyword("CHROMA_SMARTLERP");
            switch (lerp)
            {
                case ChromaLerp.Off:
                    Shader.EnableKeyword("CHROMA_NOLERP");
                    break;
                case ChromaLerp.Lerp:
                    Shader.EnableKeyword("CHROMA_LERP");
                    break;
                case ChromaLerp.Smart:
                    Shader.EnableKeyword("CHROMA_SMARTLERP");
                    break;
            }

            _lerpType = lerp;
        }


#if UNITY_EDITOR
        [ContextMenu("Set Chroma Lerp: Off")]
        private void SetChromaLerpOff()
        {
            SetChromaInterpolation(ChromaLerp.Off);
        }

        [ContextMenu("Set Chroma Lerp: Lerp")]
        private void SetChromaLerpOn()
        {
            SetChromaInterpolation(ChromaLerp.Lerp);
        }

        [ContextMenu("Set Chroma Lerp: Smart")]
        private void SetChromaLerpSmart()
        {
            SetChromaInterpolation(ChromaLerp.Smart);
        }
#endif


        private static long GetFrameInterval(float fps)
        {
            long frameTime = 0;
            switch (fps.ToString("F3"))
            {
                case "60.000":
                    frameTime = 166667;
                    break;
                case "59.000":
                case "59.940":
                    frameTime = 166833;
                    break;
                case "50.000":
                    frameTime = 200000;
                    break;
                case "30.000":
                    frameTime = 333333;
                    break;
                case "29.970":
                    frameTime = 333667;
                    break;
                case "25.000":
                    frameTime = 400000;
                    break;
                case "24.000":
                    frameTime = 416667;
                    break;
                case "23.976":
                    frameTime = 417188;
                    break;
            }
            return frameTime;
        }

        private IEnumerator FinalRenderCapture()
        {
            var wait = new WaitForEndOfFrame();
            while (Application.isPlaying)
            {
                yield return wait;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

#if UNITY_5 && !UNITY_5_0 && !UNITY_5_1 || UNITY_5_4_OR_NEWER
                GL.IssuePluginEvent(_renderEventFunctor, DeckLinkPlugin.PluginID | (int)DeckLinkPlugin.PluginEvent.UpdateAllOutputs);
#else
                GL.IssuePluginEvent(DeckLinkPlugin.PluginID | (int)DeckLinkPlugin.PluginEvent.UpdateAllOutputs);
#endif

#endif
            }
        }

        void Update()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#if UNITY_5 && !UNITY_5_0 && !UNITY_5_1 || UNITY_5_4_OR_NEWER
            GL.IssuePluginEvent(_renderEventFunctor, DeckLinkPlugin.PluginID | (int)DeckLinkPlugin.PluginEvent.UpdateAllInputs);
#else
            GL.IssuePluginEvent(DeckLinkPlugin.PluginID | (int)DeckLinkPlugin.PluginEvent.UpdateAllInputs);
#endif
#endif
        }

        public bool GetPixelConversionShader(DeckLinkPlugin.PixelFormat format, ref Shader shader, ref int pass)
        {
            bool result = true;
            pass = 0;
            switch (format)
            {
                case DeckLinkPlugin.PixelFormat.YCbCr_8bpp_422:
                    shader = _shader_YCbCr_8bpp_422;
                    break;
                case DeckLinkPlugin.PixelFormat.YCbCr_10bpp_422:
                    shader = _shader_YCbCr_10bpp_422;
                    break;
                case DeckLinkPlugin.PixelFormat.ARGB_8bpp_444:
                    shader = _shader_ARGB_8bpp_444;
                    break;
                case DeckLinkPlugin.PixelFormat.BGRA_8bpp_444:
                    shader = _shader_BGRA_8bpp_444;
                    break;
                case DeckLinkPlugin.PixelFormat.RGB_10bpp_444:
                    shader = _shader_RGB_10bpp_444;
                    pass = 0;
                    break;
                case DeckLinkPlugin.PixelFormat.RGBX_10bpp_444:
                    shader = _shader_RGB_10bpp_444;
                    pass = 0;
                    break;
                case DeckLinkPlugin.PixelFormat.RGBX_10bpp_444_LE:
                    shader = _shader_RGB_10bpp_444;
                    pass = 0;
                    break;
                default:
                    Debug.LogError("[AVProDeckLink] Unsupported pixel format " + format);
                    result = false;
                    break;
            }

            return result;
        }

        public Shader GetDeinterlaceShader()
        {
            return _shaderDeinterlace;
        }

		public void Reset()
		{
			Deinit();
			DeckLinkPlugin.Deinit();
			DeckLinkPlugin.Init();
			Init();
		}

        void OnApplicationQuit()
        {
            Deinit();
        }

        private void Deinit()
        {
            if (_devices != null)
            {
                for (int i = 0; i < _devices.Count; i++)
                {
                    _devices[i].StopInput();
                    _devices[i].StopOutput();
                    _devices[i].Dispose();
                }
                _devices.Clear();
                _devices = null;
            }

            _isInitialised = false;
            //DeckLinkPlugin.Deinit();
        }

        private void EnumDevices()
        {
            _devices = new List<Device>(8);
            int numDevices = DeckLinkPlugin.GetNumDevices();

			if(numDevices == 0)
			{
				uint apiVersionCode = DeckLinkPlugin.GetDeckLinkAPIVersion();
				string apiVersionString = "" + ((apiVersionCode >> 24) & 255) + "." + ((apiVersionCode >> 16) & 255) + "." + ((apiVersionCode >> 8) & 255) + "." + ((apiVersionCode >> 0) & 255);
				Debug.LogWarning("[AVProDeckLink] Unable to find any DeckLink Devices, It is possible that your Desktop Video is out of date. Please update to version " + apiVersionString);
			}

            for (int deviceIndex = 0; deviceIndex < numDevices; deviceIndex++)
            {
                int numInputModes = DeckLinkPlugin.GetNumVideoInputModes(deviceIndex);
				int numOutputModes = DeckLinkPlugin.GetNumVideoOutputModes(deviceIndex);
                if (numInputModes > 0 || numOutputModes > 0)
                {
                    string modelName = DeckLinkPlugin.GetDeviceName(deviceIndex);
                    string displayName = DeckLinkPlugin.GetDeviceDisplayName(deviceIndex);
                    Device device = new Device(modelName, displayName, deviceIndex);
                    _devices.Add(device);

                    if (_logDeviceEnumeration)
                    {
                        Debug.Log("[AVProDeckLink] Device" + deviceIndex + ": " + displayName + "(" + modelName + ") has " + device.NumInputModes + " video input modes, " + device.NumOutputModes + " video output modes");
                        if (device.SupportsInputModeAutoDetection)
                            Debug.Log("[AVProDeckLink]\tSupports input video mode auto-detection");
                        if (device.SupportsInternalKeying)
                            Debug.Log("[AVProDeckLink]\tSupports internal keyer");
                        if (device.SupportsExternalKeying)
                            Debug.Log("[AVProDeckLink]\tSupports external keyer");

                        for (int modeIndex = 0; modeIndex < device.NumInputModes; modeIndex++)
                        {
                            DeviceMode mode = device.GetInputMode(modeIndex);
                            Debug.Log("[AVProDeckLink]\t\tInput Mode" + modeIndex + ":  " + mode.ModeDescription + " " + mode.Width + "x" + mode.Height + " @" + mode.FrameRate + " (" + mode.PixelFormatDescription + ") ");
                        }
                        for (int modeIndex = 0; modeIndex < device.NumOutputModes; modeIndex++)
                        {
                            DeviceMode mode = device.GetOutputMode(modeIndex);
                            Debug.Log("[AVProDeckLink]\t\tOutput Mode" + modeIndex + ": " + mode.ModeDescription + " " + mode.Width + "x" + mode.Height + " @" + mode.FrameRate + " (" + mode.PixelFormatDescription + ") ");
                        }
                    }
                }
            }
        }

		public Device GetDevice(int index)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Device result = null;

            if (_devices != null && index >= 0 && index < _devices.Count)
            {
                result = _devices[index];
            }

            return result;
#else
            return null;
#endif
        }

        public Device GetDevice(string name)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Device result = null;
            int numDevices = NumDevices;
            for (int i = 0; i < numDevices; i++)
            {
                Device device = GetDevice(i);
                if (device.Name == name)
                {
                    result = device;
                    break;
                }
            }
            return result;
#else
            return null;
#endif
        }
    }
}
