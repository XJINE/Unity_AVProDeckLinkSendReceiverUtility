using RenderHeads.Media.AVProDeckLink;
using UnityEngine;

// NOTE:
// DeckLinkInput を Awake 中に GetComponent するような実装も考えましたが止めました(RequireComponent をセットにして)。
// 初期化順によっては DeckLInkInput が null になる可能性があるためです。しかし null チェックはパフォーマンスを落とします。
// テクスチャの参照は頻度が高いことが考えられることから、null チェックをしないために、手動で参照を設定します。

// NOTE:
// 一見すると Singleton で実装するのが良さそうですが、
// 複数のカードが差される可能性があるため、Singleton による実装は良くありません。

// NOTE:
// 既定のテクスチャを使う必要がない場合、DeckLinkInput をそのまま使うのが良さそうです。

/// <summary>
/// DeckLink の入力から画像を受け取ります。
/// </summary>
public class DeckLinkReceiver : MonoBehaviour
{
    /// <summary>
    /// DeckLinkInput への参照。
    /// </summary>
    public DeckLinkInput deckLinkInput;

    /// <summary>
    /// DeckLinkInput からテクスチャが得られないときの代替テクスチャ。
    /// </summary>
    public Texture2D defaultTexture = null;

    /// <summary>
    /// DeckLinkInput からテクスチャを取得します。
    /// </summary>
    public Texture Texture
    {
        get
        {
            return this.deckLinkInput.OutputTexture == null ?
                   this.defaultTexture : this.deckLinkInput.OutputTexture;
        }
    }
}