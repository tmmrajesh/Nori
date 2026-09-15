# FontBake

Bakes `TypeFace` glyph atlases (`.atlas` files) so Lux can render screen-space text on
browser-wasm, where native FreeType is unavailable.

## How it works

On the desktop, `Lux.Text` / `Text2D` / `Text3D` rasterize glyphs at runtime through
`TypeFace`, which P/Invokes `freetype.dll`. The browser can't load native code, so the
bake moves that rasterization offline — the runtime result is pixel-identical to desktop
text because it *is* the desktop rasterization, just done ahead of time:

1. **Bake (this tool, desktop only).** For each font × each pixel-size, construct a
   `TypeFace` the normal way — FreeType rasterizes every glyph into the linear,
   8192-px-wide coverage texture with gamma correction (default 1.5). Then
   `TypeFace.SaveAtlas` serializes the whole thing (format tag `NGA1`):
   - pixel size, line height, ascender/descender
   - the full `Metrics[]` table — rows, columns, bearings, advance, texture offset per glyph
   - the char → glyph-index map
   - the *nonzero* kerning pairs (sparse: a few thousand of ~1.7M possible pairs)
   - the gamma-corrected coverage texture bytes

2. **Ship.** The `.atlas` files land in `Wad/GL/Fonts` next to the TTFs and travel to
   the browser inside `Nori.wad`. They are compact (86–502 KB each) and only one is
   fetched at runtime.

3. **Load (browser, fully managed).** `TypeFace.LoadAtlas` reconstructs a working
   `TypeFace` from the bytes — no FreeType. `Measure`, `GetCharsPos`, `GetMetrics`,
   `GetKerning` are pure table lookups; the existing `BuildTexture` uploads the texture
   (as `TEXTURE_2D` + `texelFetch` in the GLSL ES shaders, replacing the desktop's
   `TEXTURE_RECTANGLE`).

4. **Size selection.** `TypeFace.Default` computes the wanted size as `9 × DPIScale`
   and loads the nearest entry from `TypeFace.BakedSizes = [9, 11, 14, 18, 27]` — these
   cover devicePixelRatio 1, 1.25, 1.5, 2 and 3. Baking a new size therefore also means
   adding it to `BakedSizes` if `TypeFace.Default` should ever pick it.

The trade-off, chosen deliberately: the font set is frozen at bake time. New font or
new size → run FontBake again and rebuild the wad.

## Usage

```
FontBake [-o outDir] [-s size,size,...] [font ...]
```

- **font** — one of, tried in this order:
  - a path to a `.ttf`/`.otf` file (`C:\Fonts\Custom.ttf`)
  - a wad font name (`Roboto-Regular` → `nori:GL/Fonts/Roboto-Regular.ttf`)
  - the family name of an installed font (`"Segoe UI"`), resolved through the Windows
    font registry — per-user fonts (`HKCU\…\Fonts`, full paths) first, then machine-wide
    (`HKLM\…\Fonts`, filenames relative to `C:\Windows\Fonts`)

  With no fonts given, bakes the two wad fonts Pix ships: `Roboto-Regular` and
  `RobotoMono-Regular`.
- **-s** — pixel sizes to bake (default `TypeFace.BakedSizes`).
- **-o** — output directory (default `<DevRoot>/Wad/GL/Fonts`).

Examples:

```
FontBake                             # rebake the shipped fonts at all BakedSizes
FontBake "Segoe UI"                  # bake an installed font at all BakedSizes
FontBake -s 12,16 -o C:\Tmp Consolas # custom sizes, custom output dir
```

Output files are named `{Name}-{size}.atlas`, with spaces stripped from installed-font
family names (`Segoe UI` → `SegoeUI-14.atlas`).

## Caveats

- **Must run on the desktop** — the bake itself uses `freetype.dll` (the exe builds to
  `Bin\` beside it).
- **`.ttc` collections** (Cambria, many CJK fonts) contain several faces; only the
  first is baked (the tool warns).
- **Wiring a new face at runtime** — `TypeFace.Default` hardcodes
  `Roboto-Regular-{size}.atlas` on the browser. Any other baked face must be loaded
  explicitly via `TypeFace.LoadAtlas (Lib.ReadBytes ("nori:GL/Fonts/…"))`.
- **Licensing** — a baked atlas embeds the font's rasterized glyphs and ships them to
  every browser client. Fonts bundled with Windows (Segoe UI in particular) are licensed
  for use on Windows devices, not for redistribution in a web app. Roboto is
  Apache-licensed, which is why it is the shipped default. Check before baking anything
  else into the wad.
