using UnityEngine;
using System.Text;
using System.Collections.Generic;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
    [System.Serializable]
    public class Device : System.IDisposable
    {
        private int _deviceIndex;
        private string _name;
        private string _modelName;
        private List<DeviceMode> _inputModes;
        private List<DeviceMode> _outputModes;
        private bool _supportsInputModeAutoDetection;
        private bool _supportsInternalKeying;
        private bool _supportsExternalKeying;
		private bool _supportsConfigurableDuplex;
        private int _maxSupportedAudioChannels;
        private FormatConverter _formatConverter;
        private DeviceMode _currentMode;
        private DeviceMode _currentOutputMode = null;
        private int _frameCount;
        private float _startFrameTime;
        private bool _isActive = false;
        private DeckLinkOutput.KeyerMode _keyingMode;
        private bool _autoDeinterlace = false;
        private bool _receivedSignal = false;
        private bool _supportsFullFrameGenlockOffset;
        private int _genlockOffset;
        private int _audioChannels = 0;

        private bool _isStreamingOutput = false;
        private bool _isStreamingInput = false;

        private bool _fullDuplexSupported = false;

        public List<DeviceMode> InputModes{
            get { return _inputModes; }
        }

        public int AudioChannels
        {
            get { return _audioChannels; }
        }

		public bool BypassGammaCorrection
		{
			get { return _formatConverter == null ? false : _formatConverter.BypassGammaCorrection; }
			set
			{
				if(_formatConverter != null)
				{
					_formatConverter.BypassGammaCorrection = value;
				}
			}
		}

        public List<DeviceMode> OutputModes
        {
            get { return _outputModes; }
        }

        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                _isActive = value;
            }
        }

        public int DeviceIndex
        {
            get { return _deviceIndex; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string ModelName
        {
            get { return _modelName; }
        }

        public int NumInputModes
        {
            get { return _inputModes.Count; }
        }

        public int NumOutputModes
        {
            get { return _outputModes.Count; }
        }

        public bool SupportsInputModeAutoDetection
        {
            get { return _supportsInputModeAutoDetection; }
        }

        public bool SupportsInternalKeying
        {
            get { return _supportsInternalKeying; }
        }

        public bool SupportsExternalKeying
        {
            get { return _supportsExternalKeying; }
        }

        public int MaxAudioChannels
        {
            get { return _maxSupportedAudioChannels; }
        }

		public bool FlipInputX
		{
			get { return _formatConverter == null ? false : _formatConverter.FlipX; }
			set {
				if (_formatConverter != null)
				{
					_formatConverter.FlipX = value;
				}
			}
		}

		public bool FlipInputY
		{
			get { return _formatConverter == null ? false : _formatConverter.FlipY; }
			set
			{
				if (_formatConverter != null)
				{
					_formatConverter.FlipY = value;
				}
			}
		}

		public Texture OutputTexture
        {
            get { if (_formatConverter != null && _formatConverter.ValidPicture) return _formatConverter.OutputTexture; return null; }
        }

        public ulong OutputFrameNumber
        {
            get { if (_formatConverter != null && _formatConverter.ValidPicture) return (ulong)_formatConverter.OutputFrameNumber; return 0; }
        }

        public DeviceMode CurrentMode
        {
            get { return _currentMode; }
        }

        public DeviceMode CurrentOutputMode
        {
            get { return _currentOutputMode; }
        }

        public bool IsStreaming
        {
            get;
            private set;
        }

        public bool IsStreamingOutput{
            get
            {
                return _isStreamingOutput;
            }
        }

        public bool IsStreamingInput
        {
            get
            {
                return _isStreamingInput;
            }
        }

        public bool FullDuplexSupported
        {
            get
            {
                return _fullDuplexSupported;
            }
        }

        public int GenlockOffset
        {
            get { return _genlockOffset; }
            set { _genlockOffset = value; }
        }

        public bool IsStreamingAudio
        {
            get;
            private set;
        }

        public DeckLinkOutput.KeyerMode CurrentKeyingMode
        {
            get { return _keyingMode; }
            set { SetKeying(value); }
        }

        public bool IsPaused
        {
            get;
            private set;
        }

		public bool IsConfigurableDuplex
		{
			get { return _supportsConfigurableDuplex; }
		}

        public bool IsPicture
        {
            get;
            private set;
        }

        public float FPS
        {
            get;
            private set;
        }

        public int FramesTotal
        {
            get;
            private set;
        }

        public bool AutoDeinterlace
        {
            get { return _autoDeinterlace; }
            set { _autoDeinterlace = value; if (_formatConverter != null) _formatConverter.AutoDeinterlace = _autoDeinterlace; }
        }

        public bool ReceivedSignal
        {
            get { return _receivedSignal; }
        }

        public bool IsGenLocked
        {
            get { return DeckLinkPlugin.IsGenLocked(_deviceIndex); }
        }

        public bool SupportsFullFrameGenlockOffset
        {
            get { return _supportsFullFrameGenlockOffset; }
        }

        public Device(string modelName, string name, int index)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            IsStreamingAudio = false;
            IsStreaming = false;
            IsPaused = true;
            IsPicture = false;
            _name = name;
            _modelName = ModelName;
            _deviceIndex = index;
            _genlockOffset = 0;
            _supportsInputModeAutoDetection = DeckLinkPlugin.SupportsInputModeAutoDetection(_deviceIndex);
            _supportsInternalKeying = DeckLinkPlugin.SupportsInternalKeying(_deviceIndex);
            _supportsExternalKeying = DeckLinkPlugin.SupportsExternalKeying(_deviceIndex);
            _maxSupportedAudioChannels = DeckLinkPlugin.GetMaxSupportedAudioChannels(_deviceIndex);
            _supportsFullFrameGenlockOffset = DeckLinkPlugin.SupportsFullFrameGenlockOffset(_deviceIndex);
			_supportsConfigurableDuplex = DeckLinkPlugin.ConfigurableDuplexMode(_deviceIndex);
			_inputModes = new List<DeviceMode>(32);
            _outputModes = new List<DeviceMode>(32);
            _formatConverter = new FormatConverter();
            _fullDuplexSupported = DeckLinkPlugin.FullDuplexSupported(_deviceIndex);
            EnumModes();
#endif
        }

        public void Dispose()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (_formatConverter != null)
            {
                _formatConverter.Dispose();
                _formatConverter = null;
            }
#endif
        }

        public bool StartInput(DeviceMode mode, int numAudioChannels, bool delayResourceCreationUntilFramesStart = false)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (!CanInput())
            {
                Debug.Log("[AVProDeckLink] Warning: Unable to start input for device " + _name + " as it is currently busy");
                return false;
            }

            if (mode != null)
                return StartInput(mode.Index, numAudioChannels, delayResourceCreationUntilFramesStart);
