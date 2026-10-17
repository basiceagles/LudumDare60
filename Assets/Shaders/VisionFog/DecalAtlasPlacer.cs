using UnityEngine;

using UnityEngine.U2D;

/// <summary>
/// Assigns one sprite of a Unity SpriteAtlas to a decal renderer.
///
/// All decals share the SAME material and SAME atlas texture, so the
/// SRP Batcher can draw every decal in a single batch.
///
/// The only thing that changes between decals is this component's
/// per-instance data (the packed sprite UV rect, the atlas texture and
/// an optional tint), which is written into a MaterialPropertyBlock.
/// MaterialPropertyBlocks do NOT break batching.
///
/// This replaces the need for one material per sprite, which would
/// otherwise produce one draw call per decal.
///
/// Example setup:
///     - One SpriteAtlas packing all decal sprites.
///     - One material using Distance Dither Fog/Decal Projection.
///     - One cube per decal, with this component referencing one sprite.
///
/// The component reads the sprite's PACKED rect (sprite.textureRect,
/// normalized to the atlas size) from the atlas, so the decal always
/// samples the correct sprite regardless of how the atlas packed it
/// (grids, tight packing, padding).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class DecalAtlasPlacer : MonoBehaviour
{
    // ========================================================================
    // SHADER PROPERTY IDs
    // ========================================================================

    private static readonly int DecalTextureID =
        Shader.PropertyToID("_DecalTexture");

    private static readonly int AtlasRectID =
        Shader.PropertyToID("_AtlasRect");

    private static readonly int DecalUVScaleID =
        Shader.PropertyToID("_DecalUVScale");

    private static readonly int TintColorID =
        Shader.PropertyToID("_TintColor");


    // ========================================================================
    // SPRITE (Unity SpriteAtlas)
    // ========================================================================

    [Header("Sprite")]

    [Tooltip(
        "The SpriteAtlas that packs the decal sprites. " +
        "Optional: when empty, the sprite's own texture is used."
    )]
    [SerializeField]
    private SpriteAtlas spriteAtlas;


    [Tooltip(
        "The decal sprite. Its packed position inside the atlas is " +
        "read automatically, so the decal always shows the correct sprite."
    )]
    [SerializeField]
    private Sprite sprite;


    [Tooltip(
        "Per-decal tint and opacity. Set the alpha below 1 to make " +
        "this decal fainter than the shared material's color."
    )]
    [SerializeField]
    private Color tint = new Color(
        1f,
        1f,
        1f,
        1f
    );


    // ========================================================================
    // FALLBACK GRID
    // ========================================================================

    [Header("Fallback Grid (no sprite)")]

    [Tooltip(
        "Used only when Sprite is empty: the raw texture is treated as " +
        "a uniform columns x rows grid, and Tile Index picks one cell."
    )]
    [SerializeField]
    private Vector2Int atlasTiles = new Vector2Int(
        4,
        5
    );


    [Tooltip(
        "Grid cell to use when Sprite is empty, starting at 0 for the " +
        "top-left cell, counting left to right, row by row."
    )]
    [Min(0)]
    [SerializeField]
    private int tileIndex = 0;


    // ========================================================================
    // SPRITE SIZE
    // ========================================================================

    [Header("Sprite Size")]

    [Tooltip(
        "When enabled, the sprite is scaled INSIDE the mesh footprint " +
        "(the mesh keeps its size). Its world size is derived from the " +
        "sprite's pixel dimensions and Pixels Per Unit."
    )]
    [SerializeField]
    private bool applySpriteSize = true;


    [Tooltip(
        "How many sprite pixels equal one world unit. Smaller values make " +
        "the sprite larger inside the mesh."
    )]
    [Min(0.0001f)]
    [SerializeField]
    private float pixelsPerUnit = 100f;


    // ========================================================================
    // UPDATE
    // ========================================================================

    [Header("Update")]

    [Tooltip(
        "Reapply every frame. Keep this enabled when the sprite or tint " +
        "can change at runtime (e.g. animated decals)."
    )]
    [SerializeField]
    private bool updateEveryFrame = false;


    // ========================================================================
    // CACHED STATE
    // ========================================================================

    private Renderer targetRenderer;

    private MaterialPropertyBlock propertyBlock;

    private Texture appliedTexture;

    private Vector4 appliedRect;

    private Vector4 appliedUVScale;

    private Color appliedTint;

    private bool applied;


    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    private void OnEnable()
    {
        AcquireTarget();

        Apply();
    }


    private void Update()
    {
        if (!updateEveryFrame)
            return;

        Apply();
    }


    private void OnValidate()
    {
        // Only apply while the component is active in the scene.
        // Play mode does not call OnValidate, so this never writes the
        // block at runtime.
        if (!isActiveAndEnabled)
            return;

        AcquireTarget();

        Apply();
    }


    private void OnDisable()
    {
        // Release the block so the renderer falls back to the shared
        // material values when the component is disabled.
        Release();
    }


    // ========================================================================
    // TARGET
    // ========================================================================

    private void AcquireTarget()
    {
        if (targetRenderer != null)
            return;

        targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogWarning(
                $"[Decal Atlas] {gameObject.name} has no Renderer. " +
                $"Attach this component to the decal cube.",
                this
            );
        }
    }


    // ========================================================================
    // APPLY
    // ========================================================================
    //
    // Writes the tile rect, atlas texture and tint into a
    // MaterialPropertyBlock so the decal keeps sharing the single
    // atlas material.
    // ========================================================================

    public void Apply()
    {
        if (targetRenderer == null)
            return;


        // ----------------------------------------------------------------
        // Resolve the packed sprite and its UV rect inside the atlas.
        // ----------------------------------------------------------------

        Sprite packedSprite =
            ResolvePackedSprite();

        Texture atlasTexture =
            packedSprite != null
            ? packedSprite.texture
            : null;

        Vector4 atlasRect =
            packedSprite != null
            ? SpriteToVector4(
                packedSprite
            )
            : GridRect();


        // ----------------------------------------------------------------
        // Scale the sprite inside the mesh footprint.
        // ----------------------------------------------------------------

        Vector4 decalUVScale =
            ComputeDecalUVScale(
                packedSprite
            );


        // Skip identical writes (also avoids editor churn while the
        // shared material is being imported).

        if (
            applied
            && appliedTexture == atlasTexture
            && appliedRect == atlasRect
            && appliedUVScale == decalUVScale
            && appliedTint == tint
        )
        {
            return;
        }


        if (propertyBlock == null)
        {
            propertyBlock =
                new MaterialPropertyBlock();
        }


        targetRenderer.GetPropertyBlock(
            propertyBlock
        );


        if (atlasTexture != null)
        {
            propertyBlock.SetTexture(
                DecalTextureID,
                atlasTexture
            );
        }


        propertyBlock.SetVector(
            AtlasRectID,
            atlasRect
        );


        propertyBlock.SetVector(
            DecalUVScaleID,
            decalUVScale
        );


        propertyBlock.SetColor(
            TintColorID,
            tint
        );


        targetRenderer.SetPropertyBlock(
            propertyBlock
        );


        appliedTexture = atlasTexture;

        appliedRect = atlasRect;

        appliedUVScale = decalUVScale;

        appliedTint = tint;

        applied = true;
    }


    // ========================================================================
    // SPRITE SIZE
    // ========================================================================
    //
    // Converts the sprite's pixel dimensions into a world-space size using
    // Pixels Per Unit, then turns it into a UV scale so the sprite occupies
    // that world size INSIDE the decal mesh. The mesh itself is never
    // resized.
    //
    // The shader maps the mesh's world-space XZ footprint (a unit cube
    // scaled by lossyScale) onto the 0..1 sprite UV, so the UV scale is
    // simply meshWorldSize / desiredWorldSize.
    //
    // ========================================================================

    private Vector4 ComputeDecalUVScale(
        Sprite packedSprite
    )
    {
        if (
            !applySpriteSize
            || packedSprite == null
        )
        {
            return new Vector4(
                1f,
                1f,
                0f,
                0f
            );
        }


        // Desired world size of the sprite, from its pixel size and the
        // chosen Pixels Per Unit.

        Vector2 desiredWorld =
            new Vector2(
                packedSprite.bounds.size.x,
                packedSprite.bounds.size.y
            )
            * (
                packedSprite.pixelsPerUnit
                / Mathf.Max(
                    pixelsPerUnit,
                    0.0001f
                )
            );


        // World footprint of the decal mesh. The shader assumes a unit
        // cube in object space, so the footprint is the transform's
        // world scale.

        Vector3 lossyScale =
            transform.lossyScale;

        Vector2 meshWorld =
            new Vector2(
                Mathf.Abs(lossyScale.x),
                Mathf.Abs(lossyScale.z)
            );


        // UV scale that maps the sprite world size onto the mesh.

        return new Vector4(
            meshWorld.x
            / Mathf.Max(
                desiredWorld.x,
                0.0001f
            ),
            meshWorld.y
            / Mathf.Max(
                desiredWorld.y,
                0.0001f
            ),
            0f,
            0f
        );
    }


    // ========================================================================
    // SPRITE RESOLUTION
    // ========================================================================
    //
    // When a Sprite is packed into a SpriteAtlas, Unity redirects the
    // sprite's texture and textureRect to the atlas at runtime, so we can
    // simply use the assigned sprite.
    //
    // The atlas reference is used only to VALIDATE that the sprite is
    // actually packed, and to warn about sprites Unity rotated during
    // packing.
    // ========================================================================

    private Sprite ResolvePackedSprite()
    {
        if (sprite == null)
            return null;


        // ----------------------------------------------------------------
        // Membership check
        // ----------------------------------------------------------------

        if (
            spriteAtlas != null
            && spriteAtlas.GetSprite(
                sprite.name
            ) == null
        )
        {
            Debug.LogWarning(
                $"[Decal Atlas] {gameObject.name}: " +
                $"'{sprite.name}' is not in '{spriteAtlas.name}'. " +
                $"Check the SpriteAtlas objects list.",
                this
            );
        }


        // ----------------------------------------------------------------
        // Rotation check
        // ----------------------------------------------------------------

        if (
            sprite.packed
            && sprite.packingRotation
            != SpritePackingRotation.None
        )
        {
            Debug.LogWarning(
                $"[Decal Atlas] {gameObject.name}: " +
                $"'{sprite.name}' was rotated during packing. " +
                $"Disable 'Enable Rotation' on the SpriteAtlas " +
                $"inspector so the decal renders upright.",
                this
            );
        }


        return sprite;
    }


    // ========================================================================
    // RECT CONVERSION
    // ========================================================================
    //
    // _AtlasRect is the sprite's normalized region inside the atlas:
    //
    //     (x, y) = bottom-left corner in atlas UV space
    //     (z, w) = positive width / height
    //
    // sprite.textureRect is in PIXELS measured from the bottom-left of
    // the atlas, so it is divided by the texture size. For a sprite
    // packed in a SpriteAtlas, sprite.texture is the atlas texture, so
    // this automatically yields the packed rect.
    // ========================================================================

    private Vector4 SpriteToVector4(
        Sprite value
    )
    {
        Texture2D texture =
            value.texture;

        if (
            texture == null
            || texture.width == 0
            || texture.height == 0
        )
        {
            return new Vector4(
                0f,
                0f,
                1f,
                1f
            );
        }


        Rect rect =
            value.textureRect;

        return new Vector4(
            rect.x / texture.width,
            rect.y / texture.height,
            rect.width / texture.width,
            rect.height / texture.height
        );
    }


    // ========================================================================
    // GRID FALLBACK
    // ========================================================================

    private Vector4 GridRect()
    {
        int columns =
            Mathf.Max(
                1,
                atlasTiles.x
            );

        int rows =
            Mathf.Max(
                1,
                atlasTiles.y
            );


        int column =
            tileIndex % columns;

        int row =
            rows
            - 1
            - (tileIndex / columns)
            % rows;


        float tileWidth =
            1f / columns;

        float tileHeight =
            1f / rows;


        return new Vector4(
            column * tileWidth,
            row * tileHeight,
            tileWidth,
            tileHeight
        );
    }


    // ========================================================================
    // RELEASE
    // ========================================================================

    private void Release()
    {
        if (targetRenderer == null)
            return;

        if (propertyBlock == null)
            return;

        targetRenderer.SetPropertyBlock(
            null
        );

        applied = false;
    }


    // ========================================================================
    // EDITOR CONVENIENCE
    // ========================================================================

