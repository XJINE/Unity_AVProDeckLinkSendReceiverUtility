using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
	[StructLayout(LayoutKind.Sequential, Size = 20)]
	public struct ComputeBufferParams
	{
		public uint width;
		public uint height;
		public uint bufferWidth;
		public uint bigEndian;
		public uint leading;
		public uint isLinear;

		public static int Size()
		{
			return 24;
		}
	}

    [AddComponentMenu("AVPro DeckLink/DeckLinkOutput")]
    public class DeckLinkOutput : DeckLink
    {
		//Anti-Aliasing
        public enum AALevel
        {
            None = 1,
            Two = 2,
            Four = 4,
            Eight = 8
        }

		public AALevel _antiAliasingLevel = AALevel.Two;

		//buffering & timing
		[Range(2, 9)]
		public int _bufferBalance = 2;

		public enum NoCameraMode
		{
			None,
			Colour,
			DefaultTexture
		}

		public NoCameraMode _noCameraMode;
		public Texture _defaultTexture;
		public Color _defaultColour;

		private int _outputFrameRate = -1;
		private int curr_frame = 0;
		private bool _canOutputFrame = false;
		private int _targetFrameRate = -1;
		private float prevFrameTime = 0.0f;
		private float currFrameTime = 0.0f;
		private float _timeSinceLastFrame = 0f;

		//pipeline textures/
		public Camera _camera;

		private Shader _rgbaToYuv422Shader;
		private Shader _rgbaToBgraShader;
		private Shader _rgbaToArgbShader;
		private Shader _interlaceShader;
		private Shader _blendShader;
		private ComputeShader _abgrTo10bitARGB;

		private RenderTexture _inputTexture;
		private RenderTexture[] _capturedFrames = null;
		private RenderTexture _blended;
		private RenderTexture _interlacedTexture;
		private RenderTexture _convertedTexture;
		private ComputeBuffer _convertedCompBuffer = null;
		private ComputeBuffer _parameters = null;
		private byte[] _outputBuffer = null;

		private Material _interlaceMaterial;
		private Material _conversionMaterial;
		private Material _blendMat;

		private DeckLinkPlugin.PixelFormat _format = DeckLinkPlugin.PixelFormat.Unknown;
		private bool _interlaced;
		private int _interlacePass = 0;
		private IntPtr _convertedPointer = IntPtr.Zero;
		
		private byte _currCapturedFrame = 0;

		//Audio
		public AudioSource _outputAudioSource;
		public bool _muteOutputAudio = false;

		private DeckLinkAudioOutput _audioOutputManager = null;

		public bool _bypassGamma = false;

		//misc
		public int _genlockPixelOffset = 0;

		private static int refCount = 0;
        private static int prevRefCount;

		public RenderTexture InputTexture
        {
            get { return _inputTexture; }
        }
        public enum KeyerMode
        {
            None = 0,
            Internal,
            External,
        }

		public KeyerMode _keyerMode = KeyerMode.None;

		private void FindShaders()
		{
			_rgbaToYuv422Shader = Shader.Find("AVProDeckLink/RGBA 4:4:4 to UYVY 4:2:2");
			_rgbaToBgraShader = Shader.Find("AVProDeckLink/RGBA 4:4:4 to BGRBA 4:4:4");
			_rgbaToArgbShader = Shader.Find("AVProDeckLink/RGBA 4:4:4 to ARGB 4:4:4");
			_interlaceShader = Shader.Find("AVProDeckLink/Interlacer");
			_blendShader = Shader.Find("AVProDeckLink/BlendFrames");
			_abgrTo10bitARGB = (ComputeShader)Resources.Load("Shaders/AVProDeckLink_RGBA_to_10RGBX");
		}

		private void InitializeAudioOutput()
		{
			DeckLinkAudioOutput[] audioOutputs = FindObjectsOfType<DeckLinkAudioOutput>();
			if (audioOutputs.Length > 1)
			{
				Debug.LogError("[AVProDeckLink] There should never be more than one DeckLinkAudioOutput object per scene");
			}
			else if (audioOutputs.Length == 1)
			{
				_audioOutputManager = audioOutputs[0];
			}
			else
			{
				if (_outputAudioSource == null)
				{
					AudioListener[] listeners = FindObjectsOfType<AudioListener>();

					GameObject listenerObject;

					if (listeners.Length == 0)
					{
						listenerObject = new GameObject("[AVProDeckLink]Listener");
						listenerObject.AddComponent<AudioListener>();
					}
					else
					{
						listenerObject = listeners[0].gameObject;
					}

					_audioOutputManager = listenerObject.AddComponent<DeckLinkAudioOutput>();

#if UNITY_5 && (UNITY_5_1 || UNITY_5_2)
                    DeckLinkAudioOutput temp = listenerObject.AddComponent<DeckLinkAudioOutput>();
                    Destroy(temp);
#endif
				}
				else
				{
					_audioOutputManager = _outputAudioSource.gameObject.AddComponent<DeckLinkAudioOutput>();
				}
			}
		}

		private void UpdateReferenceCounter()
		{
			if (refCount == 0)
			{
				prevRefCount = QualitySettings.vSyncCount;
				QualitySettings.vSyncCount = 0;
			}

			refCount++;
		}

		public override void Awake()
		{
			base.Awake();

			UpdateReferenceCounter();
			FindShaders();
			InitializeAudioOutput();
		}

		private void InitCaptureBlendResources(int width, int height)
		{
			if(_capturedFrames != null)
			{
				foreach(var frame in _capturedFrames)
				{
					RenderTexture.ReleaseTemporary(frame);
				}
				_capturedFrames = null;
			}

			if(_blended != null && (_blended.width != width || _blended.height != height || _blended.antiAliasing != (int)_antiAliasingLevel))
			{
				RenderTexture.ReleaseTemporary(_blended);
				_blended = null;
			}

			if (DeckLinkSettings.Instance._multiOutput)
			{
				_capturedFrames = new RenderTexture[2];
				for(int i = 0; i < 2; ++i)
				{
					_capturedFrames[i] = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
						/*QualitySettings.activeColorSpace == ColorSpace.Linear ? RenderTextureReadWrite.Linear : */RenderTextureReadWrite.Default, (int)_antiAliasingLevel);
				}
			}

			if(_blended == null)
			{
				_blended = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
					/*QualitySettings.activeColorSpace == ColorSpace.Linear ? RenderTextureReadWrite.Linear : */RenderTextureReadWrite.Default, (int)_antiAliasingLevel);
			}

			if (_blendMat == null)
			{
				_blendMat = new Material(_blendShader);
			}

			if (_inputTexture != null)
			{
				if (_inputTexture.width != width || _inputTexture.height != height || _inputTexture.antiAliasing != (int)_antiAliasingLevel)
				{
					RenderTexture.ReleaseTemporary(_inputTexture);
					_inputTexture = null;
				}
			}

			if (_inputTexture == null)
			{
				_inputTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32,
					/*QualitySettings.activeColorSpace == ColorSpace.Linear ? RenderTextureReadWrite.Linear : */RenderTextureReadWrite.Default, (int)_antiAliasingLevel);
			}

			if (!_inputTexture.IsCreated())
			{
				_inputTexture.Create();
			}
		}

        private void InitConversionResources(DeckLinkPlugin.PixelFormat format, int width, int height)
        {
            if (_conversionMaterial != null || _format != format)
            {
                Material.Destroy(_conversionMaterial);
                _conversionMaterial = null;
                _format = format;
            }

			int texWidth = -1;
            if (_conversionMaterial == null)
            {
                switch (format)
                {
                    case DeckLinkPlugin.PixelFormat.YCbCr_8bpp_422:
                        _conversionMaterial = new Material(_rgbaToYuv422Shader);
						texWidth = width / 2;
						break;
                    case DeckLinkPlugin.PixelFormat.BGRA_8bpp_444:
                        _conversionMaterial = new Material(_rgbaToBgraShader);
						texWidth = width;
						break;
                    case DeckLinkPlugin.PixelFormat.ARGB_8bpp_444:
                        _conversionMaterial = new Material(_rgbaToArgbShader);
						texWidth = width;
						break;
                    default:
                        break;
                }
            }

			if (_parameters != null)
			{
				_parameters.Release();
				_parameters = null;
			}

			DeckLinkPlugin.SetOutputBufferPointer(_deviceIndex, null);
			DeckLinkPlugin.SetOutputTexturePointer(_deviceIndex, IntPtr.Zero);

			if (_convertedTexture != null)
			{
				RenderTexture.ReleaseTemporary(_convertedTexture);
				_convertedTexture = null;
				_convertedPointer = IntPtr.Zero;
			}

			if(_outputBuffer != null)
			{
				_outputBuffer = null;
			}

			if(_convertedCompBuffer != null)
			{
				_convertedCompBuffer.Release();
				_convertedCompBuffer = null;
			}

			if (texWidth < 0)
			{
				//sets up compute buffers 
				if (_format == DeckLinkPlugin.PixelFormat.RGBX_10bpp_444 || _format == DeckLinkPlugin.PixelFormat.RGBX_10bpp_444_LE
					|| _format == DeckLinkPlugin.PixelFormat.RGB_10bpp_444)
				{
					_parameters = new ComputeBuffer(1, ComputeBufferParams.Size());

					ComputeBufferParams[] parms = new ComputeBufferParams[1];
					parms[0].height = (uint)height;
					parms[0].width = (uint)width;
					parms[0].bufferWidth = (uint)(width + 63) / 64 * 64;
					parms[0].leading = _format == DeckLinkPlugin.PixelFormat.RGB_10bpp_444 ? 1U : 0U;
					bool formatBigEndian = _format != DeckLinkPlugin.PixelFormat.RGBX_10bpp_444_LE ? true : false;
					if(BitConverter.IsLittleEndian)
					{
						formatBigEndian = !formatBigEndian;
					}
					parms[0].bigEndian = formatBigEndian ? 1U : 0U;
					parms[0].isLinear = QualitySettings.activeColorSpace == ColorSpace.Linear ? 1U : 0U;

					_outputBuffer = new byte[parms[0].bufferWidth * parms[0].height * 4];
					_convertedCompBuffer = new ComputeBuffer((int)(parms[0].bufferWidth * parms[0].height), 4, ComputeBufferType.Raw);

					_parameters.SetData(parms);

					DeckLinkPlugin.SetOutputBufferPointer(_deviceIndex, _outputBuffer);
				}
				else
				{
					_convertedTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, 
						QualitySettings.activeColorSpace == ColorSpace.Linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default, 1);
					_convertedPointer = _convertedTexture.GetNativeTexturePtr();
					DeckLinkPlugin.SetOutputTexturePointer(_deviceIndex, _convertedPointer);
				}
			}
			else
			{
				_convertedTexture = RenderTexture.GetTemporary(texWidth, height, 0, RenderTextureFormat.ARGB32,
					QualitySettings.activeColorSpace == ColorSpace.Linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default, 1);
				_convertedPointer = _convertedTexture.GetNativeTexturePtr();
				DeckLinkPlugin.SetOutputTexturePointer(_deviceIndex, _convertedPointer);
			}
        }

        private void InitInterlaceResources(int width, int height)
        {
            if (_interlaceMaterial == null)
            {
                _interlaceMaterial = new Material(_interlaceShader);
            }

            _interlaceMaterial.SetFloat("_TextureHeight", height);

            if (_interlacedTexture != null)
            {
                if (_interlacedTexture.width != width || _interlacedTexture.height != height)
                {
                    RenderTexture.ReleaseTemporary(_interlacedTexture);
                    _interlacedTexture = null;
                }
            }

			if (_interlacedTexture == null)
            {
                _interlacedTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                _interlacedTexture.filterMode = FilterMode.Point;

                if (!_interlacedTexture.IsCreated())
                {
                    _interlacedTexture.Create();
                }
            }
		}

        public static bool OutputFormatSupported(DeckLinkPlugin.PixelFormat format)
        {
            switch (format)
            {
                case DeckLinkPlugin.PixelFormat.YCbCr_8bpp_422:
                case DeckLinkPlugin.PixelFormat.BGRA_8bpp_444:
                case DeckLinkPlugin.PixelFormat.ARGB_8bpp_444:
				case DeckLinkPlugin.PixelFormat.RGBX_10bpp_444:
				case DeckLinkPlugin.PixelFormat.RGBX_10bpp_444_LE:
				case DeckLinkPlugin.PixelFormat.RGB_10bpp_444:
                    return true;
                default:
                    return false;
            }
        }

		public int TargetFramerate
		{
			get { return _targetFrameRate; }
		}

		public int OutputFramerate
		{
			get
			{
				if (DeckLinkSettings.Instance._multiOutput)
				{
					return _outputFrameRate;
				}
				else
				{
					return Application.targetFrameRate;
				}
			}
		}

		public void SetCamera(Camera camera)
		{
			if (_camera != null)
			{
				_camera.targetTexture = null;
			}

			_camera = camera;

			if(camera != null)
			{
				camera.targetTexture = _inputTexture;
			}
		}

		public bool CanOutputFrame()
		{
			if (Time.frameCount != curr_frame)
			{
				curr_frame = Time.frameCount;

				float secondsPerFrame = 1f / (float)_outputFrameRate;
				float delta = Mathf.Min(secondsPerFrame, Time.unscaledDeltaTime);

				_timeSinceLastFrame += delta;

				if (_outputFrameRate < 0 || _timeSinceLastFrame >= secondsPerFrame)
				{
					if (secondsPerFrame > 0)
					{
						_timeSinceLastFrame = _timeSinceLastFrame % secondsPerFrame;
						_canOutputFrame = true;
					}
					else
					{
						_timeSinceLastFrame = 0;
						_canOutputFrame = true;
					}

				}
				else
				{
					_canOutputFrame = false;
				}
			}

			return _canOutputFrame;
		}

		private void RegisterAudioOutput()
		{
			if (_audioOutputManager != null)
			{
				_audioOutputManager.RegisterDevice(_device.DeviceIndex);
			}
		}

		private void AttachToCamera()
		{
			if (_camera != null)
			{
				_camera.targetTexture = _inputTexture;
			}
			else if (gameObject.GetComponent<Camera>() != null)
			{
				_camera = gameObject.GetComponent<Camera>();
				_camera.targetTexture = _inputTexture;
			}
		}

		protected override void BeginDevice()
		{
			_currCapturedFrame = 0;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			_device.GenlockOffset = _genlockPixelOffset;

			if (!_device.StartOutput(_modeIndex))
			{
				Debug.LogWarning("[AVProDeckLink] device failed to start.");
				StopOutput();
				_device = null;
			}
			else
			{
				DeviceMode mode = _device.GetOutputMode(_modeIndex);

				RegisterAudioOutput();
				float framerate = mode.FrameRate;

				InitCaptureBlendResources(mode.Width, mode.Height);

				if (mode.InterlacedFieldMode)
				{
					_interlaced = true;
					framerate *= 2;
					InitInterlaceResources(mode.Width, mode.Height);
				}
				else
				{
					_interlaced = false;
				}

				InitConversionResources(mode.PixelFormat, mode.Width, mode.Height);

				if (!DeckLinkSettings.Instance._multiOutput)
				{
					Application.targetFrameRate = Time.captureFramerate = _targetFrameRate = Mathf.CeilToInt(framerate);
				}
				else
				{
					Application.targetFrameRate = Time.captureFramerate = -1;
					_outputFrameRate = _targetFrameRate = Mathf.CeilToInt(framerate);
				}

				if (_keyerMode != KeyerMode.None)
				{
					_device.CurrentKeyingMode = _keyerMode;
				}

				AttachToCamera();
			}
#endif
		}
		
		private void UnregisterAudioOutput()
		{
			if (_audioOutputManager != null)
			{
				if (_device != null)
				{
					_audioOutputManager.UnregisterDevice(_device.DeviceIndex);
				}
			}
		}

		public bool StopOutput()
		{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			UnregisterAudioOutput();

			if (_device != null)
			{
				_device.StopOutput();
				_device = null;
			}

			_targetFrameRate = -1;
			if (DeckLinkManager.Instance != null)
			{
				_outputFrameRate = -1;
			}

			Application.targetFrameRate = Time.captureFramerate = -1;

			_interlaced = false;

			return true;
#else
            return false;
#endif
		}

		protected override void Cleanup()
		{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			StopOutput();

			if (_inputTexture != null)
			{
				RenderTexture.ReleaseTemporary(_inputTexture);
				_inputTexture = null;
			}

			if(_capturedFrames != null)
			{
				foreach(var frame in _capturedFrames)
				{
					RenderTexture.ReleaseTemporary(frame);
				}
				_capturedFrames = null;
			}

			if(_blended != null)
			{
				RenderTexture.ReleaseTemporary(_blended);
				_blended = null;
			}

			if (_interlacedTexture != null)
			{
				RenderTexture.ReleaseTemporary(_interlacedTexture);
				_blended = null;
			}

			DeckLinkPlugin.SetOutputBufferPointer(_deviceIndex, null);
			DeckLinkPlugin.SetOutputTexturePointer(_deviceIndex, IntPtr.Zero);

			if(_convertedTexture != null)
			{
				RenderTexture.ReleaseTemporary(_convertedTexture);
				_convertedTexture = null;
				_convertedPointer = IntPtr.Zero;
			}

			if (_outputBuffer != null)
			{
				_outputBuffer = null;
			}

			if (_parameters != null)
			{
				_parameters.Release();
				_parameters = null;
			}

			if (_convertedCompBuffer != null)
			{
				_convertedCompBuffer.Release();
				_convertedCompBuffer = null;
			}

			if(_interlaceMaterial != null)
			{
				Destroy(_interlaceMaterial);
				_interlaceMaterial = null;
			}

			if(_conversionMaterial != null)
			{
				Destroy(_conversionMaterial);
				_conversionMaterial = null;
			}

			if(_blendMat != null)
			{
				Destroy(_blendMat);
				_blendMat = null;
			}
#endif
		}

		private void Convert(Texture inputTexture)
		{
			if(_convertedTexture != null)
			{
				if (_conversionMaterial != null)
				{
					Graphics.Blit(inputTexture, _convertedTexture, _conversionMaterial);
				}
				else
				{
					Graphics.Blit(inputTexture, _convertedTexture);
				}
			}
			else if(_convertedCompBuffer != null)
			{
				if (_abgrTo10bitARGB == null)
				{
					Debug.LogError("[AVProDeckLink] Unable to find shader to covert ABGR to 10bit RGBA");
					return;
				}
				int kernelHandle = _abgrTo10bitARGB.FindKernel("RGBA_to_10RGBX");
				_abgrTo10bitARGB.SetTexture(kernelHandle, "input", inputTexture);
				_abgrTo10bitARGB.SetBuffer(kernelHandle, "result", _convertedCompBuffer);
				_abgrTo10bitARGB.SetBuffer(kernelHandle, "constBuffer", _parameters);
				_abgrTo10bitARGB.Dispatch(kernelHandle, inputTexture.width / 8, inputTexture.height / 8, 1);

				_convertedCompBuffer.GetData(_outputBuffer);
			}
			else
			{
				Debug.Log("[AVPro DeckLink] Something really wrong happened, this path shouldn't be possible");
			}
		}

		private void CaptureFrame()
		{
			if(_camera == null)
			{
				if(_noCameraMode == NoCameraMode.Colour)
				{
					var curr = RenderTexture.active;
					Graphics.SetRenderTarget(_inputTexture);
					GL.Clear(true, true, _defaultColour);
					Graphics.SetRenderTarget(curr);
				}
				else if(_noCameraMode == NoCameraMode.DefaultTexture)
				{
					Graphics.Blit(_defaultTexture != null ? _defaultTexture : Texture2D.blackTexture, _inputTexture);
				}
			}

			if(_capturedFrames != null)
			{
				Graphics.Blit(_inputTexture, _capturedFrames[_currCapturedFrame]);
				prevFrameTime = currFrameTime;
				currFrameTime = Time.unscaledTime;

				_currCapturedFrame = (byte)((_currCapturedFrame + 1) % 2);
			}
			else
			{
				Graphics.Blit(_inputTexture, _blended);
			}
		}

		private void ProcessAudio()
		{
			if (_audioOutputManager)
			{
				if (_muteOutputAudio)
				{
					_audioOutputManager.UnregisterDevice(_deviceIndex);
				}
				else
				{
					_audioOutputManager.RegisterDevice(_deviceIndex);
				}
			}
		}

		private void BlendCapturedFrames()
		{
			float timeSinceLastRenderedFrame = currFrameTime - prevFrameTime;

			float t = 1f - (timeSinceLastRenderedFrame == 0f ? 1f : _timeSinceLastFrame / timeSinceLastRenderedFrame);
			t = Mathf.Clamp01(t);

			_blendMat.SetFloat("_t", t);

			uint currTex = (_currCapturedFrame + 1U) % 2U;
			_blendMat.SetTexture("_AfterTex", _capturedFrames[currTex]);
			Graphics.Blit(_capturedFrames[_currCapturedFrame], _blended, _blendMat);
		}

		private RenderTexture Interlace(RenderTexture inputTexture)
		{
			if (_interlaced)
			{
				if(_interlacedTexture == null || _interlaceMaterial == null)
				{
					Debug.LogError("[AVPro DeckLink] Something went really wrong, I should not be here :(");
				}

				Graphics.Blit(inputTexture, _interlacedTexture, _interlaceMaterial, _interlacePass);
				// Notify the plugin that the interlaced frame is complete now
				DeckLinkPlugin.SetInterlacedOutputFrameReady(_device.DeviceIndex, _interlacePass == 1);

				_interlacePass = (_interlacePass + 1) % 2;

				return _interlacedTexture;
			}

			return inputTexture;
		}

		private void AdjustPlaybackFramerate()
		{
			int numWaitingOutputFrames = DeckLinkPlugin.GetOutputBufferedFramesCount(_device.DeviceIndex);

			// Dynamically adjust frame rate so we get a smooth output
			int target = _targetFrameRate;

			if (numWaitingOutputFrames < _bufferBalance)
			{
				target = Mathf.CeilToInt(_targetFrameRate + 1);
			}
			else if (numWaitingOutputFrames > _bufferBalance)
			{
				target = Mathf.CeilToInt(_targetFrameRate - 1);
			}

			if (!DeckLinkSettings.Instance._multiOutput)
			{
				Time.captureFramerate = Application.targetFrameRate = target;
			}
			else
			{
				_outputFrameRate = target;
			}
		}

		protected override void Process()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			if (_device == null)
            {
                return;
            }

			if (_conversionMaterial != null)
			{
				//in this case, since we are dealing with non-srgb texture, need to do conversion from gamma to linear
				if (QualitySettings.activeColorSpace == ColorSpace.Linear && !_bypassGamma)
				{
					_conversionMaterial.EnableKeyword("APPLY_GAMMA");
				}
				else
				{
					_conversionMaterial.DisableKeyword("APPLY_GAMMA");
				}
			}

			if (_convertedTexture != null && _convertedPointer == IntPtr.Zero)
			{
				_convertedPointer = _convertedTexture.GetNativeTexturePtr();
				DeckLinkPlugin.SetOutputTexturePointer(_deviceIndex, _convertedPointer);
			}

            if (_device.IsStreamingOutput)
            {
				CaptureFrame();

				if (CanOutputFrame())
				{
					if (DeckLinkSettings.Instance._multiOutput)
					{
						BlendCapturedFrames();
					}

					RenderTexture input = _blended;

					input = Interlace(input);
					Convert(input);
					AdjustPlaybackFramerate();

					DeckLinkPlugin.SetDeviceOutputReady(_deviceIndex);
				}

			}
			ProcessAudio();
#endif
        }

        protected override bool IsInput()
        {
            return false;
        }

        public override void OnDestroy()
        {
            refCount--;

            if(refCount == 0)
            {
                QualitySettings.vSyncCount = prevRefCount;
            }

			if (_convertedCompBuffer != null)
			{
				_convertedCompBuffer.Release();
				_convertedCompBuffer = null;
			}

			if (_parameters != null)
			{
				_parameters.Release();
				_parameters = null;
			}

			base.OnDestroy();
        }

        

#if UNITY_EDITOR
        [ContextMenu("Save Output PNG")]
        protected override void SavePNG()
        {
            if (_inputTexture != null)
            {
                SavePNG("Image-Output.png", _inputTexture);
            }
        }
#endif

    }
}
