using UnityEngine;
using System.Collections;

namespace RenderHeads.Media.AVProDeckLink.Demos
{
    public class FrameShower : MonoBehaviour
    {
        TextMesh _textMesh;
        // Use this for initialization
        void Start()
        {
            _textMesh = GetComponent<TextMesh>();
        }

        // Update is called once per frame
        void Update()
        {
            _textMesh.text = Time.frameCount.ToString();
        }
    }
}