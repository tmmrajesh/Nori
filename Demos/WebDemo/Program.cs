// ────── ╔╗
// ╔═╦╦═╦╦╬╣ Program.cs
// ║║║║╬║╔╣║ Entry point for WebDemo: the Nori demo scenes in a browser (Blazor WebAssembly + WebGL2)
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
using System.Diagnostics;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using DemoScenes;
using Nori;
namespace WebDemo;

// class Program -----------------------------------------------------------------------------------
/// <summary>The browser counterpart of WPFDemo's startup</summary>
/// Startup order: fetch and mount Nori's resource wad, initialize Nori, then start the Blazor
/// app. The canvas side (WebHost.Init) and the demo data (Demo.wad) come later, in the component
/// that owns the canvas: the canvas has to be in the DOM first, and the demo data is only needed
/// once a scene is picked, so the page should not wait for it.
static class Program {
   static async Task Main (string[] args) {
      var builder = WebAssemblyHostBuilder.CreateDefault (args);
      HttpClient http = new () { BaseAddress = new Uri (builder.HostEnvironment.BaseAddress) };

      // A browser has no disk: Nori's resources (Nori.wad) are fetched as a zip and mounted in
      // memory. On the desktop DemoLib.Init maps "nori:" to the Wad folder instead.
      Lib.Register (new MemZipStmLocator ("nori:", await Wads.Fetch (http, "Nori.wad")));
      DemoLib.Init ();

      builder.Services.AddSingleton (http);
      builder.RootComponents.Add<Main> ("#app");
      await builder.Build ().RunAsync ();
   }
}

// class Wads --------------------------------------------------------------------------------------
/// <summary>Fetches a wad (a zip of a resource folder, packed into wwwroot at build) as bytes</summary>
static class Wads {
   /// <summary>Fetches the named file from the site root and returns its bytes</summary>
   /// Response streaming is turned off for this request: the browser HTTP handler then reads
   /// the body with one arrayBuffer() call rather than pulling it through a ReadableStream in
   /// small chunks, each marshalled into managed code. On the interpreter (no AOT) the chunked
   /// path costs several seconds per megabyte; the one-shot path is close to native speed.
   public static async Task<byte[]> Fetch (HttpClient http, string name) {
      var sw = Stopwatch.StartNew ();
      using var request = new HttpRequestMessage (HttpMethod.Get, name);
      request.SetBrowserResponseStreamingEnabled (false);
      using var response = await http.SendAsync (request);
      response.EnsureSuccessStatusCode ();
      var bytes = await response.Content.ReadAsByteArrayAsync ();
      Console.WriteLine ($"WebDemo: fetched {name} ({bytes.Length / 1024} KB) in {sw.ElapsedMilliseconds} ms");
      return bytes;
   }
}
