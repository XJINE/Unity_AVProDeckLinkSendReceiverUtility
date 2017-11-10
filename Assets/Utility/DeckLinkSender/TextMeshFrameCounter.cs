using UnityEngine;

public class TextMeshFrameCounter : MonoBehaviour
{
    public TextMesh textMesh;

    void Update()
    {
        this.textMesh.text = Time.frameCount.ToString();
    }
}