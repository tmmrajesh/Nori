// ────── ╔╗
// ╔═╦╦═╦╦╬╣ Host.cs
// ║║║║╬║╔╣║ Implements WebHost, which provides browser (Blazor WASM) implementations of interfaces
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
namespace Nori;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

#region class WebHost ------------------------------------------------------------------------------
/// <summary>The WebHost class provides browser-specific implementations of some interfaces</summary>
/// This is the third Nori host (after WPF and GLFW): it plugs WebDispatcher, WebGL,
/// WebKeyboard and WebMouse into the Hub whiteboard. The platform layer on the JS side is
/// NoriHost.js (shipped as a static web asset of this library), which owns the render canvas:
/// it creates the WebGL2 context, captures DOM pointer / wheel / keyboard events, and runs the
/// requestAnimationFrame paint loop. A Blazor application uses it like this:
///
///    Lib.Init ();
///    await WebHost.Init ("canvasId", OnReady);   // canvas element must already be in the DOM
///    static void OnReady () => Lux.UIScene = new DemoScene ();
///
/// When Init's task completes, Hub.OpenGL, Hub.Dispatcher, Hub.Keyboard and Hub.Mouse are set
/// up, the WebGL2 context exists on the canvas, and onReady has been called.
[SupportedOSPlatform ("browser")]
public static partial class WebHost {
   // Methods ------------------------------------------------------------------
   /// <summary>Init should be called once to initialize the WebHost on a given canvas</summary>
   /// bgrdColor is the application's background color: the canvas is cleared to it whenever
   /// the drawing buffer is reset (context creation, resize) so it never flashes black before
   /// the next painted frame. Defaults to Lux's no-scene background, Color4.Gray (96).
   public static async Task Init (string canvasId, Action onReady, Color4? bgrdColor = null) {
      if (mInited) return;
      mInited = true;
      await JSHost.ImportAsync (Mod, "../_content/Nori.Host.Web/NoriHost.js");
      // Lux's GL facade binds to the "nori-gl" module ([JSImport]s in Nori.Lux/OpenGLWeb.cs);
      // it must be loaded before any Lux code touches GL
      await JSHost.ImportAsync ("nori-gl", "../_content/Nori.Host.Web/NoriGL.js");
      var bgrd = bgrdColor ?? Color4.Gray (96);
      if (!JSInit (canvasId, bgrd.R / 255.0, bgrd.G / 255.0, bgrd.B / 255.0))
         throw new NotSupportedException ("WebGL2 is not available in this browser");

      Hub.Dispatcher = new WebDispatcher ();
      Hub.OpenGL = new WebGL ();
      Hub.Keyboard = mKeyboard = new WebKeyboard ();
      Hub.Mouse = mMouse = new WebMouse ();
      SynchronizationContext.SetSynchronizationContext (new WebSyncContext (Hub.Dispatcher));

      mPixelRatio = (float)JSPixelRatio ();
      JSAttach (OnPointer, OnWheel, OnKey, OnChar, OnFrame);
      onReady ();
   }
   static bool mInited;
   static WebMouse? mMouse;
   static WebKeyboard? mKeyboard;

   /// <summary>The devicePixelRatio of the display (browser equivalent of DPI scale)</summary>
   public static float PixelRatio => mPixelRatio;
   static float mPixelRatio = 1;

   // Connectors used by WebGL ----------------------------------------------
   internal static Action<int, int>? OnPaint;
   internal static void RequestRender () => JSRequestRender ();
   internal static void SetCursorVisible (bool visible) => JSSetCursorVisible (visible);

   // Event ingestion ------------------------------------------------------------
   // DOM events arrive from NoriHost.js encoded into at most 3 numbers per callback
   // (the JS function-marshalling limit), and are decoded and pushed into the Hub
   // implementations here.

   // code = type | button << 3 | modifiers << 6, with type: 0=down, 1=move, 2=up, 3=enter, 4=leave
   static void OnPointer (double code, double x, double y) {
      int c = (int)code, type = c & 7;
      var button = (EMouseButton)((c >> 3) & 7);
      var mods = (EKeyModifier)((c >> 6) & 7);
      var pos = new Vec2S ((int)x, (int)y);
      mKeyboard!.SetModifiers (mods);
      switch (type) {
         case 0: mMouse!.PushClick (button, pos, mods, EKeyState.Pressed); break;
         case 1: mMouse!.PushMove (pos); break;
         case 2: mMouse!.PushClick (button, pos, mods, EKeyState.Released); break;
         case 3: mMouse!.PushEnter (true); break;
         case 4: mMouse!.PushEnter (false); break;
      }
   }

   static void OnWheel (double delta, double x, double y)
      => mMouse!.PushWheel ((int)delta, new Vec2S ((int)x, (int)y));

   // flags = state | modifiers << 2, with state: 0=released, 1=pressed, 2=repeat
   static void OnKey (double key, double flags) {
      int f = (int)flags;
      var state = (EKeyState)(f & 3);
      var mods = (EKeyModifier)((f >> 2) & 7);
      mKeyboard!.PushKey ((EKey)(int)key, mods, state);
   }

   static void OnChar (double code) {
      foreach (var ch in char.ConvertFromUtf32 ((int)code)) mKeyboard!.PushChar (ch);
   }

   // The requestAnimationFrame callback: w, h is the framebuffer size in device pixels
   static void OnFrame (double w, double h) => OnPaint?.Invoke ((int)w, (int)h);

   // JS bindings ----------------------------------------------------------------
   const string Mod = "nori-host";

   [JSImport ("init", Mod)] internal static partial bool JSInit (string canvasId, double r, double g, double b);
   [JSImport ("requestRender", Mod)] internal static partial void JSRequestRender ();
   [JSImport ("setCursorVisible", Mod)] internal static partial void JSSetCursorVisible (bool visible);
   [JSImport ("pixelRatio", Mod)] internal static partial double JSPixelRatio ();

   [JSImport ("attach", Mod)]
   static partial void JSAttach (
      [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>] Action<double, double, double> onPointer,
      [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>] Action<double, double, double> onWheel,
      [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Action<double, double> onKey,
      [JSMarshalAs<JSType.Function<JSType.Number>>] Action<double> onChar,
      [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Action<double, double> onFrame);
}
#endregion
