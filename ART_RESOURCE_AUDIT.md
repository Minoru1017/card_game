# Art Resource Audit (Code-Reachable)

This report audits art assets that are reachable from runtime code paths in this project, focused on:

- `UiSpriteLibrary` (`Assets/Resources/UiSpriteLibrary.asset`)
- `CardArtLibrary` (`Assets/Resources/CardArtLibrary.asset`)
- Direct fallback sprite load in `TutorialPlotScriptFactory` (`Resources.Load("CardArt/林可的凝視")`)

Sortable dataset:

- `ART_RESOURCE_AUDIT_SORTABLE.csv`

## How To Sort

Open the CSV in Excel / Google Sheets and sort by:

1. `est_rgba32_mib` (desc) to find memory-heavy textures first.
2. `risk_level` to prioritize tuning order.
3. `library_ref_count` to find shared assets with broad impact.

## Top Priority Findings

1. `Assets/Resources/UI/Level background/bay.png`  
   - 1844x853, estimated RGBA32 6.00 MiB, currently `maxTextureSize=2048`.
2. `Assets/Resources/UI/pre-war preview.png`  
   - 1524x883, estimated RGBA32 5.13 MiB, currently `maxTextureSize=2048`.
3. `Assets/UI/CardArt/林可的凝視.jpg` and fallback `Assets/Resources/CardArt/林可的凝視.jpg`  
   - both 1009x1675, estimated RGBA32 6.45 MiB, currently `maxTextureSize=2048`.
4. `Assets/UI/CardArt/e9a50f70-f02c-4e03-9b51-ff34a0de2dec.png`  
   - 1038x1516, estimated RGBA32 6.00 MiB, currently `maxTextureSize=2048`.

## Suggested First Pass (Mobile)

- Set Android/iOS `maxTextureSize` to `1024` for the four top-priority large textures above.
- Set Android/iOS `maxTextureSize` to `512` for difficulty badges and rarity frames.
- Set Android/iOS `maxTextureSize` to `256` for deck thumbnails.

## Notes

- `CardArtLibrary` has 26 entries, but 24 entries currently point to one shared art texture (`Card preset images.png`).
- `CardArtLibrary` deck thumbnails are mostly unassigned (`fileID: 0`) except one entry.
- `UiSpriteLibrary` coach expression sprites are currently unassigned (all 4 fields null).
- `est_rgba32_mib` is an upper-bound estimate from sprite rect size (`width * height * 4` bytes), not final GPU compressed size.
