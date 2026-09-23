// ────── ╔╗
// ╔═╦╦═╦╦╬╣ DemoLib.cs
// ║║║║╬║╔╣║ Startup helper for the demo applications: Nori init + the "demo:" data drive
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
using Nori;
namespace DemoScenes;

#region class DemoLib ------------------------------------------------------------------------------
/// <summary>Initializes Nori and mounts the demo data as the "demo:" virtual drive</summary>
/// The scenes load their inputs (DXF, STEP, T3X ...) by names like "demo:Tess/J.dxf" rather
/// than by absolute paths, so the same scene code runs on the desktop and in a browser:
/// - Desktop: "demo:" maps to the Demos/Data folder of the Nori repository
/// - Browser: the application fetches Demo.wad (a zip of that folder, built by WebDemo) over
///   HTTP and registers a MemZipStmLocator for "demo:" itself before calling Init
public static class DemoLib {
   public static void Init () {
      if (sInited) return;
      sInited = true;
      if (!OperatingSystem.IsBrowser ())
         Lib.Register (new FileStmLocator ("demo:", $"{Lib.DevRoot}/Demos/Data/"));
      Lib.Init ();
   }
   static bool sInited;
}
#endregion
