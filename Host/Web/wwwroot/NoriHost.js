// ────── ╔╗
// ╔═╦╦═╦╦╬╣ NoriHost.js
// ║║║║╬║╔╣║ Browser platform layer for Nori.Host.Web: canvas, WebGL2 context, DOM input,
// ╚╩═╩═╩╝╚╝ requestAnimationFrame paint loop. Counterpart of Panel.cs (WPF) / Window.cs (GLFW).

let canvas = null, gl = null, bgrd = [0.375, 0.375, 0.375];
let cbPointer = null, cbWheel = null, cbKey = null, cbChar = null, cbFrame = null;
let rafPending = false;

// Creates (or adopts) the WebGL2 context on the given canvas; false if unavailable.
// getContext returns the same context object on repeated calls, so application drawing
// code that asks for the webgl2 context of this canvas shares it with the host.
// r, g, b (0..1) is the application's background color, used to clear the buffer on
// resize so the canvas never shows black before the next painted frame.
export function init (canvasId, r, g, b) {
   bgrd = [r, g, b];
   canvas = document.getElementById (canvasId);
   // stencil defaults to false in WebGL - Lux needs it (TriFanStencil / TriFanCover fills)
   gl = canvas.getContext ("webgl2", { antialias: true, alpha: false, depth: true, stencil: true, preserveDrawingBuffer: false });
   if (!gl) return false;
   new ResizeObserver (resize).observe (canvas);
   resize ();
   return true;
}

export function pixelRatio () { return window.devicePixelRatio || 1; }

// The WebGL2 context, shared with the NoriGL.js module (Lux's GL backend)
export function getGL () { return gl; }

export function setCursorVisible (visible) { if (canvas) canvas.style.cursor = visible ? "" : "none"; }

// Marks the frame dirty: schedules one requestAnimationFrame that calls the paint callback
export function requestRender () {
   if (rafPending || !gl) return;
   rafPending = true;
   requestAnimationFrame (() => {
      rafPending = false;
      if (cbFrame) cbFrame (canvas.width, canvas.height);
   });
}

function resize () {
   const dpr = window.devicePixelRatio || 1;
   const w = Math.max (1, Math.round (canvas.clientWidth * dpr));
   const h = Math.max (1, Math.round (canvas.clientHeight * dpr));
   if (canvas.width !== w || canvas.height !== h) {
      canvas.width = w; canvas.height = h;
      // Resetting width/height zeroes the drawing buffer, which composites opaque black
      // (alpha:false context) - clear to the app's background color so the canvas never
      // shows black between the resize and the next painted frame (notably the whole
      // app-startup stretch)
      gl.clearColor (bgrd[0], bgrd[1], bgrd[2], 1); gl.clear (gl.COLOR_BUFFER_BIT);
   }
   requestRender ();
}

// ── Input capture ─────────────────────────────────────────────────────────────
// Pointer events are canvas-scoped; keyboard events are window-scoped (like a desktop app
// main window) but skip events targeted at input elements so form fields keep working.
// Encodings (must match WebHost.cs): pointer code = type | button << 3 | mods << 6 with
// type 0=down 1=move 2=up 3=enter 4=leave; key flags = state | mods << 2.

