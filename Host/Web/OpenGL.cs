// ────── ╔╗
// ╔═╦╦═╦╦╬╣ OpenGL.cs
// ║║║║╬║╔╣║ Implements WebGL : a WebGL2 (browser canvas) implementation of IOpenGL
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
namespace Nori;
using Ptr = nint;

#region class WebOpenGL ----------------------------------------------------------------------------
/// <summary>WebGL2 implementation of the IOpenGL interface</summary>
/// The WebGL2 context lives on the render canvas, created by NoriHost.js during WebHost.Init.
/// Painting is driven by requestAnimationFrame: Redraw() marks the frame dirty JS-side, and on
/// the next animation frame the JS module calls back into WebHost, which invokes OnPaint with
/// the framebuffer size (exactly the contract the other hosts follow).
///
/// GetGLProcAddress cannot be implemented on this host: WebGL2 functions are JavaScript
/// methods, not native entry points, so there are no function pointers to hand out. The Lux
/// renderer accesses GL through the static GL facade, which on this platform will be backed
/// by [JSImport] bindings rather than by pointers obtained here (the Lux WebGL2 port).
class WebGL : IOpenGL {
   // This gets set by the renderer; invoked from the rAF callback via WebHost.OnAnimationFrame
   public Action<int, int> OnPaint { set => WebHost.OnPaint = value; }

   public Ptr GetGLProcAddress (string name)
      => throw new NotSupportedException (
            $"WebGL2 has no proc addresses ('{name}') - the GL facade uses the JSImport backend on browser-wasm");

   // Marks the canvas dirty; NoriHost.js schedules one requestAnimationFrame and calls
   // back into WebHost.OnAnimationFrame, which runs OnPaint
   public void Redraw () => WebHost.RequestRender ();

   // Shows / hides the mouse cursor over the render canvas
   public bool CursorVisible { set => WebHost.SetCursorVisible (value); }

   // Device-pixel-ratio of the display (browser equivalent of DPI scaling)
   public float DPIScale => Lib.Testing ? 1 : WebHost.PixelRatio;
}
#endregion
