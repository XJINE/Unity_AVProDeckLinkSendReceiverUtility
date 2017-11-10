using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink.Editor
{
    public abstract class DeckLinkEditor : UnityEditor.Editor
    {
        protected bool _isInput = true;
        protected SerializedProperty _selectedDevice = null;
        protected SerializedProperty _selectedMode = null;
        private Vector2 _scrollPos = new Vector2(0, 0);
        protected SerializedProperty _selectedResolution = null;
        protected bool _displayModes;
        private bool _expandModes = false;
		private static Texture2D _icon;

        protected SerializedProperty _exactDeviceName;
        protected SerializedProperty _desiredDeviceName;
        protected SerializedProperty _desiredDeviceIndex;
        protected SerializedProperty _exactDeviceIndex;
        protected SerializedProperty _filterDeviceByName;
        protected SerializedProperty _filterDeviceByIndex;

        protected SerializedProperty _expandDeviceSel;
        protected SerializedProperty _expandModeSel;
        protected SerializedProperty _expandAbout;

        protected SerializedProperty _filterModeByResolution;
        protected SerializedProperty _filterModeByFormat;
        protected SerializedProperty _filterModeByFPS;
        protected SerializedProperty _filterModeByInterlacing;
        protected SerializedProperty _modeWidth;
        protected SerializedProperty _modeHeight;
        protected SerializedProperty _modeFormat;
        protected SerializedProperty _modeFPS;
        protected SerializedProperty _modeInterlacing;

        protected SerializedProperty _expandPreview;

        protected SerializedProperty _showExplorer;

        private DeckLinkPlugin.PixelFormat[] formats = null;
        private string[] formatNames = null;

        private string[] fpsNames = null;
        private float[] frameRates = null;

        private Resolution[] resolutions = null;
        private string[] resolutionNames = null;

        private const string LinkPluginWebsite = "http://renderheads.com/product/avpro-decklink/";
        private const string LinkForumPage = "http://forum.unity3d.com/threads/released-avpro-decklink-broadcast-video-input-and-output-for-unity.423940/";
        private const string LinkAssetStorePage = "https://www.assetstore.unity3d.com/#!/content/68784";
        private const string LinkEmailSupport = "mailto:unitysupport@renderheads.com";
        private const string LinkUserManual = "http://downloads.renderheads.com/docs/UnityAVProDeckLink.pdf";
        private const string LinkScriptingClassReference = "http://downloads.renderheads.com/docs/AVProDeckLinkClassReference/";
        private const string SupportMessage = "If you are reporting a bug, please include any relevant files and details so that we may remedy the problem as fast as possible.\n\n" +
            "Essential details:\n" +
            "+ Error message\n" +
            "      + The exact error message\n" +
            "      + The console/output log if possible\n" +
            "+ Hardware\n" +
            "      + Phone / tablet / device type and OS version\n" +
            "      + DeckLink device model\n" +
            "      + Input / output device information\n" +
            "+ Development environment\n" +
            "      + Unity version\n" +
            "      + Development OS version\n" +
            "      + AVPro DeckLink plugin version\n" +
            " + Mode details\n" +
            "      + Resolution\n" +
            "      + Format\n" +
            "      + Frame Rate\n" +
            "      + Interlaced / Non-interlaced\n";

        protected void DrawDeviceFilters()
        {
            GUILayout.Space(8f);

            if (GUILayout.Button("Device Selection", EditorStyles.toolbarButton))
            {
                _expandDeviceSel.boolValue = !_expandDeviceSel.boolValue;
            }

            if (_expandDeviceSel.boolValue)
            {
                GUILayout.BeginVertical("box");
                if (_filterDeviceByName.boolValue)
                {
                    GUI.color = Color.green;
                }

                GUIStyle buttonStyle = GUI.skin.GetStyle("Button");
                buttonStyle.alignment = TextAnchor.UpperCenter;

                GUILayout.Space(4f);
                if (GUILayout.Button("Select device by name", buttonStyle)){
                    _filterDeviceByName.boolValue = !_filterDeviceByName.boolValue;
                }

                GUI.color = Color.white;
                
                if (_filterDeviceByName.boolValue)
                {
                    EditorGUILayout.BeginVertical("box");
                    _exactDeviceName.boolValue = !EditorGUILayout.Toggle("Approximate search", !_exactDeviceName.boolValue);
                    _desiredDeviceName.stringValue = EditorGUILayout.TextField("Device Name", _desiredDeviceName.stringValue).Trim();
                    EditorGUILayout.EndVertical();
                }

                if (_filterDeviceByIndex.boolValue)
                {
                    GUI.color = Color.green;
                }

                GUILayout.Space(4f);
                if (GUILayout.Button("Select device by index")){
                    _filterDeviceByIndex.boolValue = !_filterDeviceByIndex.boolValue;
                }

                GUI.color = Color.white;

                if (_filterDeviceByIndex.boolValue)
                {
                    EditorGUILayout.BeginVertical("box");
                    _exactDeviceIndex.boolValue = !EditorGUILayout.Toggle("Approximate search", !_exactDeviceIndex.boolValue);
                    _desiredDeviceIndex.intValue = EditorGUILayout.IntField("Device Index", _desiredDeviceIndex.intValue);
                    EditorGUILayout.EndVertical();
                }

                GUILayout.Space(4f);

                GUILayout.EndVertical();

            }

            GUILayout.Space(8f);
        }

        private static string[] GetInputFormats(out DeckLinkPlugin.PixelFormat[] formats)
        {
            string[] names = new string[4];
            formats = new DeckLinkPlugin.PixelFormat[4];

            names[0] = "UYVY 4:2:2";
            names[1] = "v210";
            names[2] = "8-bit ARGB";
            names[3] = "8-bit BGRA";

            formats[0] = DeckLinkPlugin.PixelFormat.YCbCr_8bpp_422;
            formats[1] = DeckLinkPlugin.PixelFormat.YCbCr_10bpp_422;
            formats[2] = DeckLinkPlugin.PixelFormat.ARGB_8bpp_444;
            formats[3] = DeckLinkPlugin.PixelFormat.BGRA_8bpp_444;

            return names;
        }

        private static string[] GetOutputFormats(out DeckLinkPlugin.PixelFormat[] formats)
        {
            string[] names = new string[3];
            formats = new DeckLinkPlugin.PixelFormat[3];

            names[0] = "UYVY 4:2:2";
            names[1] = "8-bit ARGB";
            names[2] = "8-bit BGRA";

            formats[0] = DeckLinkPlugin.PixelFormat.YCbCr_8bpp_422;
            formats[1] = DeckLinkPlugin.PixelFormat.ARGB_8bpp_444;
            formats[2] = DeckLinkPlugin.PixelFormat.BGRA_8bpp_444;

            return names;
        }

        protected void DrawModeFilters(bool isInput)
        {
            GUILayout.Space(8f);

            if (GUILayout.Button("Mode Selection", EditorStyles.toolbarButton))
            {
                _expandModeSel.boolValue = !_expandModeSel.boolValue;
            }

            if (_expandModeSel.boolValue)
            {
                GUILayout.BeginVertical("box");

                GUILayout.Space(4f);
                if (_filterModeByResolution.boolValue)
                {
                    GUI.color = Color.green;
                }

                if(GUILayout.Button("Force resolution"))
                {
                    _filterModeByResolution.boolValue = !_filterModeByResolution.boolValue;
                }

                GUI.color = Color.white;

                if (_filterModeByResolution.boolValue)
                {
                    GUILayout.BeginVertical("box");

                    if(resolutions == null || resolutionNames == null)
                    {
                        GetResolutions(out resolutions, out resolutionNames);
                    }

                    int foundPos = resolutionNames.Length - 1;
                    for(int i = 0; i < resolutions.Length; ++i)
                    {
                        
                        if (resolutions[i].width == _modeWidth.intValue && resolutions[i].height == _modeHeight.intValue)
                        {
                            foundPos = i;
                            break;
                        }
                    }
                    
                    int newPos = EditorGUILayout.Popup("Resolution", foundPos, resolutionNames);
                                        

                    if(newPos < resolutions.Length)
                    {
                        _modeWidth.intValue = resolutions[newPos].width;
                        _modeHeight.intValue = resolutions[newPos].height;
                    }
                    else
                    {
                        if(newPos != foundPos)
                        {
                            _modeWidth.intValue = 0;
                            _modeHeight.intValue = 0;
                        }

                        _modeWidth.intValue = EditorGUILayout.IntField("Width ", _modeWidth.intValue);
                        _modeHeight.intValue = EditorGUILayout.IntField("Height ", _modeHeight.intValue);
                    }
                    
                    GUILayout.EndVertical();
                }

                GUILayout.Space(4f);

                if (_filterModeByFormat.boolValue)
                {
                    GUI.color = Color.green;
                }

                if(GUILayout.Button("Force Format"))
                {
                    _filterModeByFormat.boolValue = !_filterModeByFormat.boolValue;
                }

                GUI.color = Color.white;

                if (_filterModeByFormat.boolValue)
                {
                    GUILayout.BeginVertical("box");

                    if(formatNames == null || formats == null)
                    {
                        if (isInput)
                        {
                            formatNames = GetInputFormats(out formats);
                        }
                        else
                        {
                            formatNames = GetOutputFormats(out formats);
                        }
                    }
                    

                    DeckLinkPlugin.PixelFormat prevFormat = (DeckLinkPlugin.PixelFormat)_modeFormat.intValue;
                    int prevSelected = 0;

                    for (int i = 0; i < formats.Length; ++i)
                    {
                        if (prevFormat == formats[i])
                        {
                            prevSelected = i;
                            break;
                        }
                    }

                    int selected = EditorGUILayout.Popup("Pixel Format", prevSelected, formatNames);
                    _modeFormat.intValue = (int)formats[selected];
                    GUILayout.EndVertical();
                }

                GUILayout.Space(4f);

                if (_filterModeByFPS.boolValue)
                {
                    GUI.color = Color.green;
                }

                if(GUILayout.Button("Force Frame Rate"))
                {
                    _filterModeByFPS.boolValue = !_filterModeByFPS.boolValue;
                }

                GUI.color = Color.white;

                if (_filterModeByFPS.boolValue)
                {
                    GUILayout.BeginVertical("box");

                    if(frameRates == null || fpsNames == null)
                    {
                        GetFrameRates(out frameRates, out fpsNames);
                    }

                    int foundPos = fpsNames.Length - 1;
                    for(int i = 0; i < frameRates.Length; ++i)
                    {
                        if(Mathf.Abs(frameRates[i] - _modeFPS.floatValue) < 0.005f)
                        {
                            foundPos = i;
                            break;
                        }
                    }

                    int newPos = EditorGUILayout.Popup("Frame Rate", foundPos, fpsNames);
                    if (newPos < frameRates.Length)
                    {
                        _modeFPS.floatValue = frameRates[newPos];
                    }
                    else
                    {
                        if(newPos != foundPos)
                        {
                            _modeFPS.floatValue = 0f;
                        }

                        _modeFPS.floatValue = EditorGUILayout.FloatField("FrameRate", _modeFPS.floatValue);
                    }

                    GUILayout.EndVertical();
                }

                GUILayout.Space(4f);

                if (_filterModeByInterlacing.boolValue)
                {
                    GUI.color = Color.green;
                }
                
                if(GUILayout.Button("Force interlacing"))
                {
                    _filterModeByInterlacing.boolValue = !_filterModeByInterlacing.boolValue;
                }

                GUI.color = Color.white;

                if (_filterModeByInterlacing.boolValue)
                {
                    GUILayout.BeginVertical("box");
                    _modeInterlacing.boolValue = EditorGUILayout.Toggle("Interlaced", _modeInterlacing.boolValue);
                    GUILayout.EndVertical();
                }
                GUILayout.Space(4f);

                GUILayout.EndVertical();
            }

            GUILayout.Space(8f);
        }

        private string[] GetDevices()
        {
            int num_devices = DeckLinkPlugin.GetNumDevices();

            string[] devices = new string[num_devices];

            for (int i = 0; i < num_devices; ++i)
            {
                devices[i] = DeckLinkPlugin.GetDeviceDisplayName(i);
            }

            return devices;
        }

        private void GetResolutions(out Resolution[] resolutions, out string[] resolutionNames)
        {
            resolutions = new Resolution[7];
            resolutions[0].width = 720; resolutions[0].height = 486;
            resolutions[1].width = 720; resolutions[1].height = 576;
            resolutions[2].width = 1280; resolutions[2].height = 720;
            resolutions[3].width = 1920; resolutions[3].height = 1080;
            resolutions[4].width = 2048; resolutions[4].height = 1080;
            resolutions[5].width = 3840; resolutions[5].height = 2160;
            resolutions[6].width = 4096; resolutions[6].height = 2160;

            resolutionNames = new string[8] {
                "NTSC",
                "PAL",
                "HD720p",
                "HD1080p",
                "2K DCI",
                "UHD 4K",
                "4K DCI",
                "Custom"
            };
        }

        private void GetFrameRates(out float[] frameRates, out string[] frameRateNames)
        {
            frameRates = new float[8]
            {
                23.98f,
                24f,
                25f,
                29.97f,
                30f,
                50f,
                59.94f,
                60
            };

            frameRateNames = new string[9]
            {
                "23.98",
                "24",
                "25",
                "29.97",
                "30",
                "50",
                "59.94",
                "60",
                "Custom"
            };
        }

        protected abstract bool ModeValid(DeckLinkPlugin.PixelFormat format);

        private string[] GetDeviceModes(int device, out List<Resolution> resolutions, out Resolution[] modeResolutions, out int[] positions)
        {
            List<Resolution> outputRes = new List<Resolution>();
            int num_modes = DeckLinkPlugin.GetNumVideoInputModes(device);

            List<string> modes = new List<string>();
            List<Resolution> mrs = new List<Resolution>();
            List<int> actual_positions = new List<int>();

            for (int i = 0; i < num_modes; ++i)
            {
                int width, height, fieldMode;
                float frameRate;
                long frameDuration;
                string modeDesc, pixelFormatDesc;

                if (_isInput)
                {
                    DeckLinkPlugin.GetVideoInputModeInfo(device, i, out width, out height, out frameRate, out frameDuration, out fieldMode, out modeDesc, out pixelFormatDesc);
                }
                else
                {
                    DeckLinkPlugin.GetVideoOutputModeInfo(device, i, out width, out height, out frameRate, out frameDuration, out fieldMode, out modeDesc, out pixelFormatDesc);
                }

                DeckLinkPlugin.PixelFormat format = DeckLinkPlugin.GetPixelFormat(pixelFormatDesc);

                if (FormatConverter.InputFormatSupported(format))
                {
                    modes.Add(modeDesc + " " + pixelFormatDesc);

                    Resolution r = new Resolution();
                    r.width = width;
                    r.height = height;
                    mrs.Add(r);

                    actual_positions.Add(i);

                    bool resolutionFound = false;
                    for (int j = 0; j < outputRes.Count; ++j)
                    {
                        if (width == outputRes[j].width && height == outputRes[j].height)
                        {
                            resolutionFound = true;
                            break;
                        }
                    }

                    if (!resolutionFound)
                    {
                        Resolution res = new Resolution();
                        res.width = width;
                        res.height = height;
                        outputRes.Add(res);
                    }
                }
            }

            resolutions = outputRes;
            modeResolutions = mrs.ToArray();
            positions = actual_positions.ToArray();

            return modes.ToArray();
        }

        protected void OnInspectorGUI_About()
        {
            GUILayout.Space(8f);

            if (GUILayout.Button("About", EditorStyles.toolbarButton))
            {
                _expandAbout.boolValue = !_expandAbout.boolValue;
            }

            if (_expandAbout.boolValue)
            {
                string version = DeckLinkPlugin.GetNativePluginVersion();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (_icon == null)
				{
					_icon = Resources.Load<Texture2D>("AVProDeckLinkIcon");
				}
				if (_icon != null)
				{
					GUILayout.Label(new GUIContent(_icon));
				}
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.color = Color.yellow;
                GUILayout.Label("AVPro DeckLink by RenderHeads Ltd", EditorStyles.boldLabel);
                GUI.color = Color.white;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.color = Color.yellow;
                GUILayout.Label("version " + version + " (scripts v" + Helper.Version + ")");
                GUI.color = Color.white;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(32f);
                GUI.backgroundColor = Color.white;

                EditorGUILayout.LabelField("Links", EditorStyles.boldLabel);

                GUILayout.Space(8f);

                EditorGUILayout.LabelField("Documentation");
                if (GUILayout.Button("User Manual", GUILayout.ExpandWidth(false)))
                {
                    Application.OpenURL(LinkUserManual);
                }
                if (GUILayout.Button("Scripting Class Reference", GUILayout.ExpandWidth(false)))
                {
                    Application.OpenURL(LinkScriptingClassReference);
                }

                GUILayout.Space(16f);

                GUILayout.Label("Rate and Review (★★★★☆)", GUILayout.ExpandWidth(false));
                if (GUILayout.Button("Unity Asset Store Page", GUILayout.ExpandWidth(false)))
                {
                    Application.OpenURL(LinkAssetStorePage);
                }

                GUILayout.Space(16f);

                GUILayout.Label("Community");
                if (GUILayout.Button("Unity Forum Page", GUILayout.ExpandWidth(false)))
                {
                    Application.OpenURL(LinkForumPage);
                }

                GUILayout.Space(16f);

                GUILayout.Label("Homepage", GUILayout.ExpandWidth(false));
                if (GUILayout.Button("AVPro DeckLink Website", GUILayout.ExpandWidth(false)))
                {
                    Application.OpenURL(LinkPluginWebsite);
                }

                GUILayout.Space(16f);

                GUILayout.Label("Bugs and Support");
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Email unitysupport@renderheads.com", GUILayout.ExpandWidth(false)))
                {
                    Application.OpenURL(LinkEmailSupport);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(32f);

				EditorGUILayout.LabelField("Credits", EditorStyles.boldLabel);
				GUILayout.Space(8f);

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Programming", EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(8f);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Andrew Griffiths");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Sunrise Wang");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				GUILayout.Space(8f);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Graphics", EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(8f);

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Jeff Rusch");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				GUILayout.Space(32f);

				EditorGUILayout.LabelField("Bug Reporting Notes", EditorStyles.boldLabel);

                EditorGUILayout.SelectableLabel(SupportMessage, EditorStyles.wordWrappedLabel, GUILayout.Height(300f));
            }

            GUILayout.Space(8f);
        }

        protected void DrawDeviceModes()
        {
            if (GUILayout.Button("Refresh devices"))
            {
                DeckLinkPlugin.Deinit();
                DeckLinkPlugin.Init();
            }

            var devices = GetDevices();

            _selectedDevice.intValue = Mathf.Min(_selectedDevice.intValue, devices.Length - 1);
            _selectedDevice.intValue = EditorGUILayout.Popup("Device", _selectedDevice.intValue < 0 ? 0 : _selectedDevice.intValue, devices);

            if (devices.Length == 0)
            {
                _selectedDevice.intValue = -1;
            }

            if (_displayModes)
            {
                GUILayout.Space(8f);

                if (GUILayout.Button("Modes", EditorStyles.toolbarButton))
                {
                    _expandModes = !_expandModes;
                }

                if (_expandModes)
                {
                    string[] modes;
                    List<Resolution> resolutions;
                    Resolution[] modeResolutions;
                    int[] actual_positions;

                    if (_selectedDevice.intValue >= 0)
                    {
                        modes = GetDeviceModes(_selectedDevice.intValue, out resolutions, out modeResolutions, out actual_positions);
                    }
                    else
                    {
                        modes = new string[0];
                        resolutions = new List<Resolution>();
                        modeResolutions = new Resolution[0];
                        actual_positions = new int[0];
                    }

                    int prev_pos = 0;

                    for (int i = 0; i < actual_positions.Length; ++i)
                    {
                        if (actual_positions[i] == _selectedMode.intValue)
                        {
                            prev_pos = i;
                            break;
                        }
                    }

                    EditorGUILayout.LabelField("Mode");

                    int rows = resolutions.Count % 4 == 0 ? resolutions.Count / 4 : resolutions.Count / 4 + 1;

                    for (int i = 0; i < rows; ++i)
                    {
                        EditorGUILayout.BeginHorizontal();
                        for (int j = 0; j < 4; ++j)
                        {
                            int pos = i * 4 + j;
                            if (pos >= resolutions.Count)
                            {
                                break;
                            }

                            if (_selectedResolution.intValue == pos)
                            {
                                GUI.color = Color.cyan;
                            }

                            if (GUILayout.Button(resolutions[pos].width + "x" + resolutions[pos].height))
                            {
                                _selectedResolution.intValue = pos;
                            }

                            GUI.color = Color.white;
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (_selectedResolution.intValue >= 0 && _selectedResolution.intValue < resolutions.Count)
                    {
                        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, false, false, GUILayout.MaxHeight(200), GUILayout.MinHeight(200));

                        for (int i = 0; i < modes.Length; ++i)
                        {
                            if (modeResolutions[i].width != resolutions[_selectedResolution.intValue].width ||
                                modeResolutions[i].height != resolutions[_selectedResolution.intValue].height)
                            {
                                continue;
                            }

                            bool selected = false;

                            if (prev_pos == i)
                            {
                                GUI.color = Color.yellow;
                            }

                            selected = GUILayout.Button(modes[i]);

                            if (prev_pos == i)
                            {
                                GUI.color = Color.white;
                            }

                            if (selected)
                            {
                                _selectedMode.intValue = actual_positions[i];
                            }
                        }

                        EditorGUILayout.EndScrollView();
                    }

                    if (modes.Length == 0)
                    {
                        _selectedMode.intValue = -1;
                    }
                }

            }

            OnInspectorGUI_About();
        }

        protected void DrawPreviewTexture(DeckLink decklink)
        {
            GUILayout.Space(8f);

            if (GUILayout.Button("Preview", EditorStyles.toolbarButton))
            {
                _expandPreview.boolValue = !_expandPreview.boolValue;
            }

            if (_expandPreview.boolValue)
            {
                bool active = decklink != null && decklink.Device != null;

                GUI.enabled = active;

                Texture previewTex = null;
                if (active)
                {
                    previewTex = _isInput ? ((DeckLinkInput)decklink).OutputTexture : ((DeckLinkOutput)decklink).InputTexture;
                    if (previewTex == null)
                    {
                        previewTex = EditorGUIUtility.whiteTexture;
                    }
                }
                else
                {
                    previewTex = EditorGUIUtility.whiteTexture;
                }

                GUILayout.Space(8f);

                if (previewTex != EditorGUIUtility.whiteTexture)
                {
                    Rect textureRect = GUILayoutUtility.GetRect(128.0f, 128.0f, GUILayout.MinWidth(128.0f), GUILayout.MinHeight(128.0f));
                    GUI.DrawTexture(textureRect, previewTex, ScaleMode.ScaleToFit);
                }
                else
                {
                    Rect textureRect = GUILayoutUtility.GetRect(1920f / 40, 1080f / 40);
                    GUI.DrawTexture(textureRect, EditorGUIUtility.whiteTexture, ScaleMode.ScaleToFit);
                }

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select Texture", GUILayout.ExpandWidth(false)))
                {
                    Selection.activeObject = previewTex;
                }
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				Device device = active ? decklink.Device : null;
                string deviceName = active ? device.Name : "N/A";
                GUILayout.Label("Device: " + deviceName);

				DeviceMode mode = null;

				if(device != null)
				{
					mode = _isInput ? device.CurrentMode : device.CurrentOutputMode;
				}
				

				if (active && mode != null)
                {
                    GUILayout.Label(string.Format("Mode: {0}x{1}/{2}hz {3}", mode.Width, mode.Height, mode.FrameRate.ToString("F2"), mode.PixelFormatDescription));
                }

                if (_isInput)
                {
                    if (active && device.FramesTotal > 30)
                    {
                        GUILayout.Label("Running at " + device.FPS.ToString("F1") + " fps");
                    }
                    else
                    {
                        GUILayout.Label("Running at ... fps");
                    }

                    if (active && device.IsStreaming)
                    {
                        GUILayout.BeginHorizontal();
                        if (device.IsPaused)
                        {
                            if (GUILayout.Button("Unpause Stream"))
                            {
                                device.Unpause();
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Pause Stream"))
                            {
                                device.Pause();
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    if (active)
                    {
                        GUILayout.Label("Genlock status: " + (device.IsGenLocked ? " Locked" : "Not Locked"));
                        if (device.IsGenLocked)
                        {
                            GUILayout.Label("Pixel offset: " + device.GenlockOffset);
                            GUILayout.Label("Full Frame Pixel Offset is " + (device.SupportsFullFrameGenlockOffset ? " supported" : "not supported"));
                        }
                        
                    }
                }

                GUI.enabled = true;
            }

            GUILayout.Space(8f);
        }
    }
}
