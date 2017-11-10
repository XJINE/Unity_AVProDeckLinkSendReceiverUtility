using RenderHeads.Media.AVProDeckLink;
using UnityEngine;

// NOTE:
// Singleton で良さそうですが、複数の送信先に送信する可能性があるので良くありません。

/// <summary>
/// DeckLink を使ってテクスチャを送信します。
/// </summary>
public class DeckLinkSender : MonoBehaviour
{
    #region Field

    /// <summary>
    /// 送信に利用する DeckLinkOutput 。
    /// </summary>
    public DeckLinkOutput deckLinkOutput;

    #endregion Field

    #region Method

    /// <summary>
    /// アウトプットするテクスチャを設定します。
    /// </summary>
    /// <param name="outputTexture">
    /// アウトプットするテクスチャ。
    /// </param>
    public void SetOutputTexture(Texture outputTexture)
    {
        this.deckLinkOutput.StopOutput();
        this.deckLinkOutput._camera = null;
        this.deckLinkOutput._defaultTexture = outputTexture;
    }

    /// <summary>
    /// アウトプットするカメラを設定します。
    /// </summary>
    /// <param name="camera">
    /// アウトプットするカメラ。
    /// </param>
    public void SetOutputCamera(Camera camera)
    {
        this.deckLinkOutput._defaultTexture = null;
        this.deckLinkOutput.SetCamera(camera);
    }

    #endregion Method
}