#endif
            return false;
        }

        public bool StartOutput(DeviceMode mode)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (!CanOutput())
            {
                Debug.LogWarning("[AVProDeckLink] Warning: Unable to start output for device " + _name + " as it is currently busy");
                return false;
            }
            
            if (mode != null)
                return StartOutput(mode.Index);
#endif
            return false;
        }

        public bool StartInput(int modeIndex, int numAudioChannels, bool delayResourceCreationUntilFramesStart = false)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            bool result = false;

            if(modeIndex == -1)
            {
                return false;
            }

            if (!CanInput())
            {
                Debug.LogWarning("[AVProDeckLink] Warning: Unable to start input for device " + _name + " as it is currently busy");
                return false;
            }

            if (DeckLinkPlugin.StartInputStream(_deviceIndex, modeIndex, numAudioChannels))
            {
                _audioChannels = numAudioChannels;
                _currentMode = _inputModes[modeIndex];
                result = true;

                if (_currentMode.Width > 0 && _currentMode.Width <= 4096 && _currentMode.Height > 0 && _currentMode.Height <= 4096)
                {
                    _formatConverter.AutoDeinterlace = _autoDeinterlace;
                    if (_formatConverter.Build(_deviceIndex, _currentMode, delayResourceCreationUntilFramesStart))
                    {
                        ResetFPS();
                        IsActive = true;
                        IsStreaming = true;
                        IsPicture = false;
                        IsPaused = false;
                        result = true;
                        _isStreamingInput = true;
                    }
                    else
                    {
                        Debug.LogWarning("[AVProDeckLink] unable to convert camera format");
                    }
                }
                else
                {
                    Debug.LogWarning("[AVProDeckLink] invalid width or height");
                }
            }
            else
            {
                Debug.LogWarning("[AVProDeckLink] Unable to start input stream on device " + _name);
            }

            if (!result)
            {
                StopInput();
            }

            return result;
#else
            return false;
