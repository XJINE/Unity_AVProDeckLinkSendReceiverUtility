using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
    [AddComponentMenu("AVPro DeckLink/IMGUI Display")]
    public class IMGUIDisplay : MonoBehaviour
    {
        public DeckLinkInput _inputDecklink;

        public ScaleMode _scaleMode = ScaleMode.ScaleToFit;
        public Color _color = Color.white;
        public int _depth = 0;

        public bool _fullScreen = true;
        public float _x = 0.0f;
        public float _y = 0.0f;
        public float _width = 1.0f;
        public float _height = 1.0f;

        public Texture2D _defaultTexture = null;

		private Material _imguiMat = null;

        //-------------------------------------------------------------------------

        public void OnGUI()
        {
            if (_inputDecklink == null)
                return;

            _x = Mathf.Clamp01(_x);
            _y = Mathf.Clamp01(_y);
            _width = Mathf.Clamp01(_width);
            _height = Mathf.Clamp01(_height);

            Texture texture = _inputDecklink.OutputTexture == null ? _defaultTexture : _inputDecklink.OutputTexture;

            if (texture != null)
            {
                GUI.depth = _depth;

                Rect rect;
                if (_fullScreen)
                    rect = new Rect(0.0f, 0.0f, Screen.width, Screen.height);
                else
                    rect = new Rect(_x * (Screen.width - 1), _y * (Screen.height - 1), _width * Screen.width, _height * Screen.height);

                GUI.color = _color;

				if (Event.current.type == EventType.Repaint)
				{
					_imguiMat.SetFloat("_width", texture.width);
					_imguiMat.SetFloat("_height", texture.height);
					_imguiMat.SetFloat("_rectWidth", rect.width);
					_imguiMat.SetFloat("_rectHeight", rect.height);

					_imguiMat.DisableKeyword("SCALE_TO_FIT");
					_imguiMat.DisableKeyword("SCALE_AND_CROP");
					_imguiMat.DisableKeyword("STRETCH_TO_FILL");

					if (_scaleMode == ScaleMode.ScaleToFit)
					{
						_imguiMat.EnableKeyword("SCALE_TO_FIT");
					}
					else if(_scaleMode == ScaleMode.ScaleAndCrop)
					{
						_imguiMat.EnableKeyword("SCALE_AND_CROP");
					}
					else
					{
						_imguiMat.EnableKeyword("STRETCH_TO_FILL");
					}

					Graphics.DrawTexture(rect, texture, _imguiMat);
				}
            }
        }

		void Start()
		{
			_imguiMat = new Material(Shader.Find("AVProDeckLink/IMGUIDisplay"));
			if(QualitySettings.activeColorSpace == ColorSpace.Linear)
			{
				_imguiMat.EnableKeyword("APPLY_GAMMA");
			}
			else
			{
				_imguiMat.DisableKeyword("APPLY_GAMMA");
			}
		}

		void OnDestroy()
		{
			if (_imguiMat)
			{
				Destroy(_imguiMat);
			}
		}
    }
}