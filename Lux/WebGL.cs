// ────── ╔╗
// ╔═╦╦═╦╦╬╣ WebGL.cs
// ║║║║╬║╔╣║ WebGL: the WebGL2 backend for the GL facade (browser-wasm only)
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
namespace Nori;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

// The browser counterpart of the unmanaged function pointers in GL: every GL call is routed
// to WebGL2 via [JSImport] bindings into the "nori-gl" JS module (NoriGL.js, shipped as a
// static web asset by Nori.Host.Web and loaded by WebHost.Init before any Lux code runs).
// WebGL2 identifies objects (buffers, programs, textures...) by JS object references, not
// integer names - the JS side keeps handle tables so this interface stays integer-based and
// the GL facade needs no signature changes. Functions that take a data pointer pass the WASM
// linear-memory offset; the JS side reads directly out of the heap (no copies via interop).
[SupportedOSPlatform ("browser")]
internal static partial class WebGL {
   const string Mod = "nori-gl";

   [JSImport ("activeTexture", Mod)] internal static partial void ActiveTexture (int unit);
   [JSImport ("attachShader", Mod)] internal static partial void AttachShader (int program, int shader);
   [JSImport ("bindBuffer", Mod)] internal static partial void BindBuffer (int target, int buffer);
   [JSImport ("bindFramebuffer", Mod)] internal static partial void BindFramebuffer (int target, int buffer);
   [JSImport ("bindRenderbuffer", Mod)] internal static partial void BindRenderbuffer (int target, int buffer);
   [JSImport ("bindTexture", Mod)] internal static partial void BindTexture (int target, int texture);
   [JSImport ("bindVertexArray", Mod)] internal static partial void BindVertexArray (int vao);
   [JSImport ("blendFunc", Mod)] internal static partial void BlendFunc (int src, int dst);
   [JSImport ("bufferData", Mod)] internal static partial void BufferData (int target, int size, int ptr, int usage);
   [JSImport ("bufferSubData", Mod)] internal static partial void BufferSubData (int target, int offset, int size, int ptr);
   [JSImport ("checkFramebufferStatus", Mod)] internal static partial int CheckFramebufferStatus (int target);
   [JSImport ("clear", Mod)] internal static partial void Clear (int mask);
   [JSImport ("clearColor", Mod)] internal static partial void ClearColor (double r, double g, double b, double a);
   [JSImport ("compileShader", Mod)] internal static partial void CompileShader (int shader);
   [JSImport ("createProgram", Mod)] internal static partial int CreateProgram ();
   [JSImport ("createShader", Mod)] internal static partial int CreateShader (int type);
   [JSImport ("deleteBuffer", Mod)] internal static partial void DeleteBuffer (int buffer);
   [JSImport ("deleteTexture", Mod)] internal static partial void DeleteTexture (int texture);
   [JSImport ("deleteVertexArray", Mod)] internal static partial void DeleteVertexArray (int vao);
   [JSImport ("disable", Mod)] internal static partial void Disable (int cap);
   [JSImport ("disableVertexAttribArray", Mod)] internal static partial void DisableVertexAttribArray (int index);
   [JSImport ("drawArrays", Mod)] internal static partial void DrawArrays (int mode, int start, int count);
   [JSImport ("drawBuffers", Mod)] internal static partial void DrawBuffers (int n);
   [JSImport ("drawArraysInstanced", Mod)] internal static partial void DrawArraysInstanced (int mode, int start, int count, int instances);
   [JSImport ("drawElementsBaseVertex", Mod)] internal static partial void DrawElementsBaseVertex (int mode, int count, int type, int ioffset, int baseVertex);
   [JSImport ("drawExpanded", Mod)] internal static partial void DrawExpanded (int vertsPerInstance, int first, int count, int sub);
   [JSImport ("drawQuads", Mod)] internal static partial void DrawQuads (int first, int count);
   [JSImport ("enable", Mod)] internal static partial void Enable (int cap);
   [JSImport ("enableVertexAttribArray", Mod)] internal static partial void EnableVertexAttribArray (int index);
   [JSImport ("finish", Mod)] internal static partial void Finish ();
   [JSImport ("framebufferRenderbuffer", Mod)] internal static partial void FramebufferRenderbuffer (int target, int attachment, int rbo);
   [JSImport ("createBuffer", Mod)] internal static partial int GenBuffer ();
   [JSImport ("createFramebuffer", Mod)] internal static partial int GenFramebuffer ();
   [JSImport ("createRenderbuffer", Mod)] internal static partial int GenRenderbuffer ();
   [JSImport ("createTexture", Mod)] internal static partial int GenTexture ();
   [JSImport ("createVertexArray", Mod)] internal static partial int GenVertexArray ();
   [JSImport ("getActiveAttrib", Mod)] internal static partial string GetActiveAttrib (int program, int index);
   [JSImport ("getActiveUniform", Mod)] internal static partial string GetActiveUniform (int program, int index);
   [JSImport ("getAttribLocation", Mod)] internal static partial int GetAttribLocation (int program, string name);
   [JSImport ("getProgram", Mod)] internal static partial int GetProgram (int program, int pname);
   [JSImport ("getProgramInfoLog", Mod)] internal static partial string GetProgramInfoLog (int program);
   [JSImport ("getShader", Mod)] internal static partial int GetShader (int shader, int pname);
   [JSImport ("getShaderInfoLog", Mod)] internal static partial string GetShaderInfoLog (int shader);
   [JSImport ("getUniformLocation", Mod)] internal static partial int GetUniformLocation (int program, string name);
   [JSImport ("linkProgram", Mod)] internal static partial void LinkProgram (int program);
   [JSImport ("pixelStore", Mod)] internal static partial void PixelStore (int pname, int param);
   [JSImport ("polygonOffset", Mod)] internal static partial void PolygonOffset (double factor, double units);
   [JSImport ("readBuffer", Mod)] internal static partial void ReadBuffer (int attachment);
   [JSImport ("readPixels", Mod)] internal static partial void ReadPixels (int x, int y, int width, int height, int format, int type, int ptr);
   [JSImport ("renderbufferStorage", Mod)] internal static partial void RenderbufferStorage (int format, int cx, int cy);
   [JSImport ("scissor", Mod)] internal static partial void Scissor (int x, int y, int width, int height);
   [JSImport ("shaderSource", Mod)] internal static partial void ShaderSource (int shader, string source);
   [JSImport ("stencilFunc", Mod)] internal static partial void StencilFunc (int func, int value, int mask);
   [JSImport ("stencilOp", Mod)] internal static partial void StencilOp (int sfail, int dpfail, int dppass);
   [JSImport ("texImage2D", Mod)] internal static partial void TexImage2D (int target, int level, int internalFormat, int width, int height, int format, int type, int ptr);
   [JSImport ("texParameter", Mod)] internal static partial void TexParameter (int target, int pname, int param);
   [JSImport ("uniform1f", Mod)] internal static partial void Uniform1f (int location, double f0);
   [JSImport ("uniform2f", Mod)] internal static partial void Uniform2f (int location, double f0, double f1);
   [JSImport ("uniform4f", Mod)] internal static partial void Uniform4f (int location, double f0, double f1, double f2, double f3);
   [JSImport ("uniformMatrix4fv", Mod)] internal static partial void UniformMatrix4fv (int location, bool transpose, int ptr);
   [JSImport ("uniform1i", Mod)] internal static partial void Uniform1i (int location, int n);
   [JSImport ("useProgram", Mod)] internal static partial void UseProgram (int program);
   [JSImport ("vertexAttribIPointer", Mod)] internal static partial void VertexAttribIPointer (int index, int size, int type, int stride, int offset);
   [JSImport ("vertexAttribPointer", Mod)] internal static partial void VertexAttribPointer (int index, int size, int type, bool normalized, int stride, int offset);
   [JSImport ("vertexAttribDivisor", Mod)] internal static partial void VertexAttribDivisor (int index, int divisor);
   [JSImport ("viewport", Mod)] internal static partial void Viewport (int x, int y, int width, int height);
}
