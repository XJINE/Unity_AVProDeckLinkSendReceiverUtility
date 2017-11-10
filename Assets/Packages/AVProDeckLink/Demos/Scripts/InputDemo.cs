using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2014-2016 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink.Demos
{
    public class InputDemo : MonoBehaviour
    {
        public DeckLinkInput _decklink;

        void Start()
        {
            if (_decklink == null)
                _decklink = GetComponent<DeckLinkInput>();
        }

        void Update()
        {
        }

        void OnGUI()
        {
            if (_decklink.Device == null)
                return;

            if (_decklink.Device.CurrentMode == null)
                return;

            GUI.depth = 2;
            
            GUILayout.Label("Using input mode " + _decklink.Device.CurrentMode.Index + "/" + _decklink.Device.NumInputModes + ": " + _decklink.Device.CurrentMode.ModeDescription);
        }
    }
}
