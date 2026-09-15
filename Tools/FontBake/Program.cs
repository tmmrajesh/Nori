// ────── ╔╗
// ╔═╦╦═╦╦╬╣ Program.cs
// ║║║║╬║╔╣║ FontBake: bakes TypeFace glyph atlases for the browser (where FreeType is unavailable)
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
using Microsoft.Win32;
using Nori;

// On the desktop, Lux rasterizes glyphs at runtime with native FreeType - impossible on
// browser-wasm. This tool runs that same FreeType path once, offline: for each font, at
// each requested pixel-size, it builds a TypeFace (rasterizing every glyph into the
// 8192-wide coverage texture) and SaveAtlas serializes everything the renderer needs -
// metrics, char map, kerning pairs, gamma-corrected texture. At runtime TypeFace.LoadAtlas
// reconstructs a working TypeFace from the .atlas with no native code, and TypeFace.Default
// picks the baked size nearest to 9 x DPIScale. See README.md for details.
//
// Usage: FontBake [-o outDir] [-s size,size,...] [font ...]
//   font   Path to a .ttf/.otf, a wad font ("Roboto-Regular"), or the family name of a
//          font installed on this machine ("Segoe UI"). Default: the two wad fonts.
//   -s     Pixel sizes to bake. Default: TypeFace.BakedSizes (9,11,14,18,27).
//   -o     Output directory. Default: the wad fonts folder, so the atlases ship in Nori.wad.
Lib.Init ();
string outDir = $"{Lib.DevRoot}/Wad/GL/Fonts";
int[] sizes = TypeFace.BakedSizes;
List<string> fonts = [];
for (int i = 0; i < args.Length; i++)
   switch (args[i]) {
      case "-o": outDir = args[++i]; break;
      case "-s": sizes = [.. args[++i].Split (',').Select (int.Parse)]; break;
      default: fonts.Add (args[i]); break;
   }
if (fonts.Count == 0) fonts.AddRange (["Roboto-Regular", "RobotoMono-Regular"]);

foreach (string font in fonts) {
   var (ttf, name) = LoadFont (font);
   foreach (int size in sizes) {
      var tf = new TypeFace (ttf, size);
      string file = Path.Combine (outDir, $"{name}-{size}.atlas");
      using (var fs = File.Create (file)) tf.SaveAtlas (fs);
      Console.WriteLine ($"{file}  ({new FileInfo (file).Length / 1024} KB)");
   }
}

// Resolves a font argument to (TTF bytes, output base-name). Tries, in order: a file on
// disk, a font in the wad (nori:GL/Fonts), a font installed on this machine.
static (byte[] Data, string Name) LoadFont (string font) {
   if (File.Exists (font))
      return (File.ReadAllBytes (font), Path.GetFileNameWithoutExtension (font));
   try { return (Lib.ReadBytes ($"nori:GL/Fonts/{font}.ttf"), font); } catch { }
   if (FindInstalledFont (font) is string path) {
      if (path.EndsWith (".ttc", StringComparison.OrdinalIgnoreCase))
         Console.WriteLine ($"Warning: {path} is a collection; baking only its first face");
      return (File.ReadAllBytes (path), string.Concat (font.Split (' ')));
   }
   throw new Exception ($"Font '{font}' is not a file, not in the wad, and not installed");
}

// Looks up an installed font family in the Windows font registry: per-user fonts first
// (HKCU, values hold full paths), then machine-wide (HKLM, values are relative to
// C:\Windows\Fonts). Value names look like "Segoe UI (TrueType)"; a .ttc housing several
// families lists them all: "MS Gothic & MS UI Gothic (TrueType)".
static string? FindInstalledFont (string family) {
   if (!OperatingSystem.IsWindows ()) return null;
   const string SubKey = @"Software\Microsoft\Windows NT\CurrentVersion\Fonts";
   foreach (var (hive, dir) in new[] { (Registry.CurrentUser, ""), (Registry.LocalMachine, Environment.GetFolderPath (Environment.SpecialFolder.Fonts)) }) {
      using var key = hive.OpenSubKey (SubKey);
      foreach (string value in key?.GetValueNames () ?? []) {
         int n = value.LastIndexOf (" (");
         string names = n >= 0 ? value[..n] : value;
         if (names.Split (" & ").Any (a => a.Equals (family, StringComparison.OrdinalIgnoreCase))) {
            string file = (string)key!.GetValue (value)!;
            return Path.IsPathRooted (file) ? file : Path.Combine (dir, file);
         }
      }
   }
   return null;
}