export function attach (onPointer, onWheel, onKey, onChar, onFrame) {
   cbPointer = onPointer; cbWheel = onWheel; cbKey = onKey; cbChar = onChar; cbFrame = onFrame;
   const dpr = () => window.devicePixelRatio || 1;
   const pos = e => {
      const r = canvas.getBoundingClientRect ();
      return [(e.clientX - r.left) * dpr (), (e.clientY - r.top) * dpr ()];
   };
   const mods = e => (e.shiftKey ? 1 : 0) | (e.ctrlKey ? 2 : 0) | (e.altKey ? 4 : 0);
   const button = e => e.button === 2 ? 1 : e.button === 1 ? 2 : 0;   // DOM → EMouseButton (L,R,M)
   const push = (type, e) => {
      const [x, y] = pos (e);
      cbPointer (type | (button (e) << 3) | (mods (e) << 6), x, y);
   };

   // ── Touch gestures ──────────────────────────────────────────────────────────
   // Touch is translated onto the same mouse pipeline the app already speaks, so the
   // C# side needs no changes: tap = left click, one-finger drag = left drag (commits
   // only after DRAG_PX so a settling second finger never fires stray clicks into a
   // drawing tool), two-finger drag = middle-drag pan of the midpoint, pinch = wheel
   // zoom ticks at the midpoint. Mouse / pen pointers bypass this entirely.
   const DRAG_PX = 12;                 // css px of travel before a touch becomes a left-drag
   const ZOOM_STEP = 1.1;              // pinch distance ratio per wheel tick
   const T = { pts: new Map (), state: "idle", p1: 0, p2: 0, sx: 0, sy: 0, mx: 0, my: 0, dist: 0 };
   const devPos = (cx, cy) => {
      const r = canvas.getBoundingClientRect (), d = dpr ();
      return [(cx - r.left) * d, (cy - r.top) * d];
   };
   const emit = (type, btn, cx, cy) => cbPointer (type | (btn << 3), ...devPos (cx, cy));
   const midDist = () => {
      const a = T.pts.get (T.p1), b = T.pts.get (T.p2);
      return [(a.x + b.x) / 2, (a.y + b.y) / 2, Math.hypot (a.x - b.x, a.y - b.y)];
   };
   const touchDown = e => {
      T.pts.set (e.pointerId, { x: e.clientX, y: e.clientY });
      if (T.state === "idle" && T.pts.size === 1) {
         T.state = "pending"; T.p1 = e.pointerId; T.sx = e.clientX; T.sy = e.clientY;
      } else if (T.state === "pending" && T.pts.size === 2) {
         T.state = "multi"; T.p2 = e.pointerId;
         [T.mx, T.my, T.dist] = midDist ();
         emit (1, 0, T.mx, T.my);      // move first so the app's cursor is at the midpoint
         emit (0, 2, T.mx, T.my);      // middle-down = start pan
      }
      // 3rd+ fingers, and fingers landing mid-drag or after a gesture ended: ignored
   };
   const touchMove = e => {
      const p = T.pts.get (e.pointerId);
      if (!p) return;
      p.x = e.clientX; p.y = e.clientY;
      if (T.state === "pending" && e.pointerId === T.p1) {
         if (Math.hypot (p.x - T.sx, p.y - T.sy) < DRAG_PX) return;
         T.state = "single";           // deferred left-down at the start point, then catch up
         emit (1, 0, T.sx, T.sy); emit (0, 0, T.sx, T.sy); emit (1, 0, p.x, p.y);
      } else if (T.state === "single" && e.pointerId === T.p1) emit (1, 0, p.x, p.y);
      else if (T.state === "multi" && (e.pointerId === T.p1 || e.pointerId === T.p2)) {
         const [mx, my, dist] = midDist ();
         emit (1, 0, mx, my);          // middle is down: moving the midpoint pans
         let ticks = 0;                // pinch: one wheel tick per ZOOM_STEP crossing
         while (dist > T.dist * ZOOM_STEP) { ticks++; T.dist *= ZOOM_STEP; }
         while (dist < T.dist / ZOOM_STEP) { ticks--; T.dist /= ZOOM_STEP; }
         if (ticks) cbWheel (ticks, ...devPos (mx, my));
         T.mx = mx; T.my = my;
      }
   };
   const touchUp = e => {
      if (T.pts.delete (e.pointerId)) {
         if (T.state === "pending" && e.pointerId === T.p1) {         // tap = left click
            emit (1, 0, T.sx, T.sy); emit (0, 0, T.sx, T.sy); emit (2, 0, T.sx, T.sy);
         } else if (T.state === "single" && e.pointerId === T.p1)
            emit (2, 0, e.clientX, e.clientY);                        // end left-drag
         else if (T.state === "multi" && (e.pointerId === T.p1 || e.pointerId === T.p2))
            { emit (2, 2, T.mx, T.my); T.state = "dead"; }            // end pan; eat the rest
      }
      if (T.pts.size === 0) T.state = "idle";
   };

   canvas.addEventListener ("pointerdown", e => {
      // Synthetic pointers (dispatched by the smoke test) are not 'active' and cannot be captured
      try { canvas.setPointerCapture (e.pointerId); } catch { }
      if (e.pointerType === "touch") touchDown (e); else push (0, e);
      e.preventDefault ();
   });
   canvas.addEventListener ("pointermove", e => e.pointerType === "touch" ? touchMove (e) : push (1, e));
   canvas.addEventListener ("pointerup", e => e.pointerType === "touch" ? touchUp (e) : push (2, e));
   canvas.addEventListener ("pointercancel", e => { if (e.pointerType === "touch") touchUp (e); });
   // Enter/leave stay mouse-only: touch taps would spuriously flip the hover state
   canvas.addEventListener ("pointerenter", e => { if (e.pointerType !== "touch") push (3, e); });
   canvas.addEventListener ("pointerleave", e => { if (e.pointerType !== "touch") push (4, e); });
   canvas.addEventListener ("contextmenu", e => e.preventDefault ());
   canvas.addEventListener ("wheel", e => {
      const [x, y] = pos (e);
      // Normalize to wheel 'ticks' (+up / -down), matching the other hosts
      cbWheel (e.deltaY < 0 ? 1 : -1, x, y);
      e.preventDefault ();
   }, { passive: false });

   const isFormTarget = e => {
      const t = e.target;
      return t && (t.tagName === "INPUT" || t.tagName === "TEXTAREA" || t.tagName === "SELECT" || t.isContentEditable);
   };
   window.addEventListener ("keydown", e => {
      if (isFormTarget (e)) return;
      const k = KEYMAP[e.code];
      if (k !== undefined) cbKey (k, (e.repeat ? 2 : 1) | (mods (e) << 2));
      if (e.key.length === 1) cbChar (e.key.codePointAt (0));
      // Keep browser shortcuts like F5 / Ctrl+R; swallow keys apps rely on (Tab, arrows, Backspace)
      if (["Tab", "Backspace", "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", " "].includes (e.key))
         e.preventDefault ();
   });
   window.addEventListener ("keyup", e => {
      if (isFormTarget (e)) return;
      const k = KEYMAP[e.code];
      if (k !== undefined) cbKey (k, 0 | (mods (e) << 2));
   });
}

