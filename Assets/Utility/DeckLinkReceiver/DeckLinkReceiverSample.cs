using RenderHeads.Media.AVProDeckLink;
using UnityEngine;

/// <summary>
/// DeckLink の入力から画像を受け取ります。
/// </summary>
public class DeckLinkReceiverSample : MonoBehaviour
{
    public bool fullScreen = false;

    public DeckLinkReceiver deckLinkReceiver;

    protected void OnGUI()
    {
        Texture texture = this.deckLinkReceiver.Texture;

        if (this.fullScreen)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), this.deckLinkReceiver.Texture, ScaleMode.StretchToFill);
        }
        else
        {
            Vector2 halfScreen  = new Vector2(Screen.width / 2, Screen.height / 2);
            Vector2 halfTexture = new Vector2(texture.width / 2, texture.height / 2);

            GUI.DrawTexture(new Rect(halfScreen.x - halfTexture.x,
                                     halfScreen.y - halfTexture.y,
                                     texture.width,
                                     texture.height),
                            this.deckLinkReceiver.Texture,
                            ScaleMode.ScaleToFit);
        }
    }
}