#if UNITY_EDITOR

    /// <summary>
    /// Writes the atlas texture into the shared material so the material
    /// asset previews and directly-assigned materials show the atlas.
    ///
    /// This modifies the shared material asset. It is intentional and
    /// only runs when you invoke it from the context menu.
    /// </summary>
    [ContextMenu("Assign Atlas Texture To Material")]
    public void AssignAtlasTextureToMaterial()
    {
        if (targetRenderer == null)
        {
            AcquireTarget();
        }

        if (targetRenderer == null)
            return;

        Sprite packed =
            ResolvePackedSprite();

        if (
            packed == null
            || packed.texture == null
        )
        {
            Debug.LogWarning(
                $"[Decal Atlas] {gameObject.name}: " +
                $"cannot resolve an atlas texture.",
                this
            );

            return;
        }


        Material material =
            targetRenderer.sharedMaterial;

        if (material == null)
        {
            Debug.LogWarning(
                $"[Decal Atlas] {gameObject.name}: " +
                $"no shared material to update.",
                this
            );

            return;
        }


        material.SetTexture(
            DecalTextureID,
            packed.texture
        );


        UnityEditor.EditorUtility.SetDirty(
            material
        );
    }

#endif


    // ========================================================================
    // PUBLIC API
    // ========================================================================

    /// <summary>
    /// Switches this decal to a different sprite in the atlas.
    /// </summary>
    public void SetSprite(
        Sprite value
    )
    {
        sprite = value;

        Apply();
    }


    /// <summary>
    /// Changes the per-decal tint / opacity.
    /// </summary>
    public void SetTint(
        Color value
    )
    {
        tint = value;

        Apply();
    }
}
