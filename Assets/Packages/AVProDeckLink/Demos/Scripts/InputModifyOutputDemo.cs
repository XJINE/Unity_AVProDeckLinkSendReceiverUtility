using UnityEngine;
using System.Collections.Generic;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2014-2016 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink.Demos
{
    public class InputModifyOutputDemo : MonoBehaviour
    {
        public DeckLinkInput _inputDecklink;
        public DeckLinkOutput _outputDecklink;
        public Camera _renderCamera;
        public GUISkin _guiSkin;
        public bool _outputModeMatchInputMode;

        private Vector2 _inputModeScrollPos = Vector2.zero;
        private Vector2 _outputModeScrollPos = Vector2.zero;
        private Device _selectedInputDevice;
        private Device _selectedOutputDevice;
        private bool _needsInputAutoScroll;
        private bool _needsOutputAutoScroll;
        private DeviceMode _lastInputModeSet;

        private GUIStyle _modeListStyle;

        void Update()
        {
            if (_outputModeMatchInputMode)
            {
                if (_inputDecklink.Device != null && _inputDecklink.Device.IsStreaming && _inputDecklink.OutputTexture != null)
                {
                    AutoStartOutput(_inputDecklink.Device.CurrentMode);
                }
            }

            if (_inputDecklink.Device != null && _inputDecklink.Device.IsStreaming && _inputDecklink.OutputTexture != null)
            {
                if (_lastInputModeSet != _inputDecklink.Device.CurrentMode)
                {
                    _needsInputAutoScroll = true;
                }
            }

            if (_renderCamera.targetTexture == null && _outputDecklink.InputTexture != null)
            {
                _outputDecklink.SetCamera(_renderCamera);
            }

            //_outputDecklink.Process();
        }

        private void StartInput(DeviceMode mode)
        {
            StopInput();

            _inputDecklink.DeviceIndex = mode.Device.DeviceIndex;
            _inputDecklink.ModeIndex = mode.Index;
            _lastInputModeSet = mode;
            _inputDecklink.Begin();
        }

        private void AutoStartOutput(DeviceMode targetMode)
        {
            Device device = _outputDecklink.Device;
            if (device == null)
            {
                device = _selectedOutputDevice;
            }

            if (device != null)
            {
                DeviceMode mode = FindClosestOutputMode(device, targetMode);
                if (mode != null)
                {
                    if (_outputDecklink.Device == null || _outputDecklink.Device.CurrentOutputMode != mode)
                    {
                        StartOutput(mode);
                        _needsOutputAutoScroll = true;
                    }
                }
                else
                {
                    Debug.LogWarning("Couldn't find a matching output mode");
                }
            }
        }

        DeviceMode FindClosestOutputMode(Device device, DeviceMode targetMode)
        {
            DeviceMode result = null;

            List<DeviceMode> potentialModes = new List<DeviceMode>();
            for (int i = 0; i < device.NumOutputModes; i++)
            {
                DeviceMode mode = device.GetOutputMode(i);
                if (mode.Width == targetMode.Width &&
                    mode.Height == targetMode.Height &&
                    mode.FrameRate == targetMode.FrameRate &&
                    mode.PixelFormat == DeckLinkPlugin.PixelFormat.YCbCr_8bpp_422 &&
                    mode.FieldModeString == targetMode.FieldModeString)
                {
                    potentialModes.Add(mode);
                }
            }

            // If we have several matching modes try to pick the best one
            //for (int i = 0; i < potentialModes.Count; i++)
            //{
                // For now we have no criteria so just pick the first one
                result = potentialModes.Count > 0 ? potentialModes[0] : null;
                //break;
            //}

            return result;
        }


        private void StartOutput(DeviceMode mode)
        {
            if(_outputDecklink != null)
            {
                StopOutput();

                _outputDecklink.DeviceIndex = _selectedOutputDevice.DeviceIndex;
                _outputDecklink._modeIndex = mode.Index;
                _outputDecklink.Begin();

                if(_renderCamera != null)
                {
                    _outputDecklink.SetCamera(_renderCamera);
                }
                else
                {
                    Debug.LogWarning("Warning: Render Camera not assigned");
                }
                _outputDecklink.Begin();
            }
            else
            {
                Debug.LogWarning("Warning: Output DeckLink not assigned");
            }
        }

        private void StopInput()
        {
            if (_inputDecklink != null && _inputDecklink.Device != null)
            {
                _inputDecklink.StopInput();
            }
        }

        private void StopOutput()
        {
            if (_renderCamera != null)
            {
                _renderCamera.targetTexture = null;
            }

            if (_outputDecklink != null)
            {
                _outputDecklink.StopOutput();
                if (_outputDecklink.Device != null)
                {
                    _outputDecklink.StopOutput();
                }
            }
        }

        public void OnGUI()
        {
            GUI.skin = _guiSkin;

            if (_modeListStyle == null)
            {
                _modeListStyle = GUI.skin.GetStyle("ModeList");
            }

            // List the devices
            GUILayout.BeginVertical("box", GUILayout.MaxWidth(400));
            if (GUILayout.Button("Select Input and Output Device:"))
            {
                _selectedInputDevice = null;
                _selectedOutputDevice = null;
                _needsInputAutoScroll = _needsOutputAutoScroll = false;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box", GUILayout.MaxWidth(400));
            for (int i = 0; i < DeckLink.GetNumDevices(); i++)
            {
                Device device = DeckLink.GetDevice(i);

                GUILayout.BeginHorizontal();
                GUILayout.Label(device.Name + " " + device.ModelName, _modeListStyle, GUILayout.Width(192f));

                GUI.color = Color.white;
                if (device == _selectedInputDevice)
                    GUI.color = Color.blue;
                if (device.IsStreamingInput)
                    GUI.color = Color.green;

                GUI.enabled = true;
                if (device.IsStreamingOutput && !device.FullDuplexSupported)
                    GUI.enabled = false;

                if (GUILayout.Button("Input", _modeListStyle))
                {
                    _selectedInputDevice = device;
                    _needsInputAutoScroll = true;
                }

                GUI.color = Color.white;
                if (device == _selectedOutputDevice)
                    GUI.color = Color.blue;
                if (device.IsStreamingOutput)
                    GUI.color = Color.green;

                GUI.enabled = true;
                if (device.IsStreamingInput && !device.FullDuplexSupported)
                    GUI.enabled = false;

                if (GUILayout.Button("Output", _modeListStyle))
                {
                    _selectedOutputDevice = device;
                    _needsOutputAutoScroll = true;
                }

                GUI.enabled = device.IsStreaming;
                GUI.color = Color.white;
                if (GUILayout.Button("Stop", _modeListStyle))
                {
                    if (device.IsStreamingInput)
                        StopInput();
                    if (device.IsStreamingOutput)
                        StopOutput();
                }
                GUILayout.EndHorizontal();

                GUI.enabled = true;
                GUI.color = Color.white;
            }
            GUILayout.EndVertical();

            _outputModeMatchInputMode = GUILayout.Toggle(_outputModeMatchInputMode, "Auto Match Output Mode To Input");

            GUILayout.BeginHorizontal();

            // For the selected device, list the INPUTmodes available
            if (_selectedInputDevice != null)
            {
                GUILayout.BeginVertical();
                GUILayout.BeginVertical("box", GUILayout.MaxWidth(500));
                if (GUILayout.Button("Select " + _selectedInputDevice.Name + " Input Mode:"))
                {
                    _selectedInputDevice = null;
                }
                GUILayout.EndVertical();

                if (_selectedInputDevice != null)
                {
                    GUILayout.BeginVertical("box", GUILayout.MaxWidth(500), GUILayout.MaxHeight(400));
                    _inputModeScrollPos = GUILayout.BeginScrollView(_inputModeScrollPos, false, false);

                    for (int j = 0; j < _selectedInputDevice.NumInputModes; j++)
                    {
                        DeviceMode mode = _selectedInputDevice.GetInputMode(j);

                        if (_inputDecklink.Device != null && _inputDecklink.Device.IsStreaming && _inputDecklink.Device.CurrentMode == mode)
                            GUI.color = Color.green;

                        if (GUILayout.Button("" + j.ToString("D2") + ") " + mode.ModeDescription + " - " + mode.PixelFormatDescription + " - " + mode.Width + "x" + mode.Height, _modeListStyle))
                        {
                            StartInput(mode);
                        }

                        GUI.color = Color.white;
                    }

                    if (Event.current.type == EventType.repaint)
                    {
                        if (_needsInputAutoScroll)
                        {
                            if (_inputDecklink.Device != null && _inputDecklink.Device.IsStreaming && _inputDecklink.Device.CurrentMode != null)
                            {
                                float height = _modeListStyle.CalcHeight(new GUIContent("A"), 64);
                                float y = _inputDecklink.Device.CurrentMode.Index * height;
                                GUI.ScrollTo(new Rect(0, y, 10, height * 8));
                            }
                            _needsInputAutoScroll = false;
                        }
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                }
                GUILayout.EndVertical();
            }

            // For the selected device, list the OUTPUT modes available
            if (_selectedOutputDevice != null)
            {
                GUILayout.BeginVertical();
                GUILayout.BeginVertical("box", GUILayout.MaxWidth(500));
                if (GUILayout.Button("Select " + _selectedOutputDevice.Name + " Output Mode:"))
                {
                    _selectedOutputDevice = null;
                }
                GUILayout.EndVertical();

                if (_selectedOutputDevice != null)
                {
                    GUILayout.BeginVertical("box", GUILayout.MaxWidth(500), GUILayout.MaxHeight(400));
                    _outputModeScrollPos = GUILayout.BeginScrollView(_outputModeScrollPos, false, false);

                    for (int j = 0; j < _selectedOutputDevice.NumOutputModes; j++)
                    {
                        DeviceMode mode = _selectedOutputDevice.GetOutputMode(j);

                        if (_outputDecklink.Device != null && _outputDecklink.Device.IsStreaming && _outputDecklink.Device.CurrentOutputMode == mode)
                            GUI.color = Color.green;

                        if (GUILayout.Button("" + j.ToString("D2") + ") " + mode.ModeDescription + " - " + mode.PixelFormatDescription + " - " + mode.Width + "x" + mode.Height, _modeListStyle))
                        {
                            StartOutput(mode);
                        }

                        GUI.color = Color.white;
                    }

                    if (Event.current.type == EventType.repaint)
                    {
                        if (_needsOutputAutoScroll)
                        {
                            if (_outputDecklink.Device != null && _outputDecklink.Device.IsStreaming && _outputDecklink.Device.CurrentOutputMode != null)
                            {
                                float height = _modeListStyle.CalcHeight(new GUIContent("A"), 64);
                                float y = _outputDecklink.Device.CurrentOutputMode.Index * height;
                                GUI.ScrollTo(new Rect(0, y, 10, height * 8));
                            }
                            _needsOutputAutoScroll = false;
                        }
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                }
                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();


            // Active input output device summary
            GUILayout.BeginArea(new Rect(0, Screen.height - 64, Screen.width, 64));
            GUILayout.BeginVertical("box");
            string inputStr = "None";
            string outputStr = "None";
            if (_inputDecklink.Device != null && _inputDecklink.Device.IsStreamingInput)
            {
                inputStr = "" + _inputDecklink.Device.Name + " - " + _inputDecklink.Device.CurrentMode.ModeDescription;
            }
            if (_outputDecklink.Device != null && _outputDecklink.Device.IsStreamingOutput)
            {
                outputStr = "" + _outputDecklink.Device.Name + " - " + _outputDecklink.Device.CurrentOutputMode.ModeDescription;
            }
            GUILayout.Label("Input: " + inputStr, _modeListStyle);
            GUILayout.Label("Output: " + outputStr, _modeListStyle);
            GUILayout.EndVertical();
            GUILayout.EndArea();

            // Output stats
            GUILayout.BeginArea(new Rect(Screen.width - 256, Screen.height - 192, 256, 192));
            GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
            if (_outputDecklink.Device != null && _outputDecklink.Device.IsStreaming)
            {
                int numDecklinkBufferedFrames = DeckLinkPlugin.GetOutputBufferedFramesCount(_outputDecklink.Device.DeviceIndex);
                int numWaitingOutputFrames = DeckLinkPlugin.GetWaitingOutputBufferCount(_outputDecklink.Device.DeviceIndex);
                int numFreeOutputFrames = DeckLinkPlugin.GetFreeOutputBufferCount(_outputDecklink.Device.DeviceIndex);

                //GUILayout.Space(20f);
                GUILayout.Label(string.Format("VSync {0}", QualitySettings.vSyncCount));
                GUILayout.Label(string.Format("{0:F3} {1}", _outputDecklink.TargetFramerate, _outputDecklink.OutputFramerate));
                GUILayout.Label(string.Format("Buffers: DeckLink {0:D2} << Full {1:D2} << Empty {2:D2}", numDecklinkBufferedFrames, numWaitingOutputFrames, numFreeOutputFrames));

                //GUILayout.Label("Buffered Frames: " + AVProDeckLinkPlugin.GetOutputBufferedFramesCount(_outputDecklink.Device.DeviceIndex));
                //GUILayout.Label("Free Frames: " + AVProDeckLinkPlugin.GetFreeOutputBufferCount(_outputDecklink.Device.DeviceIndex));
                GUILayout.Label("Screen: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height + " " + Screen.currentResolution.refreshRate);
            }
            else
            {
                GUILayout.Label("No Output Stats");
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }


        void Start()
        {
            Application.runInBackground = true;
        }

        void OnDestroy()
        {
            StopInput();
            StopOutput();
        }
    }

}