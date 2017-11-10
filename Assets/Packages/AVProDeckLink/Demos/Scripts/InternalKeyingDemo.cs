using UnityEngine;
using System.Collections.Generic;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink.Demos
{
    public class InternalKeyingDemo : MonoBehaviour
    {
        public GUISkin _guiSkin;
        public DeckLinkOutput _decklink;

        private Vector2 _deviceScrollPos = Vector2.zero;
        private Vector2 _modeScrollPos = Vector2.zero;
        private Device _selectedDevice;
        private DeviceMode _activeMode;

        private GUIStyle _modeListStyle;

        void Update()
        {
            if(_decklink.Device != null && _decklink.Device.CurrentMode != null && _activeMode == null)
            {
                _activeMode = _decklink.Device.CurrentMode;
            }
        }

        private void StartOutput(DeviceMode mode)
        {
            StopOutput();
            _decklink._modeIndex = mode.Index;
            _decklink.Begin();
            _decklink.SetCamera(Camera.main);
            _activeMode = mode;
        }

        private void StopOutput()
        {
            if (Camera.main != null)
                Camera.main.targetTexture = null;

            _decklink.StopOutput();
            _activeMode = null;
        }

        public void OnGUI()
        {
            GUI.skin = _guiSkin;

            if (_modeListStyle == null)
            {
                _modeListStyle = GUI.skin.GetStyle("ModeList");
            }


            GUILayout.BeginHorizontal();
            // List the devices
            _deviceScrollPos = GUILayout.BeginScrollView(_deviceScrollPos, false, false);

            GUILayout.BeginVertical("box", GUILayout.MaxWidth(300));
            if (GUILayout.Button("Select Device:"))
            {
                _selectedDevice = null;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box", GUILayout.MaxWidth(300));
            for (int i = 0; i < DeckLink.GetNumDevices(); i++)
            {

                Device device = DeckLink.GetDevice(i);

                if (device == _selectedDevice)
                {
                    GUI.color = Color.blue;
                }
                if (_activeMode != null && device == _selectedDevice && device.IsStreamingOutput)
                {
                    GUI.color = Color.green;
                }


                if (GUILayout.Button(device.Name + " " + device.ModelName, _modeListStyle))
                {
                    _selectedDevice = device;
                }
                GUI.color = Color.white;

            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            // For the selected device, list the modes available
            if (_selectedDevice != null)
            {
                GUILayout.BeginVertical();
                GUILayout.BeginVertical("box", GUILayout.MaxWidth(300));
                if (GUILayout.Button("Select Mode:"))
                {
                    _selectedDevice = null;
                }
                GUILayout.EndVertical();

                if (_selectedDevice != null)
                {
                    GUILayout.BeginVertical("box", GUILayout.MaxWidth(600));
                    _modeScrollPos = GUILayout.BeginScrollView(_modeScrollPos, false, false);
                    for (int j = 0; j < _selectedDevice.NumOutputModes; j++)
                    {
                        DeviceMode mode = _selectedDevice.GetOutputMode(j);

                        if (mode == _activeMode)
                        {

                            if (_selectedDevice.IsStreamingOutput)
                            {
                                GUI.color = Color.green;
                            }
                            else
                            {
                                GUI.color = Color.blue;
                            }
                        }
                        //GUILayout.BeginVertical("box", GUILayout.MaxWidth(300));
                        if (GUILayout.Button("" + j.ToString("D2") + ") " + mode.ModeDescription + " - " + mode.PixelFormatDescription + " - " + mode.Width + "x" + mode.Height, _modeListStyle))
                        {
                            _decklink.StopOutput();
                            _decklink.DeviceIndex = _selectedDevice.DeviceIndex;
                            _decklink.ModeIndex = mode.Index;
                            _decklink.Begin();

                            _activeMode = mode;
                        }

                        GUI.color = Color.white;
                    }
                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                }
                GUILayout.EndVertical();
            }

            if (_decklink.Device != null && _decklink.Device.IsStreamingOutput)
            {
                GUILayout.BeginVertical("box");
                int numDecklinkBufferedFrames = DeckLinkPlugin.GetOutputBufferedFramesCount(_decklink.Device.DeviceIndex);
                int numWaitingOutputFrames = DeckLinkPlugin.GetWaitingOutputBufferCount(_decklink.Device.DeviceIndex);
                int numFreeOutputFrames = DeckLinkPlugin.GetFreeOutputBufferCount(_decklink.Device.DeviceIndex);

                GUILayout.Space(20f);
                GUILayout.Label(string.Format("VSync {0}", QualitySettings.vSyncCount));
                GUILayout.Label(string.Format("{0:F3} {1}", _decklink.TargetFramerate, _decklink.OutputFramerate));
                GUILayout.Label(string.Format("Buffers: DeckLink {0:D2} << Full {1:D2} << Empty {2:D2}", numDecklinkBufferedFrames, numWaitingOutputFrames, numFreeOutputFrames));
                GUILayout.Label("Screen: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height + " " + Screen.currentResolution.refreshRate);
                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
        }


        void Start()
        {
            Application.runInBackground = true;

            EnumerateDevices();

            if (QualitySettings.vSyncCount > 0)
            {
                Debug.LogWarning("[AVProDeckLink] VSync is enabled.  This could result in stuttering during DeckLink output");
            }

            _decklink.Start();

            if(_decklink.Device != null)
            {
                _activeMode = _decklink.Device.CurrentOutputMode;
            }
            
        }

        void OnDestroy()
        {
            StopOutput();
        }

        private void EnumerateDevices()
        {
            // Enumerate all devices
            int numDevices = DeckLink.GetNumDevices();
            print("num devices: " + numDevices);
            for (int i = 0; i < numDevices; i++)
            {
                Device device = DeckLink.GetDevice(i);

                // Enumerate output modes
                print("device " + i + ": " + device.Name + " has " + device.NumOutputModes + " output modes");
                for (int j = 0; j < device.NumOutputModes; j++)
                {
                    DeviceMode mode = device.GetOutputMode(j);

					print("  mode " + j + ": " + mode.Width + "x" + mode.Height + " @" + mode.FrameRate.ToString("F2") + "fps [" + mode.PixelFormatDescription + "] idx:" + mode.Index + " " + (mode.InterlacedFieldMode ? "Interlaced" : "Progressive"));
				}
			}
        }
    }
}
