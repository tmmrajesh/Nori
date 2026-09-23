// ────── ╔╗
// ╔═╦╦═╦╦╬╣ Program.cs
// ║║║║╬║╔╣║ Entry point for WebShell: a Blazor WebAssembly + WebGL2 'hello world' for Nori
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Nori;
namespace WebShell;

// class Program -----------------------------------------------------------------------------------
/// <summary>The browser counterpart of GLShell / WPFDemo's startup</summary>
/// Startup order: fetch and mount Nori's resource wad, initialize Nori.Core, then start the
/// Blazor app. The canvas side (WebHost.Init) happens later, in the component that owns the
/// canvas, since the canvas has to be in the DOM first.
static class Program {
   static async Task Main (string[] args) {
      var builder = WebAssemblyHostBuilder.CreateDefault (args);
      HttpClient http = new () { BaseAddress = new Uri (builder.HostEnvironment.BaseAddress) };

      // On the desktop, Lib.Init finds Nori.wad beside the EXE or uses the Wad folder of the
      // repository. A browser has no disk, so the wad (zipped into wwwroot at build) is fetched
      // as one file and mounted in memory. Lib asks its locators in order, first answer wins.
      Lib.Register (new MemZipStmLocator ("nori:", await http.GetByteArrayAsync ("Nori.wad")));
      Lib.Init ();

      builder.Services.AddSingleton (http);
      builder.RootComponents.Add<Main> ("#app");
      await builder.Build ().RunAsync ();
   }
}
