using System.Collections.Generic;
using UnityEngine;

namespace LochyIGorzala.Helpers
{
    /// <summary>
    /// Utility class for extracting individual sprites from sprite sheet textures.
    /// Works with 32x32 pixel grid-based sprite sheets.
    ///
    /// IMPORTANT — Sprite cache:
    /// Extracted sprites are cached in a static dictionary keyed by (texture, col, row, tileSize).
    /// Without this cache, Sprite.Create was invoked for every tile/enemy/item on every scene load,
    /// producing thousands of unreferenced Sprite instances that accumulated across Dungeon↔Combat
    /// transitions and eventually froze the editor during scene unload (see decisions.md — "Asset
    /// leak pomimo UnloadUnusedAssets"). With the cache, the same coordinate on the same sheet
    /// always returns the same Sprite instance for the entire editor session.
    /// </summary>
    public static class SpriteSheetHelper
    {
        public const int DefaultTileSize = 32;
        public const float DefaultPPU = 32f;

        // Key: (sheet instanceID, col, row, tileSize) — value: the single shared Sprite instance.
        // Using InstanceID instead of the Texture2D reference lets the cache survive even if the
        // sprite sheet asset is re-imported (a new instance replaces the old one cleanly).
        private readonly struct SpriteKey : System.IEquatable<SpriteKey>
        {
            public readonly int SheetId;
            public readonly int Col;
            public readonly int Row;
            public readonly int TileSize;

            public SpriteKey(int sheetId, int col, int row, int tileSize)
            { SheetId = sheetId; Col = col; Row = row; TileSize = tileSize; }

            public bool Equals(SpriteKey o) =>
                SheetId == o.SheetId && Col == o.Col && Row == o.Row && TileSize == o.TileSize;

            public override bool Equals(object o) => o is SpriteKey k && Equals(k);

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = SheetId;
                    h = h * 397 + Col;
                    h = h * 397 + Row;
                    h = h * 397 + TileSize;
                    return h;
                }
            }
        }

        private static readonly Dictionary<SpriteKey, Sprite> _cache = new Dictionary<SpriteKey, Sprite>(256);

        /// <summary>
        /// Extracts a single sprite from a sprite sheet by grid column and row.
        /// Row 0 = top row of the image.
        /// Cached — repeated calls with the same arguments return the same Sprite instance.
        /// </summary>
        public static Sprite ExtractSprite(Texture2D sheet, int col, int row, int tileSize = DefaultTileSize)
        {
            if (sheet == null)
            {
                Debug.LogError("SpriteSheetHelper: sheet texture is null.");
                return null;
            }

            var key = new SpriteKey(sheet.GetHashCode(), col, row, tileSize);
            if (_cache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            float x = col * tileSize;
            float yFromTop = row * tileSize;
            float yFromBottom = sheet.height - yFromTop - tileSize;

            // Clamp to valid texture bounds
            if (x < 0 || x + tileSize > sheet.width || yFromBottom < 0 || yFromBottom + tileSize > sheet.height)
            {
                Debug.LogWarning($"SpriteSheetHelper: Requested sprite at ({col},{row}) is out of bounds for texture {sheet.name} ({sheet.width}x{sheet.height}).");
                return null;
            }

            Rect rect = new Rect(x, yFromBottom, tileSize, tileSize);
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            Sprite fresh = Sprite.Create(sheet, rect, pivot, DefaultPPU);
            if (fresh != null)
                fresh.name = $"{sheet.name}_c{col}_r{row}";

            _cache[key] = fresh;
            return fresh;
        }

        /// <summary>
        /// Extracts a sprite using pixel coordinates directly (from top-left origin).
        /// Also cached — keyed by pixel position.
        /// </summary>
        public static Sprite ExtractSpriteByPixel(Texture2D sheet, int pixelX, int pixelY, int width = DefaultTileSize, int height = DefaultTileSize)
        {
            if (sheet == null) return null;

            // Encode pixel key by reusing SpriteKey with a marker in TileSize: negative values
            // indicate pixel-coord lookups. Safe because DefaultTileSize is always positive.
            var key = new SpriteKey(sheet.GetHashCode(), pixelX, pixelY, -(width * 1000 + height));
            if (_cache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            float yFromBottom = sheet.height - pixelY - height;
            Rect rect = new Rect(pixelX, yFromBottom, width, height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            Sprite fresh = Sprite.Create(sheet, rect, pivot, DefaultPPU);
            if (fresh != null)
                fresh.name = $"{sheet.name}_px{pixelX}_{pixelY}";

            _cache[key] = fresh;
            return fresh;
        }

        /// <summary>
        /// Clears the sprite cache. Only needed for domain reloads or testing —
        /// the cache lives for the entire editor session by design.
        /// </summary>
        public static void ClearCache() => _cache.Clear();
    }
}
