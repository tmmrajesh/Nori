// ────── ╔╗
// ╔═╦╦═╦╦╬╣ DemoScene.cs
// ║║║║╬║╔╣║ The 'Welcome to Nori' scene (same as GLShell), drawn through WebGL2
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
using Nori;
namespace WebShell;

// class DemoScene ---------------------------------------------------------------------------------
class DemoScene : Scene2 {
   public DemoScene () {
      // TypeFace.Load picks the nearest pre-baked atlas in the browser (no FreeType there)
      mFace = TypeFace.Load ("Roboto-Regular", (int)(48 * Lux.DPIScale));
      Bound = new Bound2 (0, 0, 100, 50);
      BgrdColor = new Color4 (128, 96, 64);

      string message = "Welcome to Nori.";
      var size = mFace.Measure (message, true);
      int dx = size.Width, dy = size.Height;
      Vec2S cen = new (dx / 2 + dy, dy / 2 + dy);
      var vn1 = new SimpleVN (
         () => (Lux.Color, Lux.TypeFace, Lux.ZLevel) = (new (255, 224, 226, 228), mFace, 1),
         () => Lux.Text (message, new Vec2S (cen.X - dx / 2, cen.Y + dy / 2))
      );

      var vn2 = new SimpleVN (
         () => Lux.UIRect (cen, new Vec2S (size.Width + dy, size.Height + dy), 16, 8, new (255, 64, 66, 68), new (255, 200, 202, 204))
      ) { Streaming = true };
      Root = new GroupVN ([vn1, vn2]);
   }

   TypeFace mFace;
}
