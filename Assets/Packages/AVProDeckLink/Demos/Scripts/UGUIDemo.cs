using UnityEngine;
using System.Collections;

namespace RenderHeads.Media.AVProDeckLink.Demos
{
    public class UGUIDemo : MonoBehaviour {
        public GameObject display;
        public DeckLink decklink;

        private int currRot = 0;
        // Use this for initialization
        void Start() {

        }

        // Update is called once per frame
        void Update() {

        }

        public void RotateDisplay()
        {
            if(display != null)
            {
                currRot = (currRot + 90) % 360;
                display.transform.rotation = Quaternion.AngleAxis((float)currRot, Vector3.forward);
            }
        }

        public void ToggleExplorer()
        {
            if (decklink != null)
            {
                decklink._showExplorer = !decklink._showExplorer;
            }
        }
    }
}
