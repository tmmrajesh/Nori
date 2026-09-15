// ────── ╔╗
// ╔═╦╦═╦╦╬╣ Keyboard.cs
// ║║║║╬║╔╣║ Implements WebKeyboard : a browser implementation of IKeyboard
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
namespace Nori;

#region class WebKeyboard --------------------------------------------------------------------------
/// <summary>Browser implementation of the IKeyboard interface</summary>
/// DOM keydown/keyup events are captured window-wide by NoriHost.js (skipping events targeted
/// at input elements), mapped there from KeyboardEvent.code to the EKey enumeration, and fed
/// into WebHost.OnKey / OnChar. The browser offers no polled key-state API, so Modifiers is
/// maintained from the modifier bits that ride along with every key and pointer event.
class WebKeyboard : IKeyboard {
   // Interface ----------------------------------------------------------------
   public IObservable<KeyInfo> Keys => mKeys;
   public IObservable<char> Chars => mChars;
   public EKeyModifier Modifiers => mModifiers;

   // Implementation -----------------------------------------------------------
   internal void PushKey (EKey key, EKeyModifier mods, EKeyState state)
      { mModifiers = mods; mKeys.Raise (new (key, mods, state)); }
   internal void PushChar (char ch) => mChars.Raise (ch);
   internal void SetModifiers (EKeyModifier mods) => mModifiers = mods;

   EKeyModifier mModifiers;
   readonly WebEvent<KeyInfo> mKeys = new ();
   readonly WebEvent<char> mChars = new ();
}
#endregion
