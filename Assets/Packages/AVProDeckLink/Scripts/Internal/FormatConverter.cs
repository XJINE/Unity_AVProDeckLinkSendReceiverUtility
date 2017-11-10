#if UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5 || UNITY_5_4_OR_NEWER
	#define AVPRODECKLINK_UNITYFEATURE_NONPOW2TEXTURES
#endif

using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
    [System.Serializable]
    public class FormatConverter : System.IDisposable
    {
        private int _deviceHandle;

        // Format conversion and texture output
        private Texture2D _rawTexture;
        private RenderTexture _finalTexture;
        private Material _conversionMaterial;
        private Material _deinterlaceMaterial;
        private int _conversionMaterialPass;
        private int _usedTextureWidth, _usedTextureHeight;
        private bool _requiresTextureCrop;
        private Vector4 _uv;

        // Conversion params
        private DeviceMode _mode;
        private bool _autoDeinterlace;
        private bool _deinterlace;
        private int _lastFrameUploaded = -1;
        private bool _isBuilt;

		private bool _flipX = false;
		private bool _flipY = false;
		private bool _uvsDirty = false;

		private bool _bypassGammaCorrection = false;

        public Texture OutputTexture
        {
            get {
				return _finalTexture;
            }
        }

		public bool BypassGammaCorrection
		{
			get { return _bypassGammaCorrection; }
			set { _bypassGammaCorrection = value; }
		}

		public bool FlipX
		{
			get { return _flipX; }
			set { _flipX = value; _uvsDirty = true; }
		}

		public bool FlipY
		{
			get { return _flipY; }
			set { _flipY = value; _uvsDirty = true; }
		}

        public int OutputFrameNumber
        {
            get { return _lastFrameUploaded; }
        }

        public bool ValidPicture { get; private set; }
        public bool AutoDeinterlace
        {
            get { return _autoDeinterlace; }
            set { _autoDeinterlace = value; if (_mode != null) _deinterlace = (AutoDeinterlace && _mode.InterlacedFieldMode); }
        }

        public void Reset()
        {
            ValidPicture = false;
            _lastFrameUploaded = -1;
        }

        public bool Build(int deviceHandle, DeviceMode mode, bool delayResourceCreationUntilFramesStart = false)
        {
            bool result = true;
            Reset();

            _deviceHandle = deviceHandle;
            _mode = mode;
            _deinterlace = (AutoDeinterlace && _mode.InterlacedFieldMode);

            _isBuilt = false;
            if (!delayResourceCreationUntilFramesStart)
            {
                result = Build();
            }
            return result;
        }

        private bool Build()
        {
            if (CreateMaterial())
            {
#if AVPRODECKLINK_UNITYFEATURE_EXTERNALTEXTURES
			System.IntPtr texPtr = AVProDeckLinkPlugin.GetTexturePointer(_deviceHandle);
			if (texPtr != System.IntPtr.Zero)
			{
				_usedTextureWidth = _mode.Pitch / 4;
				_usedTextureHeight = _mode.Height;				
				_rawTexture = Texture2D.CreateExternalTexture(_mode.Width/2, _mode.Height, TextureFormat.ARGB32, false, false, AVProDeckLinkPlugin.GetTexturePointer(_deviceHandle));
			}
#else
                CreateRawTexture();
#endif
                if (_rawTexture != null)
                {
                    CreateFinalTexture();

                    _requiresTextureCrop = (_usedTextureWidth != _rawTexture.width || _usedTextureHeight != _rawTexture.height);

                    if (_requiresTextureCrop)
                    {
                        CreateUVs(_flipX || false, !_flipY && true);
                    }
                    else
                    {
                        Flip(_flipX || false, !_flipY && true);
                    }

                    _conversionMaterial.SetFloat("_TextureWidth", _mode.Width);
                    _conversionMaterial.mainTexture = _rawTexture;
#if !AVPRODECKLINK_UNITYFEATURE_EXTERNALTEXTURES
                    DeckLinkPlugin.SetTexturePointer(_deviceHandle, _rawTexture.GetNativeTexturePtr());
#endif
                }
            }

            _isBuilt = (_conversionMaterial != null && _rawTexture != null && _finalTexture != null);
            return _isBuilt;
        }

        private void Flip(bool flipX, bool flipY)
        {
            Vector2 scale = new Vector2(1f, 1f);
            Vector2 offset = new Vector2(0f, 0f);
            if (flipX)
            {
                scale = new Vector2(-1f, scale.y);
                offset = new Vector2(1f, offset.y);
            }
            if (flipY)
            {
                scale = new Vector2(scale.x, -1f);
                offset = new Vector2(offset.x, 1f);
            }

            _conversionMaterial.mainTextureScale = scale;
            _conversionMaterial.mainTextureOffset = offset;

#if (UNITY_5 && !UNITY_5_1 && !UNITY_5_2) || UNITY_5_4_OR_NEWER
            _conversionMaterial.SetVector("_MainTex_ST2", new Vector4(scale.x, scale.y, offset.x, offset.y));
#endif
        }

        public bool Update()
        {
            bool result = false;
            //RenderTexture prev = RenderTexture.active;

            if (!_isBuilt)
            {
                if (DeckLinkPlugin.GetLastCapturedFrameTime(_deviceHandle) > 0)
                {
                    if (!Build())
                        return false;
                }
                return false;
            }
			/*
			#if UNITY_5_3_OR_NEWER
						Flip();
			#endif
			*/
			if (_uvsDirty)
			{
				_requiresTextureCrop = (_usedTextureWidth != _rawTexture.width || _usedTextureHeight != _rawTexture.height);

				if (_requiresTextureCrop)
				{
					CreateUVs(_flipX || false, !_flipY && true);
				}
				else
				{
					Flip(_flipX || false, !_flipY && true);
				}

				_uvsDirty = false;
			}

            // Wait until next frame has been uploaded to the texture
            int lastFrameUploaded = (int)DeckLinkPlugin.GetLastFrameUploaded(_deviceHandle);
            if (_lastFrameUploaded != lastFrameUploaded)
            {
                RenderTexture prev = RenderTexture.active;

                if (!_deinterlace)
                {
                    // Format convert
                    if (DoFormatConversion(_finalTexture))
                    {
                        result = true;
                    }
                }
                else
                {
                    // Format convert and Deinterlace
                    RenderTexture tempTarget = RenderTexture.GetTemporary(_finalTexture.width, _finalTexture.height, 0, _finalTexture.format, RenderTextureReadWrite.Default);
                    tempTarget.filterMode = FilterMode.Point;
                    tempTarget.wrapMode = TextureWrapMode.Clamp;
                    if (DoFormatConversion(tempTarget))
                    {
                        DoDeinterlace(tempTarget, _finalTexture);
                        
                        result = true;
                    }
                    RenderTexture.ReleaseTemporary(tempTarget);
                }

                RenderTexture.active = prev;

                _lastFrameUploaded = lastFrameUploaded;
            }
            else if (!_finalTexture.IsCreated())
            {
                Debug.LogError("GPU Reset");
                // If the texture has been lost due to GPU reset(from full screen mode change or vsync change) we'll need fill the texture again
                Reset();
            }

            return ValidPicture && result;
        }

        public void Dispose()
        {
            _mode = null;
            ValidPicture = false;

            if (_conversionMaterial != null)
            {
                _conversionMaterial.mainTexture = null;
                Material.Destroy(_conversionMaterial);
                _conversionMaterial = null;
            }

            if (_deinterlaceMaterial != null)
            {
                _deinterlaceMaterial.mainTexture = null;
                Material.Destroy(_deinterlaceMaterial);
                _deinterlaceMaterial = null;
            }

            if (_finalTexture != null)
            {
                RenderTexture.ReleaseTemporary(_finalTexture);
                _finalTexture = null;
            }

#if AVPRODECKLINK_UNITYFEATURE_EXTERNALTEXTURES
		_rawTexture = null;
#else
            if (_rawTexture != null)
            {
                Texture2D.Destroy(_rawTexture);
                DeckLinkPlugin.SetTexturePointer(_deviceHandle, System.IntPtr.Zero);
                if(_conversionMaterial != null)
                {
                    _conversionMaterial.mainTexture = null;
                }
                _rawTexture = null;
            }
#endif
        }

        private bool CreateMaterial()
        {
            Shader shader = null;
            int pass = 0;
            if (DeckLinkManager.Instance.GetPixelConversionShader(_mode.PixelFormat, ref shader, ref pass))
            {
                if (_conversionMaterial != null)
                {
                    if (_conversionMaterial.shader != shader)
                    {
                        Material.Destroy(_conversionMaterial);
                        _conversionMaterial = null;
                    }
                }

                if (_conversionMaterial == null)
                {
                    _conversionMaterial = new Material(shader);
                    _conversionMaterial.name = "AVProDeckLink-Material";
				}

                _conversionMaterialPass = pass;
            }

            if (true)
            {
                shader = DeckLinkManager.Instance.GetDeinterlaceShader();
                if (shader)
                {
                    if (_deinterlaceMaterial != null)
                    {
                        if (_deinterlaceMaterial.shader != shader)
                        {
                            Material.Destroy(_deinterlaceMaterial);
                            _deinterlaceMaterial = null;
                        }
                    }

                    if (_deinterlaceMaterial == null)
                    {
                        _deinterlaceMaterial = new Material(shader);
                        _deinterlaceMaterial.name = "AVProDeckLink-DeinterlaceMaterial";
                    }
                }
            }

            _deinterlaceMaterial.SetFloat("_TextureHeight", _mode.Height);

			return (_conversionMaterial != null) && (_deinterlaceMaterial != null);
        }

        private void CreateRawTexture()
        {
            _usedTextureWidth = _mode.Pitch / 4;
            _usedTextureHeight = _mode.Height;

            // HACK for 10-bit..
            if (_mode.PixelFormat == DeckLinkPlugin.PixelFormat.YCbCr_10bpp_422)
            {
                _usedTextureWidth = ((_mode.Width * 16) / 6) / 4;
            }

            if (_mode.PixelFormat == DeckLinkPlugin.PixelFormat.RGB_10bpp_444)
            {

            }

            // We use a power-of-2 texture as Unity makes these internally anyway and not doing it seems to break things for texture updates
            int textureWidth = _usedTextureWidth;
            int textureHeight = _usedTextureHeight;

            bool requiresPOT = true;
#if AVPRODECKLINK_UNITYFEATURE_NONPOW2TEXTURES
            requiresPOT = (SystemInfo.npotSupport == NPOTSupport.None);
#endif
            // If the texture isn't a power of 2
            if (requiresPOT)
            {
                if (!Mathf.IsPowerOfTwo(textureWidth) || !Mathf.IsPowerOfTwo(textureHeight))
                {
                    textureWidth = Mathf.NextPowerOfTwo(textureWidth);
                    textureHeight = Mathf.NextPowerOfTwo(textureHeight);
                }
                Debug.Log("[AVProDeckLink] using texture: " + textureWidth + "x" + textureHeight);
            }

            // Create texture that stores the initial raw frame
            // If there is already a texture, only destroy it if it's too small
            if (_rawTexture != null)
            {
                if (_rawTexture.width != textureWidth ||
                    _rawTexture.height != textureHeight)
                {
                    Texture2D.Destroy(_rawTexture);
                    DeckLinkPlugin.SetTexturePointer(_deviceHandle, System.IntPtr.Zero);
                    if (_conversionMaterial != null)
                    {
                        _conversionMaterial.mainTexture = null;
                    }
                    _rawTexture = null;
                }
            }

            if (_rawTexture == null)    
            {
                _rawTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false, true);
                _rawTexture.wrapMode = TextureWrapMode.Clamp;
                _rawTexture.filterMode = FilterMode.Point;
                _rawTexture.name = "AVProDeckLink-RawTexture";
                //_rawTexture.Apply(false, true);
                DeckLinkPlugin.SetTexturePointer(_deviceHandle, _rawTexture.GetNativeTexturePtr());
            }
        }

        public static bool InputFormatSupported(DeckLinkPlugin.PixelFormat format)
        {
            switch (format)
            {
                case DeckLinkPlugin.PixelFormat.YCbCr_10bpp_422:
                case DeckLinkPlugin.PixelFormat.ARGB_8bpp_444:
                case DeckLinkPlugin.PixelFormat.YCbCr_8bpp_422:
                case DeckLinkPlugin.PixelFormat.BGRA_8bpp_444:
                    return true;
                default:
                    return false;
            }
        }

        private void CreateFinalTexture()
        {
            // For 10-bit formats we render to higher precision buffers
            // TODO: make this optional?
            RenderTextureFormat format = RenderTextureFormat.ARGB32;
            switch (_mode.PixelFormat)
            {
                case DeckLinkPlugin.PixelFormat.YCbCr_10bpp_422:
                case DeckLinkPlugin.PixelFormat.RGB_10bpp_444:
                case DeckLinkPlugin.PixelFormat.RGBX_10bpp_444:
                case DeckLinkPlugin.PixelFormat.RGBX_10bpp_444_LE:
                    format = RenderTextureFormat.ARGBHalf;
                    break;
            }

            // Create RenderTexture for post transformed frames
            // If there is already a renderTexture, only destroy it doesn't match the desired size
            if (_finalTexture != null)
            {
                if (_finalTexture.width != _mode.Width ||
                    _finalTexture.height != _mode.Height ||
                    _finalTexture.format != format)
                {
                    RenderTexture.ReleaseTemporary(_finalTexture);
                    _finalTexture = null;
                }
            }

            if (_finalTexture == null)
            {
                _finalTexture = RenderTexture.GetTemporary(_mode.Width, _mode.Height, 0, format, RenderTextureReadWrite.Default);
                _finalTexture.wrapMode = TextureWrapMode.Clamp;
                _finalTexture.filterMode = FilterMode.Trilinear;
                _finalTexture.name = "AVProDeckLink-FinalTexture";
				_finalTexture.useMipMap = true;
#if UNITY_5_5_OR_NEWER
				_finalTexture.autoGenerateMips = true;
#endif
				_finalTexture.Create();
            }
        }

        private void CreateUVs(bool invertX, bool invertY)
        {
            float x1, x2;
            float y1, y2;
            if (invertX)
            {
                x1 = 1.0f; x2 = 0.0f;
            }
            else
            {
                x1 = 0.0f; x2 = 1.0f;
            }
            if (invertY)
            {
                y1 = 1.0f; y2 = 0.0f;
            }
            else
            {
                y1 = 0.0f; y2 = 1.0f;
            }

            // Alter UVs if we're only using a portion of the texture
            if (_usedTextureWidth != _rawTexture.width)
            {
                float xd = _usedTextureWidth / (float)_rawTexture.width;
                x1 *= xd; x2 *= xd;
            }
            if (_usedTextureHeight != _rawTexture.height)
            {
                float yd = _usedTextureHeight / (float)_rawTexture.height;
                y1 *= yd; y2 *= yd;
            }

            _uv = new Vector4(x1, y1, x2, y2);
        }

        private bool DoFormatConversion(RenderTexture target)
        {
            if (target == null)
                return false;

			if(_conversionMaterial != null)
			{
				if (QualitySettings.activeColorSpace == ColorSpace.Linear && !_bypassGammaCorrection)
				{
					_conversionMaterial.EnableKeyword("APPLY_LINEAR");
				}
				else
				{
					_conversionMaterial.DisableKeyword("APPLY_LINEAR");
				}
			}

			target.DiscardContents();

            if (!_requiresTextureCrop)
            {
                Graphics.Blit(_rawTexture, target, _conversionMaterial, _conversionMaterialPass);
			}
            else
            {
                RenderTexture.active = target;
                _conversionMaterial.SetPass(_conversionMaterialPass);

                GL.PushMatrix();
                GL.LoadOrtho();
				DrawQuad(_uv);
                GL.PopMatrix();
			}
			ValidPicture = true;

            return true;
        }

        private void DoDeinterlace(RenderTexture source, RenderTexture target)
        {
            target.DiscardContents();

            int passIndex = 0;
            switch (DeckLinkManager.Instance._deinterlaceMethod)
            {
                case DeckLinkManager.DeinterlaceMethod.None:
                    passIndex = 0;
                    break;
                case DeckLinkManager.DeinterlaceMethod.Blend:
                    passIndex = 1;
                    break;
                case DeckLinkManager.DeinterlaceMethod.Discard:
                    passIndex = 2;
                    break;
                case DeckLinkManager.DeinterlaceMethod.DiscardSmooth:
                    passIndex = 3;
                    break;
            }

            Graphics.Blit(source, target, _deinterlaceMaterial, passIndex);
        }

        private static void DrawFullscreenTriangle(Vector4 uv)
        {
            // TODO: use triangle for simplicity
        }

        private static void DrawQuad(Vector4 uv)
        {
            GL.Begin(GL.QUADS);

            GL.TexCoord2(uv.x, uv.y);
            GL.Vertex3(0.0f, 0.0f, 0.1f);

            GL.TexCoord2(uv.z, uv.y);
            GL.Vertex3(1.0f, 0.0f, 0.1f);

            GL.TexCoord2(uv.z, uv.w);
            GL.Vertex3(1.0f, 1.0f, 0.1f);

            GL.TexCoord2(uv.x, uv.w);
            GL.Vertex3(0.0f, 1.0f, 0.1f);

            GL.End();
        }
    }
}
