using System.Collections.Generic;
using UnityEngine;

public class DeckLinkSenderSample : MonoBehaviour
{
    #region Field

    public DeckLinkSender deckLinkSender;

    public List<Texture> sendTextures;

    protected int sendTexturesIndex = 0;

    public Camera sendCamera;

    public KeyCode setTextureKey = KeyCode.UpArrow;

    public KeyCode setCameraKey = KeyCode.Space;

    #endregion Field

    /// <summary>
    /// 更新時に呼び出されます。
    /// </summary>
    public virtual void Update()
    {
        if (Input.GetKeyDown(this.setTextureKey))
        {
            Debug.Log("HERE1");

            this.sendTexturesIndex++;

            if (this.sendTexturesIndex >= this.sendTextures.Count)
            {
                this.sendTexturesIndex = 0;
            }

            this.deckLinkSender.SetOutputTexture(this.sendTextures[this.sendTexturesIndex]);
        }

        if (Input.GetKeyDown(this.setCameraKey))
        {
            Debug.Log("HERE2");

            this.deckLinkSender.SetOutputCamera(this.sendCamera);
        }
    }
}