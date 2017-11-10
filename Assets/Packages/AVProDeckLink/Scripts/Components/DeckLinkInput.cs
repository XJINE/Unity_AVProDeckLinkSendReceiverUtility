using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
    [AddComponentMenu("AVPro DeckLink/DeckLinkInput")]
    public class DeckLinkInput : DeckLink
    {
        //video settings

        public bool _autoDeinterlace = true;

        public bool _autoDetectMode = true;

		[HideInInspector]
		[SerializeField]
		private bool _flipX = false;

		[HideInInspector]
		[SerializeField]
		private bool _flipY = false;

		//audio settings
        [Range(0f, 10f)]
        public float _audioVolume = 1f;


        /*[Range(2, 8)]
        public uint _audioChannels = 2;*/
        private uint _audioChannels = 2;

		public bool _muteAudio = false;

		public bool _bypassGamma = false;

		private AudioSource _audioSource;

		public ulong LastFrameTimestamp
		{
			get { return _device == null ? 0 : _device.OutputFrameNumber; }
		}

		public bool FlipX
		{
			get { return _flipX; }
			set
			{
				_flipX = value;
				if(_device != null)
				{
					_device.FlipInputX = _flipX;
				}
			}
		}

		public bool FlipY
		{
			get { return _flipY; }
			set
			{
				_flipY = value;
				if (_device != null)
				{
					_device.FlipInputY = _flipY;
				}
			}
		}

		public AudioSource InputAudioSource
        {
            get { return _audioSource; }
        }

        public Texture OutputTexture
        {

            get
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                if (_device != null && _device.OutputTexture != null && _device.ReceivedSignal)
                    return _device.OutputTexture;
#endif
				return null;

            }
        }

        protected override void Process()
        {
			if (_device != null)
			{
				if(_device.BypassGammaCorrection != _bypassGamma)
				{
					_device.BypassGammaCorrection = _bypassGamma;
				}
			}
		}

        protected override bool IsInput()
        {
            return true;
        }

        public override void Awake()
        {
            base.Awake();
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.hideFlags = HideFlags.HideInInspector;
            _audioSource.loop = true;
        }

        protected override void BeginDevice()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            DeckLinkPlugin.SetAutoDetectEnabled(_deviceIndex, _autoDetectMode);

            _device.AutoDeinterlace = _autoDeinterlace;
            int actualAudioChannels;

            if(_audioChannels <= 2)
            {
                actualAudioChannels = 2;
            }
            else
            {
                actualAudioChannels = 8;
            }

            int maxSupportedChannels = DeckLinkPlugin.GetMaxSupportedAudioChannels(_deviceIndex);
            if (actualAudioChannels > maxSupportedChannels)
            {
                actualAudioChannels = maxSupportedChannels;
            }

            if (!_device.StartInput(_modeIndex, actualAudioChannels))
            {
                _device.StopInput();
                _device = null;
            }
            if(_device != null)
            {
				_device.FlipInputX = _flipX;
				_device.FlipInputY = _flipY;

				DeviceMode mode = _device.GetInputMode(_modeIndex);
                _audioSource.clip = AudioClip.Create("DeckLink Input Audio", 48000 / (int)(mode.FrameRate + 0.5f), actualAudioChannels, 48000, false);
                _audioSource.Play();
            }
#endif
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
			if (!_muteAudio)
			{
				DeckLinkPlugin.GetAudioBuffer(_deviceIndex, data, data.Length, channels, _audioVolume);
			}
        }

        public bool StopInput()
        {
            _audioSource.Stop();
            AudioClip.Destroy(_audioSource.clip);
            _audioSource.clip = null;

            if (_device == null)
            {
                return false;
            }

            _device.StopInput();
            _device = null;

            return true;
        }

        public void Pause()
        {
            if (_device != null)
            {
                _device.Pause();
            }
        }

        public void Unpause()
        {
            if (_device != null)
            {
                _device.Unpause();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Save Input PNG")]
        protected override void SavePNG()
        {
            if (OutputTexture != null)
            {
                SavePNG("Image-Input.png", (RenderTexture)OutputTexture);
            }
        }
#endif
        protected override void Cleanup()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            DeckLinkPlugin.SetAutoDetectEnabled(_deviceIndex, false);

            _audioSource.Stop();

            if(_audioSource.clip != null)
            {
                AudioClip.Destroy(_audioSource.clip);
                _audioSource.clip = null;
            }
#endif
        }
    }
}
