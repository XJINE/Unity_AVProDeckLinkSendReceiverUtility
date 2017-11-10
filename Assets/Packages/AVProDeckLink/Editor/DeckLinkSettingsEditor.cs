using UnityEngine;
using UnityEditor;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
	[CustomEditor(typeof(DeckLinkSettings))]
	public class DeckLinkSettingsEditor : UnityEditor.Editor
	{
		private SerializedProperty _deviceSettings;

		private SerializedProperty _showSettings;

		private void DrawDeviceSettings()
		{
			GUILayout.Space(8f);

			DrawDefaultInspector();

			//GUILayout.Label("DeckLink Settings");
			_showSettings.boolValue = EditorGUILayout.Foldout(_showSettings.boolValue, "DeckLink Settings");

			if (_showSettings.boolValue)
			{
				int toRemove = -1;
				GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));

				int arraySize = _deviceSettings.arraySize;
				for(int i = 0; i < arraySize; ++i)
				{
					GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
					SerializedProperty setting = _deviceSettings.GetArrayElementAtIndex(i);
					SerializedProperty nameToFind = setting.FindPropertyRelative("deviceName");
					SerializedProperty single = setting.FindPropertyRelative("single");
					SerializedProperty setDuplexMode = setting.FindPropertyRelative("setDuplexMode");

					nameToFind.stringValue = EditorGUILayout.TextField("Device Name", nameToFind.stringValue);

					if (single.boolValue)
					{
						GUI.color = Color.green;
					}

					if(GUILayout.Button("Single Device"))
					{
						single.boolValue = !single.boolValue;
					}

					if (single.boolValue)
					{
						GUI.color = Color.white;
					}

					if (single.boolValue)
					{
						SerializedProperty index = setting.FindPropertyRelative("deviceIndex");

						index.intValue = EditorGUILayout.IntField("Device Index", index.intValue);
					}

					if (setDuplexMode.boolValue)
					{
						GUI.color = Color.green;
					}

					if (GUILayout.Button("Set Duplex Mode"))
					{
						setDuplexMode.boolValue = !setDuplexMode.boolValue;
					}

					if (setDuplexMode.boolValue)
					{
						GUI.color = Color.white;
					}

					if (setDuplexMode.boolValue)
					{
						SerializedProperty duplexMode = setting.FindPropertyRelative("duplexMode");

						string[] duplexModes = new string[] { "Full", "Half" };

						duplexMode.enumValueIndex = EditorGUILayout.Popup("Duplex Mode", duplexMode.enumValueIndex, duplexModes);
					}

					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					GUI.color = Color.red;
					if (GUILayout.Button("Remove", GUILayout.MaxWidth(100)))
					{
						toRemove = i;
					}
					GUILayout.EndHorizontal();
					GUI.color = Color.white;
					GUILayout.Space(8f);

					GUILayout.EndVertical();
				}

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();

				GUI.color = Color.green;
				if (GUILayout.Button("Add Setting", GUILayout.MaxWidth(100)))
				{
					_deviceSettings.arraySize = arraySize + 1;
				}
				GUILayout.EndHorizontal();
				GUI.color = Color.white;

				GUILayout.EndVertical();
				
				if(toRemove != -1)
				{
					_deviceSettings.DeleteArrayElementAtIndex(toRemove);
				}
			}
			

			GUILayout.Space(8f);
		}

		void OnEnable()
		{
			_deviceSettings = serializedObject.FindProperty("_deviceSettings");
			_showSettings = serializedObject.FindProperty("_showSettings");
		}

		public override bool RequiresConstantRepaint()
		{
			return false;
		}

		public override void OnInspectorGUI()
		{
			DrawDeviceSettings();
			serializedObject.ApplyModifiedProperties();
		}
	}
}