using UnityEngine;
using System.Text;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
    public class DeckLinkPlugin
    {
        // For use by GL.IssuePluginEvent
        public const int PluginID = 0xFA50000;
        public enum PluginEvent
        {
            UpdateAllInputs = 0,
            UpdateAllOutputs = 1,
        }

        public enum PixelFormat
        {
            YCbCr_8bpp_422 = 0,
            YCbCr_10bpp_422,

            // Beta
            ARGB_8bpp_444,
            BGRA_8bpp_444,
            RGB_10bpp_444,
            RGBX_10bpp_444,
			RGBX_10bpp_444_LE,

            Unknown,
        }

        public static PixelFormat GetPixelFormat(string name)
        {
            PixelFormat result = PixelFormat.Unknown;
            switch (name)
            {
                case "8-bit 4:2:2 YUV":
                    result = PixelFormat.YCbCr_8bpp_422;
                    break;
                case "10-bit 4:2:2 YUV":
                    result = PixelFormat.YCbCr_10bpp_422;
                    break;
                case "8-bit 4:4:4:4 ARGB":
                    result = PixelFormat.ARGB_8bpp_444;
                    break;
                case "8-bit 4:4:4:4 BGRA":
                    result = PixelFormat.BGRA_8bpp_444;
                    break;
                case "10-bit 4:4:4 RGB":
                    result = PixelFormat.RGB_10bpp_444;
                    break;
                case "10-bit 4:4:4 RGBX LE":
                    result = PixelFormat.RGBX_10bpp_444;
                    break;
                case "10-bit 4:4:4 RGBX":
                    result = PixelFormat.RGBX_10bpp_444_LE;
                    break;
                default:
                    break;
            }
            return result;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        // Global Init/Deinit
        //////////////////////////////////////////////////////////////////////////////////////////////

        [DllImport("AVProDeckLink")]
        private static extern System.IntPtr GetPluginVersion();

		public static string GetNativePluginVersion()
		{
			return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(GetPluginVersion());
		}

		[DllImport("AVProDeckLink")]
        public static extern uint GetDeckLinkAPIVersion();

        [DllImport("AVProDeckLink")]
        public static extern void SetUnityFeatures(bool supportsExternalTextures);

        [DllImport("AVProDeckLink")]
        public static extern bool Init();

        [DllImport("AVProDeckLink")]
        public static extern void Deinit();

		//////////////////////////////////////////////////////////////////////////////////////////////
		// Devices
		//////////////////////////////////////////////////////////////////////////////////////////////

		[DllImport("AVProDeckLink")]
        public static extern int GetNumDevices();

        public static string GetDeviceName(int deviceIndex)
        {
            string result = "Invalid";
            StringBuilder nameBuffer = new StringBuilder(128);
            if (GetDeviceName(deviceIndex, nameBuffer, nameBuffer.Capacity))
            {
                result = nameBuffer.ToString();
            }
            return result;
        }

        public static string GetDeviceDisplayName(int deviceIndex)
        {
            string result = "Invalid";
            StringBuilder nameBuffer = new StringBuilder(128);
            if (GetDeviceDisplayName(deviceIndex, nameBuffer, nameBuffer.Capacity))
            {
                result = nameBuffer.ToString();
            }
            return result;
        }

		[DllImport("AVProDeckLink")]
		public static extern bool FullDuplexSupported(int device);
		[DllImport("AVProDeckLink")]
		public static extern void SetDuplexMode(int device, bool isFull);
		[DllImport("AVProDeckLink")]
		public static extern bool ConfigurableDuplexMode(int device);

		//////////////////////////////////////////////////////////////////////////////////////////////
		// Video Input Modes
		//////////////////////////////////////////////////////////////////////////////////////////////

		[DllImport("AVProDeckLink")]
        public static extern int GetNumVideoInputModes(int deviceIndex);

        public static bool GetVideoInputModeInfo(int deviceIndex, int modeIndex, out int width, out int height, out float frameRate, out long frameDuration, out int fieldMode, out string modeDesc, out string formatDesc)
        {
            bool result = false;
            StringBuilder modeDescStr = new StringBuilder(32);
            StringBuilder formatDescStr = new StringBuilder(32);
            if (GetVideoInputModeInfo(deviceIndex, modeIndex, out width, out height, out frameRate, out frameDuration, out fieldMode, modeDescStr, modeDescStr.Capacity, formatDescStr, formatDescStr.Capacity))
            {
                modeDesc = modeDescStr.ToString();
                formatDesc = formatDescStr.ToString();
                result = true;
            }
            else
            {
                modeDesc = string.Empty;
                formatDesc = string.Empty;
            }

            return result;
        }

        [DllImport("AVProDeckLink")]
        public static extern bool SupportsInputModeAutoDetection(int deviceIndex);

        //////////////////////////////////////////////////////////////////////////////////////////////
        // Video Output Modes
        //////////////////////////////////////////////////////////////////////////////////////////////

        [DllImport("AVProDeckLink")]
        public static extern int GetNumVideoOutputModes(int deviceIndex);

        public static bool GetVideoOutputModeInfo(int deviceIndex, int modeIndex, out int width, out int height, out float frameRate, out long frameDuration, out int fieldMode, out string modeDesc, out string formatDesc)
        {
            bool result = false;
            StringBuilder modeDescStr = new StringBuilder(32);
            StringBuilder formatDescStr = new StringBuilder(32);
            if (GetVideoOutputModeInfo(deviceIndex, modeIndex, out width, out height, out frameRate, out frameDuration, out fieldMode, modeDescStr, modeDescStr.Capacity, formatDescStr, formatDescStr.Capacity))
            {
                modeDesc = modeDescStr.ToString();
                formatDesc = formatDescStr.ToString();
                result = true;
            }
            else
            {
                modeDesc = string.Empty;
                formatDesc = string.Empty;
            }

            return result;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////	
        // Keying
        //////////////////////////////////////////////////////////////////////////////////////////////	

        [DllImport("AVProDeckLink")]
        public static extern bool SupportsInternalKeying(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern bool SupportsExternalKeying(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern bool SwitchKeying(int deviceIndex, bool state, bool isExternal);

        //////////////////////////////////////////////////////////////////////////////////////////////	
        // Start / Stop
        //////////////////////////////////////////////////////////////////////////////////////////////	

        [DllImport("AVProDeckLink")]
        public static extern bool StartInputStream(int deviceIndex, int modeIndex, int numAudioChannels);

        [DllImport("AVProDeckLink")]
        public static extern bool StartOutputStream(int deviceIndex, int modeIndex);

        [DllImport("AVProDeckLink")]
        public static extern int GetVideoInputModeIndex(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern bool StopStream(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern bool Pause(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern bool Unpause(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern bool IsNoInputSignal(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern bool StopOutputStream(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern bool StopInputStream(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern void SetAutoDetectEnabled(int device, bool enabled);

        //////////////////////////////////////////////////////////////////////////////////////////////
        // Rendering
        //////////////////////////////////////////////////////////////////////////////////////////////

        [DllImport("AVProDeckLink")]
        public static extern void SetTexturePointer(int deviceIndex, System.IntPtr texturePointer);

        [DllImport("AVProDeckLink")]
        public static extern void SetOutputTexturePointer(int deviceIndex, System.IntPtr texturePtr);

		[DllImport("AVProDeckLink")]
		public static extern void SetOutputBufferPointer(int deviceIndex, byte[] buffer);

        [DllImport("AVProDeckLink")]
        public static extern System.IntPtr GetTexturePointer(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern ulong GetLastFrameUploaded(int deviceIndex);

        // Interlaced output frame notification
        [DllImport("AVProDeckLink")]
        public static extern void SetInterlacedOutputFrameReady(int deviceIndex, bool isReady);

        // SYNC

        [DllImport("AVProDeckLink")]
        public static extern void SetPresentFrame(long minTime, long maxTime);

        [DllImport("AVProDeckLink")]
        public static extern long GetLastCapturedFrameTime(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern System.IntPtr GetFramePixels(int deviceIndex, long time);

        //////////////////////////////////////////////////////////////////////////////////////////////
        // DEBUGGING
        //////////////////////////////////////////////////////////////////////////////////////////////

        [DllImport("AVProDeckLink")]
        public static extern int GetReadBufferIndex(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern int GetWriteBufferIndex(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern int GetOutputBufferedFramesCount(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern int GetFreeOutputBufferCount(int deviceIndex);

        [DllImport("AVProDeckLink")]
        public static extern int GetWaitingOutputBufferCount(int deviceIndex);

        //////////////////////////////////////////////////////////////////////////////////////////////
        // Private internal functions
        //////////////////////////////////////////////////////////////////////////////////////////////

        [DllImport("AVProDeckLink", CharSet = CharSet.Unicode)]
        private static extern bool GetDeviceName(int deviceIndex, StringBuilder name, int nameBufferLength);

        [DllImport("AVProDeckLink", CharSet = CharSet.Unicode)]
        private static extern bool GetDeviceDisplayName(int deviceIndex, StringBuilder name, int nameBufferLength);

        [DllImport("AVProDeckLink")]
        private static extern bool GetVideoInputModeInfo(int deviceIndex, int modeIndex, out int width, out int height, out float frameRate, out long frameDuration, out int fieldMode, StringBuilder modeDesc, int modeDescLength, StringBuilder formatDesc, int formatDescLength);

        [DllImport("AVProDeckLink")]
        private static extern bool GetVideoOutputModeInfo(int deviceIndex, int modeIndex, out int width, out int height, out float frameRate, out long frameDuration, out int fieldMode, StringBuilder modeDesc, int modeDescLength, StringBuilder formatDesc, int formatDescLength);

#if UNITY_5 && !UNITY_5_0 && !UNITY_5_1 || UNITY_5_4_OR_NEWER
        [DllImport("AVProDeckLink")]
        public static extern System.IntPtr GetRenderEventFunc();
#endif

        [DllImport("AVProDeckLink")]
        public static extern void FrameSent();

        [DllImport("AVProDeckLink")]
        public static extern void SetFrameNumber(int number);

        [DllImport("AVProDeckLink")]
        public static extern int FramesProcessed();
		[DllImport("AVProDeckLink")]
		public static extern void SetDeviceOutputReady(int deviceIndex);

		//////////////////////////////////////////////////////////////////////////////////////////////
		// Genlock functions
		//////////////////////////////////////////////////////////////////////////////////////////////
		[DllImport("AVProDeckLink")]
        public static extern bool IsGenLocked(int device);
        [DllImport("AVProDeckLink")]
        public static extern void SetGenlockOffset(int device, int offset);
        [DllImport("AVProDeckLink")]
        public static extern bool SupportsFullFrameGenlockOffset(int device);


        //////////////////////////////////////////////////////////////////////////////////////////////
        // Audio functions
        //////////////////////////////////////////////////////////////////////////////////////////////
        //It is important to lock/unlock before/after you call GetAudioBufferSize and GetAudioBuffer
        [DllImport("AVProDeckLink")]
        public static extern void GetAudioBuffer(int device, float[] buffer, int size, int channels, float volume);
        [DllImport("AVProDeckLink")]
        public static extern int GetMaxSupportedAudioChannels(int device);
        [DllImport("AVProDeckLink")]
        public static extern void OutputAudio(int deviceIndex, short[] data, int sizeInBytes);


		//////////////////////////////////////////////////////////////////////////////////////////////
		// Other functions
		//////////////////////////////////////////////////////////////////////////////////////////////
		[DllImport("AVProDeckLink")]
		private static extern bool ActivateLicense([MarshalAs(UnmanagedType.LPStr)] string productName, [MarshalAs(UnmanagedType.LPStr)] string licenseKey, uint iterationCount, StringBuilder licenseType, StringBuilder userName, StringBuilder userCompany, StringBuilder userEmail, StringBuilder expireMessage);

		public static bool ActivateLicense(string productName, string licenseKey, uint iterationCount, out string licenseType, out string userName, out string userCompany, out string userEmail, out string expireMessage)
		{
			bool result = false;
			StringBuilder licenseTypeStr = new StringBuilder(32);
			StringBuilder userNameStr = new StringBuilder(64);
			StringBuilder userCompanyStr = new StringBuilder(64);
			StringBuilder userEmailStr = new StringBuilder(128);
			StringBuilder expireMessageStr = new StringBuilder(256);
			if (ActivateLicense(productName, licenseKey, iterationCount, licenseTypeStr, userNameStr, userCompanyStr, userEmailStr, expireMessageStr))
			{
				licenseType = licenseTypeStr.ToString();
				userName = userNameStr.ToString();
				userCompany = userCompanyStr.ToString();
				userEmail = userEmailStr.ToString();
				result = true;
			}
			else
			{
				licenseType = "Invalid";
				userName = string.Empty;
				userCompany = string.Empty;
				userEmail = string.Empty;
			}

			expireMessage = expireMessageStr.ToString();

			return result;
		}

	}
}