#endif
        }

        /// <summary>
        /// This is used by the dynamic automatic input detection
        /// </summary>
        private bool ChangeInput(int modeIndex)
        {
            bool result = false;
            if (_isStreamingInput)
            {
                DeviceMode newMode = _inputModes[modeIndex];
                Debug.Log("[AVProDeckLink] Changing device '" + this._name + "' input " + newMode.Width + "x" + newMode.Height + " (" + newMode.ModeDescription + " " + newMode.PixelFormatDescription + ")");

                if (_formatConverter.Build(_deviceIndex, newMode, false))
                {
                    _currentMode = newMode;
                    result = true;
                }
            }

            if (!result)
            {
                Debug.LogWarning("[AVProDeckLink] unable to change input device mode");
                StopInput();
            }

            return result;
        }

        /*public bool StartAudioOutput(int numChannels)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (IsStreamingAudio)
            {
                StopAudio();
            }

            if (numChannels > 0 && numChannels <= MaxAudioChannels)
            {
                IsStreamingAudio = DeckLinkPlugin.StartAudioOutput(_deviceIndex, numChannels);
            }
            else
            {
                Debug.LogError("Unsupported number of audio channels " + numChannels + " vs " + MaxAudioChannels);
            }

            return IsStreamingAudio;
#else
            return false;
#endif
        }*/

        public bool StartOutput(int modeIndex)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            bool result = false;
            
            if (_isStreamingOutput)
            {
                Debug.Log("Warning: Please stop device before starting new stream");
                return false;
            }

            DeckLinkPlugin.SetGenlockOffset(_deviceIndex, _genlockOffset);

            if (DeckLinkPlugin.StartOutputStream(_deviceIndex, modeIndex))
            {
                //_currentOutputMode = _outputModes[modeIndex];
                _currentOutputMode = _outputModes[modeIndex];

                //if (_currentOutputMode.Width > 0 && _currentOutputMode.Width <= 4096 && _currentOutputMode.Height > 0 && _currentOutputMode.Height <= 4096)
                if (_currentOutputMode.Width > 0 && _currentOutputMode.Width <= 4096 && _currentOutputMode.Height > 0 && _currentOutputMode.Height <= 4096)
                {
                    ResetFPS();
                    IsActive = true;
                    IsStreaming = true;
                    _isStreamingOutput = true;
                    IsPicture = false;
                    IsPaused = false;
                    result = true;
                }
                else
                {
                    Debug.LogWarning("[AVProDeckLink] invalid width or height");
                }
            }

            /*if(!DeckLinkPlugin.StartAudioOutput(_deviceIndex, 2))
            {
                Debug.LogWarning("[AVProDeckLink] Unable to start audio output stream");
            }*/

            if (!result)
            {
                Debug.LogWarning("[AVProDeckLink] unable to start output device");
                StopOutput();
            }

            return result;
#else
            return false;
#endif
        }

        public void Pause()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (_isStreamingInput)
            {
                DeckLinkPlugin.Pause(_deviceIndex);
                IsPaused = true;
            }
#endif
        }

        public void Unpause()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (_isStreamingInput)
            {
                DeckLinkPlugin.Unpause(_deviceIndex);
                IsPaused = false;
            }
#endif
        }

        public bool CanInput()
        {
            if (_fullDuplexSupported)
            {
                return !IsStreamingInput;
            }
            else
            {
                return !IsStreaming;
            }
        }

        public bool CanOutput()
        {
            if (_fullDuplexSupported)
            {
                return !IsStreamingOutput;
            }
            else
            {
                return !IsStreaming;
            }
        }

        public void Update()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (_isStreamingInput)
            {
                // Check if the input mode has changed
                if (DeckLinkPlugin.IsNoInputSignal(_deviceIndex))
                {
                    _receivedSignal = false;
                    return;
                }
                else
                {
                    _receivedSignal = true;
                }

                int modeIndex = DeckLinkPlugin.GetVideoInputModeIndex(_deviceIndex);
                if (modeIndex != _currentMode.Index)
                {
                    var newMode = _inputModes[modeIndex];

                    if (!FormatConverter.InputFormatSupported(newMode.PixelFormat))
                    {
                        Debug.LogWarning("Auto detected format for input device not currently supported");
                    }

                    if (modeIndex >= 0 && modeIndex < _inputModes.Count)
                    {
                        // If the device has changed mode we may need to rebuild buffers
                        ChangeInput(modeIndex);
                        return;
                    }
                }

                // Update textures
                if (_formatConverter != null)
                {
                    if (_formatConverter.Update())
                    {
                        UpdateFPS();
                    }
                }
            }
