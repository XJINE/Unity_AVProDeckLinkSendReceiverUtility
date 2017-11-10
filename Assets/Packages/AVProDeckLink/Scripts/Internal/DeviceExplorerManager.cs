using UnityEngine;
using System.Collections.Generic;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
    public class DeviceExplorerManager : PrefabSingleton<DeviceExplorerManager>
    {
        private List<DeckLink> _components;
        private DeckLink _currDeckLink = null;
        private Vector2 _lastScrollPos = Vector2.zero;
        Vector3 _lastMousePos = Vector3.zero;
        public GUISkin _skin;
        public float _showMaxButtonTime = 3f;
        public float _maxButtonTime = 0f;
        public bool _showExplorer = true;
        public int _depth = 1;

        [Range(1f, 10f)]
        public float _showSensitivity = 1f;

        void Awake()
        {
            _components = new List<DeckLink>();
        }

        void OnGUI()
        {
            if (_components.Count == 0)
            {
                return;
            }

            int prevDepth = GUI.depth;
            GUI.depth = _depth;

            GUISkin prevSkin = GUI.skin;
            GUI.skin = _skin;

            if (_showExplorer)
            {
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical("box", GUILayout.MinWidth(250), GUILayout.MaxHeight(150));

                if(GUILayout.Button("Select DeckLink Object"))
                {
                    _currDeckLink = null;
                }

                _lastScrollPos = GUILayout.BeginScrollView(_lastScrollPos);

                foreach (DeckLink component in _components)
                {
                    if(component == _currDeckLink)
                    {
                        GUI.color = Color.green;
                    }

                    if (GUILayout.Button(component.gameObject.name))
                    {
                        _currDeckLink = component;
                    }

                    GUI.color = Color.white;
                }

                GUILayout.EndScrollView();

                GUILayout.EndVertical();

                if(_currDeckLink != null)
                {
                    _currDeckLink.RenderExplorer();
                }

                if (GUILayout.Button("x", GUILayout.MaxWidth(20)))
                {
                    _showExplorer = false;
                    _maxButtonTime = 0f;
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                if(_maxButtonTime < _showMaxButtonTime)
                {
                    if (GUILayout.Button("Explorer", GUILayout.Width(100)))
                    {
                        _showExplorer = true;
                    }
                }
            }
            GUI.skin = prevSkin;
            GUI.depth = prevDepth;
        }

        public void RegisterExplorer(DeckLink deckLink)
        {
			if (_components.Contains(deckLink))
			{
				return;
			}


            _components.Add(deckLink);
        }

        public void UnregisterExplorer(DeckLink deckLink)
        {
            _components.Remove(deckLink);
            if(_currDeckLink == deckLink)
            {
                _currDeckLink = null;
            }
        }

        void Update()
        {
            Vector3 diff = Input.mousePosition - _lastMousePos;
            _lastMousePos = Input.mousePosition;

            float threshold = 1 / (_showSensitivity * _showSensitivity ) * 100f;

            if(diff.magnitude > threshold)
            {
                _maxButtonTime = 0f;
            }
            else
            {
                _maxButtonTime += Time.unscaledDeltaTime;
            }
        }
	}

}