// DOM KeyboardEvent.code → Nori EKey (GLFW key codes)
const KEYMAP = (() => {
   const m = {};
   for (let i = 0; i < 26; i++) m["Key" + String.fromCharCode (65 + i)] = 65 + i;   // A..Z
   for (let i = 0; i < 10; i++) { m["Digit" + i] = 48 + i; m["Numpad" + i] = 320 + i; }
   for (let i = 1; i <= 12; i++) m["F" + i] = 289 + i;
   Object.assign (m, {
      Space: 32, Quote: 39, Comma: 44, Minus: 45, Period: 46, Slash: 47,
      Semicolon: 59, Equal: 61, BracketLeft: 91, Backslash: 92, BracketRight: 93,
      Backquote: 96, Escape: 256, Enter: 257, Tab: 258, Backspace: 259, Insert: 260,
      Delete: 261, ArrowRight: 262, ArrowLeft: 263, ArrowDown: 264, ArrowUp: 265,
      PageUp: 266, PageDown: 267, Home: 268, End: 269, CapsLock: 280, ScrollLock: 281,
      NumLock: 282, PrintScreen: 283, Pause: 284, NumpadDecimal: 330, NumpadDivide: 331,
      NumpadMultiply: 332, NumpadSubtract: 333, NumpadAdd: 334, NumpadEnter: 335,
      NumpadEqual: 336, ShiftLeft: 340, ControlLeft: 341, AltLeft: 342, MetaLeft: 343,
      ShiftRight: 344, ControlRight: 345, AltRight: 346, MetaRight: 347, ContextMenu: 348,
   });
   return m;
})();
