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
    [CustomEditor(typeof(DeckLinkInput))]
    public class DeckLinkInputEditor : DeckLinkEditor
    {
        private DeckLinkInput _camera;
		protected SerializedProperty _flipx;
		protected SerializedProperty _flipy;

		void OnEnable()
        {
            Init();
        }

        private void Init()
        {
            _camera = (this.target) as DeckLinkInput;

            _selectedDevice = serializedObject.FindProperty("_deviceIndex");

            _selectedMode = serializedObject.FindProperty("_modeIndex"); ;

            _selectedResolution = serializedObject.FindProperty("_resolutionIndex");

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

			_flipx = serializedObject.FindProperty("_flipX");
			_flipy = serializedObject.FindProperty("_flipY");

			_isInput = true;
        }

        public override bool RequiresConstantRepaint()
        {
            return _expandPreview.boolValue;
        }

        protected override bool ModeValid(DeckLinkPlugin.PixelFormat format)
        {
            return FormatConverter.InputFormatSupported(format);
        }

		private void DrawFlipCheckboxes()
		{
			_flipx.boolValue = EditorGUILayout.Toggle("Flip X", _flipx.boolValue);
			_camera.FlipX = _flipx.boolValue;

			_flipy.boolValue = EditorGUILayout.Toggle("Flip Y", _flipy.boolValue);
			_camera.FlipY = _flipy.boolValue;
		}

        public override void OnInspectorGUI()
        {
			if(serializedObject == null)
			{
				return;
			}

            if (_camera == null)
            {
                Init();
            }

            serializedObject.Update();

            if (!Application.isPlaying)
            {
                DrawDefaultInspector();
				DrawFlipCheckboxes();

				EditorGUILayout.PropertyField(_showExplorer);

                EditorGUIUtility.labelWidth = 150;
                DrawDeviceFilters();
                EditorGUIUtility.labelWidth = 175;
                DrawModeFilters(true);
                DrawPreviewTexture(null);
                OnInspectorGUI_About();
            }
            else
            {
                DrawDefaultInspector();
				DrawFlipCheckboxes();

				EditorGUILayout.PropertyField(_showExplorer);
                DrawPreviewTexture(_camera);

                OnInspectorGUI_About();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
