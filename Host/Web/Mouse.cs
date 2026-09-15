// ────── ╔╗
// ╔═╦╦═╦╦╬╣ Mouse.cs
// ║║║║╬║╔╣║ Implements WebMouse : a browser-canvas implementation of IMouse
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
namespace Nori;

#region class WebMouse -----------------------------------------------------------------------------
/// <summary>Browser implementation of the IMouse interface</summary>
/// DOM pointer events are captured by NoriHost.js on the render canvas and funneled into
/// WebHost.OnPointer / OnWheel, which call the Push* methods here. Positions are in device
/// pixels with the canvas top-left as (0,0), +Y down - the same convention as the other hosts.
class WebMouse : IMouse {
   // Interface ----------------------------------------------------------------
   public IObservable<MouseClickInfo> Clicks => mClicks;
   public IObservable<bool> Enter => mEnter;
   public IObservable<Vec2S> Moves => mMoves;
   public IObservable<MouseWheelInfo> Wheel => mWheel;
   public Vec2S Pos => mPos;

   // Implementation -----------------------------------------------------------
   // Called by WebHost when DOM events arrive (always on the UI thread)
   internal void PushClick (EMouseButton button, Vec2S pos, EKeyModifier mods, EKeyState state)
      { mPos = pos; mClicks.Raise (new (button, pos, mods, state)); }
   internal void PushMove (Vec2S pos) { mPos = pos; mMoves.Raise (pos); }
   internal void PushEnter (bool enter) => mEnter.Raise (enter);
   internal void PushWheel (int delta, Vec2S pos) { mPos = pos; mWheel.Raise (new (delta, pos)); }

   Vec2S mPos;
   readonly WebEvent<MouseClickInfo> mClicks = new ();
   readonly WebEvent<bool> mEnter = new ();
   readonly WebEvent<Vec2S> mMoves = new ();
   readonly WebEvent<MouseWheelInfo> mWheel = new ();
}
#endregion

#region class WebEvent<T> --------------------------------------------------------------------------
/// <summary>EventWrapper whose events are fed by the host rather than by a subscription</summary>
/// The DOM listeners in NoriHost.js are attached once for the lifetime of the canvas, so
/// there is nothing to connect or disconnect per-subscriber - Connect is a no-op and the
/// host simply calls Raise for every incoming event.
class WebEvent<T> : EventWrapper<T> {
   public void Raise (T item) => Push (item);
   protected override void Connect (bool connect) { }
}
#endregion
