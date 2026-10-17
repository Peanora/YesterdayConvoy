using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Persistent sprite subassets. Runtime animation never mutates this library.</summary>
    public sealed class LayeredSpriteLibrary : ScriptableObject
    {
        [SerializeField] private List<Sprite> sprites = new List<Sprite>();
        public IReadOnlyList<Sprite> Sprites => sprites;
#if UNITY_EDITOR
        public void AddAuthoredSprite(Sprite sprite) => sprites.Add(sprite);
#endif
    }
}
