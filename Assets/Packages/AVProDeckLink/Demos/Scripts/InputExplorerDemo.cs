using UnityEngine;
using System.Collections.Generic;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2014-2016 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink.Demos
{
    public class InputExplorerDemo : MonoBehaviour
    {
        public GUISkin _guiSkin;
        public bool _autoDetect;

        private List<Vector2> _scrollPos = new List<Vector2>();
        private Vector2 _horizScrollPos = Vector2.zero;

        private Texture _zoomed = null;
        private const float ZoomTime = 0.25f;
        private float _zoomTimer;
        private bool _zoomUp;
        private Rect _zoomSrcDest;
        private List<DeckLinkInput> _inputDecklinks;

        public void Start()
        {
            _inputDecklinks = new List<DeckLinkInput>();
            ///if()
            Application.runInBackground = true;
         
            EnumerateDevices();
        }

        private void EnumerateDevices()
        {
            foreach(var input in _inputDecklinks)
            {
                Destroy(input.gameObject);
            }
            _inputDecklinks.Clear();

            // Enumerate all devices
            int numDevices = DeckLink.GetNumDevices();
            print("num devices: " + numDevices);
            for (int i = 0; i < numDevices; i++)
            {
				Device device = DeckLink.GetDevice(i);
				if(device.NumInputModes == 0)
				{
					continue;
				}

                GameObject decklinkObject = new GameObject();
                DeckLinkInput input = decklinkObject.AddComponent<DeckLinkInput>();
                input.DeviceIndex = i;
                input.ModeIndex = 0;
                input._playOnStart = true;
                input._autoDeinterlace = true;
                input._autoDetectMode = _autoDetect;
                input.Begin();
                _inputDecklinks.Add(input);

                // Enumerate input modes
                print("device " + i + ": " + input.Device.Name + " has " + input.Device.NumInputModes + " input modes");
                for (int j = 0; j < input.Device.NumInputModes; j++)
                {
                    DeviceMode mode = input.Device.GetInputMode(j);
                    print("  mode " + j + ": " + mode.Width + "x" + mode.Height + " @" + mode.FrameRate.ToString("F2") + "fps [" + mode.PixelFormatDescription + "] idx:" + mode.Index);
                }

                _scrollPos.Add(new Vector2(0, 0));
            }
        }

        public void Update()
        {
            // Handle mouse click to unzoom
            if (_zoomed != null)
            {
                if (_zoomUp)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        _zoomTimer = ZoomTime;
                        _zoomUp = false;
                    }
                    else
                    {
                        _zoomTimer += Time.deltaTime;
                    }

                }
                else
                {
                    if (_zoomTimer <= 0.0f)
                    {
                        _zoomed = null;
                    }
                    _zoomTimer -= Time.deltaTime;
                }
            }
        }

        public void NewDeviceAdded()
        {
            EnumerateDevices();
        }

        public void OnGUI()
        {
            GUI.skin = _guiSkin;

            _horizScrollPos = GUILayout.BeginScrollView(_horizScrollPos, false, false);
            GUILayout.BeginHorizontal();

            for (int i = 0; i < _inputDecklinks.Count; i++)
            {
                GUILayout.BeginVertical("box", GUILayout.MaxWidth(375));

                // Image preview
                Rect cameraRect = GUILayoutUtility.GetRect(375, 200);
                if (GUI.Button(cameraRect, _inputDecklinks[i].OutputTexture))
                {
                    if (_zoomed == null)
                    {
                        _zoomed = _inputDecklinks[i].OutputTexture;
                        _zoomSrcDest = cameraRect;
                        _zoomUp = true;
                    }
                }

                // Controls
                GUILayout.Box("Device " + i + ": " + _inputDecklinks[i].Device.Name);
                if (!_inputDecklinks[i].Device.IsStreaming)
                {
                    GUILayout.Box("Stopped");
                }
                else
                {
                    GUILayout.Box(string.Format("{0} [{1}]", _inputDecklinks[i].Device.CurrentMode.ModeDescription, _inputDecklinks[i].Device.CurrentMode.PixelFormatDescription));
					GUILayout.BeginHorizontal();
					if (_inputDecklinks[i].FlipX)
					{
						GUI.color = Color.green;
					}

					if (GUILayout.Button("Flip X"))
					{
						_inputDecklinks[i].FlipX = !_inputDecklinks[i].FlipX;
					}

					GUI.color = Color.white;

					if (_inputDecklinks[i].FlipY)
					{
						GUI.color = Color.green;
					}

					if (GUILayout.Button("Flip Y"))
					{
						_inputDecklinks[i].FlipY = !_inputDecklinks[i].FlipY;
					}

					GUI.color = Color.white;

					GUILayout.EndHorizontal();
					if (!DeckLinkPlugin.IsNoInputSignal(_inputDecklinks[i].Device.DeviceIndex))
                    {
                        GUILayout.Box(string.Format("Capture {0}hz Display {1}hz", _inputDecklinks[i].Device.CurrentMode.FrameRate.ToString("F2"), 
                            _inputDecklinks[i].Device.FPS.ToString("F2")));
                    }
                    else
                    {
                        GUILayout.Box("No Signal");
                    }
                    if (GUILayout.Button("Stop"))
                    {
                        if (_zoomed == null)
                        {
                            _inputDecklinks[i].Device.StopInput();
                        }
                    }
                }


                if (_inputDecklinks[i].Device.AutoDeinterlace != GUILayout.Toggle(_inputDecklinks[i].Device.AutoDeinterlace, "Auto Deinterlace", GUILayout.ExpandWidth(true)))
                {
                    _inputDecklinks[i].Device.AutoDeinterlace = !_inputDecklinks[i].Device.AutoDeinterlace;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("Select a mode:");
                GUILayout.EndHorizontal();

                _scrollPos[i] = GUILayout.BeginScrollView(_scrollPos[i], false, false);
                for (int j = 0; j < _inputDecklinks[i].Device.NumInputModes; j++)
                {
                    DeviceMode mode = _inputDecklinks[i].Device.GetInputMode(j);

                    GUI.color = Color.white;
                    if (_inputDecklinks[i].Device.IsStreaming && _inputDecklinks[i].Device.CurrentMode == mode)
                    {
                        GUI.color = Color.green;
                    }

                    if (GUILayout.Button(j + "/ " + mode.ModeDescription + " [" + mode.PixelFormatDescription + "]"))
                    {
                        if (_zoomed == null)
                        {
                            // Start selected device
                            _inputDecklinks[i]._modeIndex = j;
                            _inputDecklinks[i].Begin();
                        }
                    }
                }
                GUILayout.EndScrollView();


                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            // Show zoomed camera image
            if (_zoomed != null)
            {
                Rect fullScreenRect = new Rect(0, 0, Screen.width, Screen.height);

                float t = Mathf.Clamp01(_zoomTimer / ZoomTime);
                t = Mathf.SmoothStep(0, 1, t);
                Rect r = new Rect();
                r.x = Mathf.Lerp(_zoomSrcDest.x, fullScreenRect.x, t);
                r.y = Mathf.Lerp(_zoomSrcDest.y, fullScreenRect.y, t);
                r.width = Mathf.Lerp(_zoomSrcDest.width, fullScreenRect.width, t);
                r.height = Mathf.Lerp(_zoomSrcDest.height, fullScreenRect.height, t);
                GUI.DrawTexture(r, _zoomed, ScaleMode.ScaleToFit, false);
            }
        }
    }
}