#endif
        }

		public void SetDuplexMode(bool isFull)
		{
			DeckLinkPlugin.SetDuplexMode(_deviceIndex, isFull);
		}

        protected void ResetFPS()
        {
            _frameCount = 0;
            FramesTotal = 0;
            FPS = 0.0f;
            _startFrameTime = 0.0f;
        }

        public void UpdateFPS()
        {
            _frameCount++;
            FramesTotal++;

            float timeNow = Time.realtimeSinceStartup;
            float timeDelta = timeNow - _startFrameTime;
            if (timeDelta >= 1.0f)
            {
                FPS = (float)_frameCount / timeDelta;
                _frameCount = 0;
                _startFrameTime = timeNow;
            }
        }

        public void Stop()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            _currentMode = null;

            _isStreamingOutput = false;
            _isStreamingInput = false;
            IsStreaming = false;
            IsPaused = false;
            ResetFPS();
            DeckLinkPlugin.StopStream(_deviceIndex);
            _keyingMode = DeckLinkOutput.KeyerMode.None;
#endif
        }

        public void StopOutput()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            _currentOutputMode = null;
            _isStreamingOutput = false;
            IsStreaming = _isStreamingOutput || _isStreamingInput;
            ResetFPS();
            DeckLinkPlugin.StopOutputStream(_deviceIndex);
            _keyingMode = DeckLinkOutput.KeyerMode.None;
#endif
        }

        public void StopInput()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            _currentMode = null;
            _isStreamingInput = false;
            IsStreaming = _isStreamingOutput || _isStreamingInput;
            IsPaused = false;
            ResetFPS();
            DeckLinkPlugin.StopInputStream(_deviceIndex);
            DeckLinkPlugin.SetTexturePointer(_deviceIndex, System.IntPtr.Zero);
#endif
        }

        public DeviceMode GetInputMode(int index)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            DeviceMode result = null;

            if (index >= 0 && index < _inputModes.Count)
                result = _inputModes[index];

            return result;
#else
            return null;
#endif
        }

        public DeviceMode GetOutputMode(int index)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            DeviceMode result = null;

            if (index >= 0 && index < _outputModes.Count)
                result = _outputModes[index];

            return result;
#else
            return null;
#endif
        }

        private void SetKeying(DeckLinkOutput.KeyerMode mode)
        {
            if (_supportsInternalKeying || _supportsExternalKeying)
            {
                DeckLinkPlugin.SwitchKeying(_deviceIndex, mode != DeckLinkOutput.KeyerMode.None, mode == DeckLinkOutput.KeyerMode.External);
                _keyingMode = mode;
            }
        }

        private void EnumModes()
        {
            int numModes = DeckLinkPlugin.GetNumVideoInputModes(_deviceIndex);
            for (int modeIndex = 0; modeIndex < numModes; modeIndex++)
            {
                int width, height;
                float frameRate;
                string modeDesc;
                string pixelFormatDesc;
                long frameDuration;
                int fieldMode;
                if (DeckLinkPlugin.GetVideoInputModeInfo(_deviceIndex, modeIndex, out width, out height, out frameRate, out frameDuration, out fieldMode, out modeDesc, out pixelFormatDesc))
                {
                    DeviceMode mode = new DeviceMode(this, modeIndex, width, height, frameRate, frameDuration, (DeviceMode.FieldMode)fieldMode, modeDesc, pixelFormatDesc);
                    _inputModes.Add(mode);
                }
            }

            numModes = DeckLinkPlugin.GetNumVideoOutputModes(_deviceIndex);
            for (int modeIndex = 0; modeIndex < numModes; modeIndex++)
            {
                int width, height;
                float frameRate;
                string modeDesc;
                string pixelFormatDesc;
                long frameDuration;
                int fieldMode;
                if (DeckLinkPlugin.GetVideoOutputModeInfo(_deviceIndex, modeIndex, out width, out height, out frameRate, out frameDuration, out fieldMode, out modeDesc, out pixelFormatDesc))
                {
                    DeviceMode mode = new DeviceMode(this, modeIndex, width, height, frameRate, frameDuration, (DeviceMode.FieldMode)fieldMode, modeDesc, pixelFormatDesc);
                    _outputModes.Add(mode);
                }
            }
        }
    }
}