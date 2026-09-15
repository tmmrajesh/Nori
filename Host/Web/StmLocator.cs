// ────── ╔╗
// ╔═╦╦═╦╦╬╣ StmLocator.cs
// ║║║║╬║╔╣║ Implements MemZipStmLocator : IStmLocator over an in-memory zip (a fetched wad)
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
namespace Nori;
using System.IO.Compression;

#region class MemZipStmLocator ---------------------------------------------------------------------
/// <summary>Serves virtual-drive resources from a zip archive held in memory</summary>
/// The browser cannot open files from disk, so wads (Nori.wad / Pix.wad) are fetched over
/// HTTP as a byte[] once at startup and mounted with this locator:
///
///    var bytes = await http.GetByteArrayAsync ("Nori.wad");
///    Lib.Register (new MemZipStmLocator ("nori:", bytes));
///
/// This is the browser counterpart of ZipStmLocator (which opens the zip from a file path).
public class MemZipStmLocator (string prefix, byte[] zipData) : IStmLocator {
   // Properties ---------------------------------------------------------------
   public string Prefix => prefix;

   // Methods ------------------------------------------------------------------
   public Stream? Open (string name) {
      if (!name.StartsWith (prefix)) return null;
      mArchive ??= new ZipArchive (new MemoryStream (zipData, writable: false));
      if (mArchive.GetEntry (name[prefix.Length..].Replace ('\\', '/')) is { } ze) {
         // Copy out to a MemoryStream so callers get a seekable stream with a Length
         var ms = new MemoryStream ((int)ze.Length);
         using (var stm = ze.Open ()) stm.CopyTo (ms);
         ms.Position = 0;
         return ms;
      }
      return null;
   }

   ZipArchive? mArchive;
}
#endregion
