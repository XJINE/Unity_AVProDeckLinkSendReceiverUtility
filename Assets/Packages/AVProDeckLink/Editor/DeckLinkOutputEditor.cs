using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DeckLinkOutput))]
    public class DeckLinkOutputEditor : DeckLinkEditor
    {
        private DeckLinkOutput _camera;
        private SerializedProperty _keying_mode = null;
        private bool validate = true;

        void OnEnable()
        {
            Init();
        }

        private void Init()
        {
            _camera = (this.target) as DeckLinkOutput;

            _selectedDevice = serializedObject.FindProperty("_deviceIndex");

            _selectedMode = serializedObject.FindProperty("_modeIndex");

            _selectedResolution = serializedObject.FindProperty("_resolutionIndex");

            _keying_mode = serializedObject.FindProperty("_keyerMode");

            _isInput = false;

            _displayModes = true;

            _exactDeviceName = serializedObject.FindProperty("_exactDeviceName");
            _desiredDeviceName = serializedObject.FindProperty("_desiredDeviceName");
            _desiredDeviceIndex = serializedObject.FindProperty("_desiredDeviceIndex");
            _exactDeviceIndex = serializedObject.FindProperty("_exactDeviceIndex");
            _filterDeviceByName = serializedObject.FindProperty("_filterDeviceByName");
            _filterDeviceByIndex = serializedObject.FindProperty("_filterDeviceByIndex");

            _expandDeviceSel = serializedObject.FindProperty("_expandDeviceSel");
            _expandModeSel = serializedObject.FindProperty("_expandModeSel");
            _expandAbout = serializedObject.FindProperty("_expandAbout");

            _filterModeByResolution = serializedObject.FindProperty("_filterModeByResolution");
            _filterModeByFormat = serializedObject.FindProperty("_filterModeByFormat");
            _filterModeByFPS = serializedObject.FindProperty("_filterModeByFPS");
            _filterModeByInterlacing = serializedObject.FindProperty("_filterModeByInterlacing");
            _modeWidth = serializedObject.FindProperty("_modeWidth");
            _modeHeight = serializedObject.FindProperty("_modeHeight");
            _modeFormat = serializedObject.FindProperty("_modeFormat");
            _modeFPS = serializedObject.FindProperty("_modeFPS");
            _modeInterlacing = serializedObject.FindProperty("_modeInterlacing");

            _expandPreview = serializedObject.FindProperty("_expandPreview");

            _showExplorer = serializedObject.FindProperty("_showExplorer");
        }

        private void DrawKeyerModes()
        {
            int newKeyMode = EditorGUILayout.Popup("Keying Mode", _keying_mode.intValue, _keying_mode.enumDisplayNames);

            if (_keying_mode.intValue != newKeyMode)
            {
                validate = true;
            }

            _keying_mode.intValue = newKeyMode;
        }

        private void ValidateKeyerMode()
        {
            bool internal_supported = DeckLinkPlugin.SupportsInternalKeying(_selectedDevice.intValue);
            bool external_supported = DeckLinkPlugin.SupportsExternalKeying(_selectedDevice.intValue);

            if ((DeckLinkOutput.KeyerMode)_keying_mode.intValue == DeckLinkOutput.KeyerMode.External)
            {
                if (!external_supported && validate)
                {
                    validate = false;
                    Debug.LogWarning("External keying mode for DeckLinkOutput component is not supported by the selected decklink device");
                }
            }
            else if ((DeckLinkOutput.KeyerMode)_keying_mode.intValue == DeckLinkOutput.KeyerMode.Internal)
            {
                if (!internal_supported && validate)
                {
                    validate = false;
                    Debug.LogWarning("Internal keying mode for DeckLinkOutput component is not supported by the selected card");
                }
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return _expandPreview.boolValue;
        }

        protected override bool ModeValid(DeckLinkPlugin.PixelFormat format)
        {
            return DeckLinkOutput.OutputFormatSupported(format);
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null)
            {
                Init();
            }

            serializedObject.Update();
            if (!Application.isPlaying)
            {
                DrawDefaultInspector();
                EditorGUILayout.PropertyField(_showExplorer);

                EditorGUIUtility.labelWidth = 150;
                DrawDeviceFilters();
                EditorGUIUtility.labelWidth = 175;
                DrawModeFilters(false);
                DrawPreviewTexture(null);
                OnInspectorGUI_About();
            }
            else
            {
                DrawDefaultInspector();
                EditorGUILayout.PropertyField(_showExplorer);
                DrawPreviewTexture(_camera);
                OnInspectorGUI_About();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
