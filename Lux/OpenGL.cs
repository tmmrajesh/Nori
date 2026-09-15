// ────── ╔╗
// ╔═╦╦═╦╦╬╣ GL.cs
// ║║║║╬║╔╣║ Pointers to all the OpenGL functions we use in Lux
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
using System.Text;
using Nori;
using Ptr = nint;
using unsafe GLDEBUGPROC = delegate* unmanaged< uint, uint, uint, uint, int, byte*, void*, void>;

// The GL class maintains unmanaged function pointers to all the OpenGL functions we use. 
// We don't use [DllImport] since not all these functions are actually exported from "opengl32" or 
// the underlying DLL. Some of them are, but others are obtained using GL's built-in loader 
// (such as wglGetProcAddress for Windows platform). We could store these as delegates, but it's
// more efficient to store them as just unmanaged function pointers (no delegates, no marshalling). 
// The flip side is that we have to be very careful to match the function signatures perfectly, 
// but with a very finite function list like we are using, that is not difficult. 
static unsafe class GL {
   // True when running under browser-wasm (WebGL2 backend via WebGL)
   static readonly bool Web = OperatingSystem.IsBrowser ();

   // Select the active texture unit
   public static void ActiveTexture (ETexUnit unit) { if (Web) WebGL.ActiveTexture ((int)unit); else glActiveTexture (unit); }
   static delegate* unmanaged<ETexUnit, void> glActiveTexture;

   // Attach a shader to a shader-pipeline (program)
   public static void AttachShader (HProgram program, HShader shader) { if (Web) WebGL.AttachShader ((int)program, (int)shader); else glAttachShader (program, shader); }
   static delegate* unmanaged<HProgram, HShader, void> glAttachShader;

   // Bind a storage buffer to a buffer target
   public static void BindBuffer (EBufferTarget target, HBuffer buffer) { if (Web) WebGL.BindBuffer ((int)target, (int)buffer); else glBindBuffer (target, buffer); }
   static delegate* unmanaged<EBufferTarget, HBuffer, void> glBindBuffer;

   // Bind a frame buffer for use
   public static void BindFrameBuffer (EFrameBufferTarget target, HFrameBuffer buffer) { if (Web) WebGL.BindFramebuffer ((int)target, (int)buffer); else glBindFramebuffer (target, buffer); }
   static delegate* unmanaged<EFrameBufferTarget, HFrameBuffer, void> glBindFramebuffer;

   // Bind a named texture to a texturing target
   public static void BindTexture (ETexTarget target, HTexture id) { if (Web) WebGL.BindTexture ((int)target, (int)id); else glBindTexture (target, id); }
   static delegate* unmanaged<ETexTarget, HTexture, void> glBindTexture;

   // Bind a render buffer to a target
   public static void BindRenderBuffer (ERenderBufferTarget target, HRenderBuffer buffer) { if (Web) WebGL.BindRenderbuffer ((int)target, (int)buffer); else glBindRenderbuffer (target, buffer); }
   static delegate* unmanaged<ERenderBufferTarget, HRenderBuffer, void> glBindRenderbuffer;

   // Bind a vertex array object (VAO) for use
   public static void BindVertexArray (HVertexArray array) { if (Web) WebGL.BindVertexArray ((int)array); else glBindVertexArray (array); }
   static delegate* unmanaged<HVertexArray, void> glBindVertexArray;

   // Specify pixel arithmetic
   public static void BlendFunc (EBlendFactor src, EBlendFactor dest) { if (Web) WebGL.BlendFunc ((int)src, (int)dest); else glBlendFunc (src, dest); }
   static delegate* unmanaged<EBlendFactor, EBlendFactor, void> glBlendFunc;

   // Allocates and copies data to a buffer object's storage
   public static void BufferData (EBufferTarget target, int size, Ptr data, EBufferUsage usage) { if (Web) WebGL.BufferData ((int)target, size, (int)data, (int)usage); else glBufferData (target, size, data, usage); }
   static delegate* unmanaged<EBufferTarget, int, Ptr, EBufferUsage, void> glBufferData;

   // Check the completeness of the frame buffer
   public static EFrameBufferStatus CheckFrameBufferStatus (EFrameBufferTarget target) { if (Web) return (EFrameBufferStatus)WebGL.CheckFramebufferStatus ((int)target); return glCheckFramebufferStatus (target); }
   static delegate* unmanaged<EFrameBufferTarget, EFrameBufferStatus> glCheckFramebufferStatus;

   // Clear buffers to preset values
   public static void Clear (EBuffer mask) { if (Web) WebGL.Clear ((int)mask); else glClear (mask); }
   static delegate* unmanaged<EBuffer, void> glClear;

   // Specify clear values for the color buffers
   public static void ClearColor (float r, float g, float b, float a) { if (Web) WebGL.ClearColor (r, g, b, a); else glClearColor (r, g, b, a); }
   static delegate* unmanaged<float, float, float, float, void> glClearColor;

   // Compile an OpenGL shader
   public static void CompileShader (HShader hShader) { if (Web) WebGL.CompileShader ((int)hShader); else glCompileShader (hShader); }
   static delegate* unmanaged<HShader, void> glCompileShader;

   // Create an OpenGL program (shader pipeline)
   public static HProgram CreateProgram () { if (Web) return (HProgram)(ulong)WebGL.CreateProgram (); return glCreateProgram (); }
   static delegate* unmanaged<HProgram> glCreateProgram;

   // Create an OpenGL shader (one step of a shader pipeline)
   public static HShader CreateShader (EShader type) { if (Web) return (HShader)(ulong)WebGL.CreateShader ((int)type); return glCreateShader (type); }
   static delegate* unmanaged<EShader, HShader> glCreateShader;

   // Install the debug message callback (no-op on WebGL2, which has no debug-output extension)
   public static void SetDebugOn () {
      if (Web) return;
      Enable (ECap.DebugOutput); Enable (ECap.DebugOutputSynchronous);
      glDebugMessageCallback (&DebugCallback, null);
      DebugMessageControl (ESeverity.DontCare, false);
      DebugMessageControl (ESeverity.High, true);
   }
   static delegate* unmanaged<GLDEBUGPROC,void*,void> glDebugMessageCallback;

   [UnmanagedCallersOnly]
   static void DebugCallback (uint source, uint type, uint id, uint severity, int length, byte* message, void* userParam) {
      string msg = Encoding.UTF8.GetString (new ReadOnlySpan<byte> (message, length));
      Debug.WriteLine (msg);
   }

   static void DebugMessageControl (ESeverity severity, bool enable)
      => glDebugMessageControl (0x1100, 0x1100, severity, 0, null, (byte)(enable ? 1 : 0));
   static delegate* unmanaged<uint, uint, ESeverity, int, uint*, byte, void> glDebugMessageControl;

   // Delete a named buffer object
   public static void DeleteBuffer (HBuffer buffer) { if (Web) WebGL.DeleteBuffer ((int)buffer); else glDeleteBuffers (1, &buffer); }
   static delegate* unmanaged<int, HBuffer*, void> glDeleteBuffers;

   // Delete a texture
   public static void DeleteTexture (HTexture texture) { if (Web) WebGL.DeleteTexture ((int)texture); else glDeleteTextures (1, &texture); }
   static delegate* unmanaged<int, HTexture*, void> glDeleteTextures;

   // Deletes a vertex array object
   public static void DeleteVertexArray (HVertexArray array) { if (Web) WebGL.DeleteVertexArray ((int)array); else glDeleteVertexArrays (1, &array); }
   static delegate* unmanaged<int, HVertexArray*, void> glDeleteVertexArrays;

   // Disable GL capabilities
   public static void Disable (ECap cap) { if (Web) WebGL.Disable ((int)cap); else glDisable (cap); }
   static delegate* unmanaged<ECap, void> glDisable;

   // Disable a vertex attribute array
   public static void DisableVertexAttribArray (int index) { if (Web) WebGL.DisableVertexAttribArray (index); else glDisableVertexAttribArray (index); }
   static delegate* unmanaged<int, void> glDisableVertexAttribArray;

   // Render primitives from array data
   public static void DrawArrays (EMode mode, int start, int count) {
      if (Web) {
         // WebGL2 has no QUADS mode - the JS side emulates it with an index-pattern drawElements
         if (mode == EMode.Quads) WebGL.DrawQuads (start, count);
         else WebGL.DrawArrays ((int)mode, start, count);
      } else glDrawArrays (mode, start, count);
   }
   static delegate* unmanaged<EMode, int, int, void> glDrawArrays;

   // Instanced drawing (multiple instances)
   public static void DrawArraysInstanced (EMode mode, int start, int instances, int count) { if (Web) WebGL.DrawArraysInstanced ((int)mode, start, instances, count); else glDrawArraysInstanced (mode, start, instances, count); }
   static delegate* unmanaged<EMode, int, int, int, void> glDrawArraysInstanced;

   // Draws with the 'expanded' pattern: each set of vertsPerInstance vertices becomes one
   // instance of a 4-vertex triangle strip whose corners the vertex shader positions - sub such
   // strips per set when the shader tessellates (Bezier2D.vert), one otherwise (this is
   // the WebGL2 replacement for the geometry-shader pipelines; see NoriGL.js drawExpanded)
   public static void DrawExpanded (int vertsPerInstance, int first, int count, int sub)
      => WebGL.DrawExpanded (vertsPerInstance, first, count, sub);

   // Indexed drawing from an array (with baseVertex added to each index)
   public static void DrawElementsBaseVertex (EMode mode, int count, EIndexType type, Ptr indices, int baseVertex) { if (Web) WebGL.DrawElementsBaseVertex ((int)mode, count, (int)type, (int)indices, baseVertex); else glDrawElementsBaseVertex (mode, count, type, indices, baseVertex); }
   static delegate* unmanaged<EMode, int, EIndexType, Ptr, int, void> glDrawElementsBaseVertex;

   // Selects the first n color attachments as the draw-buffer set (n > 1 means MRT)
   public static void DrawBuffers (int n) {
      if (Web) { WebGL.DrawBuffers (n); return; }
      uint* bufs = stackalloc uint[n];
      for (int i = 0; i < n; i++) bufs[i] = (uint)EFrameBufferAttachment.Color0 + (uint)i;
      glDrawBuffers (n, bufs);
   }
   static delegate* unmanaged<int, uint*, void> glDrawBuffers;

   // Enable GL capabilities
   public static void Enable (ECap cap) { if (Web) WebGL.Enable ((int)cap); else glEnable (cap); }
   static delegate* unmanaged<ECap, void> glEnable;
   public static void Enable (ECap cap, bool v) { if (v) Enable (cap); else Disable (cap); }

   // Specify that a particular element (specified by glVertexAttribPointer) is in use
   public static void EnableVertexAttribArray (int index) { if (Web) WebGL.EnableVertexAttribArray (index); else glEnableVertexAttribArray (index); }
   static delegate* unmanaged<int, void> glEnableVertexAttribArray;

   // Block until all GL execution is complete
   public static void Finish () { if (Web) WebGL.Finish (); else glFinish (); }
   static delegate* unmanaged<void> glFinish;

   // Attach render-buffer to frame buffer
   public static void FrameBufferRenderBuffer (EFrameBufferTarget ftarget, EFrameBufferAttachment attachment, HRenderBuffer rbo) { if (Web) WebGL.FramebufferRenderbuffer ((int)ftarget, (int)attachment, (int)rbo); else glFramebufferRenderbuffer (ftarget, attachment, ERenderBufferTarget.RenderBuffer, rbo); }
   static delegate* unmanaged<EFrameBufferTarget, EFrameBufferAttachment, ERenderBufferTarget, HRenderBuffer, void> glFramebufferRenderbuffer;

   // Allocate a new data-storage buffer object
   public static HBuffer GenBuffer () { if (Web) return (HBuffer)(ulong)WebGL.GenBuffer (); HBuffer buffer; glGenBuffers (1, &buffer); return buffer; }
   static delegate* unmanaged<int, HBuffer*, void> glGenBuffers;

   // Create a new framebuffer (for render-to-image)
   public static HFrameBuffer GenFrameBuffer () { if (Web) return (HFrameBuffer)(ulong)WebGL.GenFramebuffer (); HFrameBuffer buffer; glGenFramebuffers (1, &buffer); return buffer; }
   static delegate* unmanaged<int, HFrameBuffer*, void> glGenFramebuffers;

   // Create a new render buffer
   public static HRenderBuffer GenRenderBuffer () { if (Web) return (HRenderBuffer)(ulong)WebGL.GenRenderbuffer (); HRenderBuffer buffer; glGenRenderbuffers (1, &buffer); return buffer; }
   static delegate* unmanaged<int, HRenderBuffer*, void> glGenRenderbuffers;

   // Generate texture names
   public static void GenTextures (int n, HTexture* pTex) {
      if (Web) { for (int i = 0; i < n; i++) pTex[i] = (HTexture)(ulong)WebGL.GenTexture (); }
      else glGenTextures (n, pTex);
   }
   static delegate* unmanaged<int, HTexture*, void> glGenTextures;
   public static HTexture GenTexture () { HTexture tex; GenTextures (1, &tex); return tex; }

   // Allocate a new VertexArray object (VAO)
   public static HVertexArray GenVertexArray () { if (Web) return (HVertexArray)(ulong)WebGL.GenVertexArray (); HVertexArray array; glGenVertexArrays (1, &array); return array; }
   static delegate* unmanaged<int, HVertexArray*, void> glGenVertexArrays;

   // Gets information about a program attribute
   public static void GetActiveAttrib (HProgram program, int index, out int size, out EDataType type, out string name, out int location) {
      if (Web) {
         var w = WebGL.GetActiveAttrib ((int)program, index).Split ('|');
         (name, size, type, location) = (w[0], int.Parse (w[1]), (EDataType)uint.Parse (w[2]), int.Parse (w[3]));
         return;
      }
      Span<byte> data = stackalloc byte[256];
      fixed (byte* p = data) {
         glGetActiveAttrib (program, index, 255, out int length, out size, out type, (Ptr)p);
         name = Encoding.UTF8.GetString (data[..length]);
         location = GetAttribLocation (program, name);
      }
   }
   static delegate* unmanaged<HProgram, int, int, out int, out int, out EDataType, Ptr, void> glGetActiveAttrib;

   // <summary>Gets information about a uniform variable
   public static void GetActiveUniform (HProgram program, int index, out int size, out EDataType type, out string name, out int location) {
      if (Web) {
         var w = WebGL.GetActiveUniform ((int)program, index).Split ('|');
         (name, size, type, location) = (w[0], int.Parse (w[1]), (EDataType)uint.Parse (w[2]), int.Parse (w[3]));
         return;
      }
      Span<byte> data = stackalloc byte[256];
      fixed (byte* p = data) {
         glGetActiveUniform (program, index, 255, out int length, out size, out type, (Ptr)p);
         name = Encoding.UTF8.GetString (data[..length]);
         location = GetUniformLocation (program, name);
      }
   }
   static delegate* unmanaged<HProgram, int, int, out int, out int, out EDataType, Ptr, void> glGetActiveUniform;

   // Gets information about an attribute's location
   public static int GetAttribLocation (HProgram program, string name) { if (Web) return WebGL.GetAttribLocation ((int)program, name); return glGetAttribLocation (program, name); }
   static delegate* unmanaged<HProgram, string, int> glGetAttribLocation;

   // Gets a parameter from a program object
   public static int GetProgram (HProgram program, EProgramParam pname) { if (Web) return WebGL.GetProgram ((int)program, (int)pname); int n; glGetProgramiv (program, pname, &n); return n; }
   static delegate* unmanaged<HProgram, EProgramParam, int*, void> glGetProgramiv;

   // Gets the error log for a program
   public static string GetProgramInfoLog (HProgram program) {
      if (Web) return WebGL.GetProgramInfoLog ((int)program);
      int length = GetProgram (program, EProgramParam.InfoLogLength), actual = length;
      if (length <= 1) return "";
      byte[] data = new byte[length];
      fixed (byte* p = data) glGetProgramInfoLog (program, length, &actual, p);
      return Encoding.UTF8.GetString (data);
   }
   static delegate* unmanaged<HProgram, int, int*, byte*, void> glGetProgramInfoLog;

   // Gets some information from a shader
   public static int GetShader (HShader shader, EShaderParam pname) { if (Web) return WebGL.GetShader ((int)shader, (int)pname); int n; glGetShaderiv (shader, pname, &n); return n; }
   static delegate* unmanaged<HShader, EShaderParam, int*, void> glGetShaderiv;

   // Gets the error log for a shader
   public static string GetShaderInfoLog (HShader shader) {
      if (Web) return WebGL.GetShaderInfoLog ((int)shader);
      int length = GetShader (shader, EShaderParam.InfoLogLength), actual = length;
      if (length <= 1) return "";
      byte[] data = new byte[length];
      fixed (byte* p = data) glGetShaderInfoLog (shader, length, &actual, p);
      return Encoding.UTF8.GetString (data);
   }
   static delegate* unmanaged<HShader, int, int*, byte*, void> glGetShaderInfoLog;

   // Gets the location (slot) of a uniform variable
   public static int GetUniformLocation (HProgram program, string name) { if (Web) return WebGL.GetUniformLocation ((int)program, name); return glGetUniformLocation (program, name); }
   static delegate* unmanaged<HProgram, string, int> glGetUniformLocation;

   // Links all the shaders into a single program (shader-pipeline)
   public static void LinkProgram (HProgram program) { if (Web) WebGL.LinkProgram ((int)program); else glLinkProgram (program); }
   static delegate* unmanaged<HProgram, void> glLinkProgram;

   // Map part of a buffer object to client address space.
   // WebGL2 has no buffer mapping at all, so on the web we hand out a pointer into a pinned
   // staging array, and UnmapBuffer pushes that data down with a BufferSubData call. This keeps
   // the StreamBuffer design (map / write / unmap) working unchanged.
   public static Ptr MapBufferRange (EBufferTarget target, int offset, int length, EMapAccess access) {
      if (Web) {
         if (sMapStaging.Length < length) sMapStaging = GC.AllocateArray<byte> (length.RoundUp (65536), pinned: true);
         (sMapTarget, sMapOffset, sMapLength) = ((int)target, offset, length);
         fixed (byte* p = sMapStaging) return (Ptr)p;
      }
      return glMapBufferRange (target, offset, length, access);
   }
   static delegate* unmanaged<EBufferTarget, Ptr, Ptr, EMapAccess, Ptr> glMapBufferRange;
   static byte[] sMapStaging = [];
   static int sMapTarget, sMapOffset, sMapLength;

   // Set up a parameter for patch rendering (no-op on WebGL2: no tessellation stages)
   public static void PatchParameter (EPatchParam pname, int value) { if (Web) return; glPatchParameteri (pname, value); }
   static delegate* unmanaged<EPatchParam, int, void> glPatchParameteri;

   // Set up the sentinel value to signal a primitive-restart.
   // No-op on WebGL2: primitive restart is always on with the fixed index 0xFFFFFFFF,
   // which is exactly the value Lux uses.
   public static void PrimitiveRestartIndex (uint index) { if (Web) return; glPrimitiveRestartIndex (index); }
   static delegate* unmanaged<uint, void> glPrimitiveRestartIndex;

   // Allocates render buffer storage
   public static void RenderBufferStorage (ERenderBufferFormat format, int cx, int cy) { if (Web) WebGL.RenderbufferStorage ((int)format, cx, cy); else glRenderbufferStorage (ERenderBufferTarget.RenderBuffer, format, cx, cy); }
   static delegate* unmanaged<ERenderBufferTarget, ERenderBufferFormat, int, int, void> glRenderbufferStorage;

   // Set up the source code for a shader
   public static void ShaderSource (HShader shader, string source) {
      if (Web) { WebGL.ShaderSource ((int)shader, source); return; }
      byte[] data = Encoding.UTF8.GetBytes (source);
      fixed (byte* p = data) { int len = data.Length; glShaderSource (shader, 1, &p, &len); }
   }
   static delegate* unmanaged<HShader, int, byte**, int*, void> glShaderSource;

   // Set up the stencil function for testing (WebGL2 has no separate front/back via this path;
   // Lux only ever uses FrontAndBack, which maps to the combined stencilFunc)
   public static void StencilFunc (EFace face, EStencilFunc func, int value, uint mask) { if (Web) WebGL.StencilFunc ((int)func, value, unchecked ((int)mask)); else glStencilFuncSeparate (face, func, value, mask); }
   static delegate* unmanaged<EFace, EStencilFunc, int, uint, void> glStencilFuncSeparate;

   // Set up the stencil op for front or back face
   public static void StencilOp (EFace face, EStencilOp sfail, EStencilOp dpfail, EStencilOp dppass) { if (Web) WebGL.StencilOp ((int)sfail, (int)dpfail, (int)dppass); else glStencilOpSeparate (face, sfail, dpfail, dppass); }
   static delegate* unmanaged<EFace, EStencilOp, EStencilOp, EStencilOp, void> glStencilOpSeparate;

   // Selects the color attachment that ReadPixels reads from
   public static void ReadBuffer (EFrameBufferAttachment attachment) { if (Web) WebGL.ReadBuffer ((int)attachment); else glReadBuffer (attachment); }
   static delegate* unmanaged<EFrameBufferAttachment, void> glReadBuffer;

   // Read a block of pixels from the frame buffer
   public static void ReadPixels (int x, int y, int width, int height, EPixelFormat format, EPixelType type, Ptr pixels) { if (Web) WebGL.ReadPixels (x, y, width, height, (int)format, (int)type, (int)pixels); else glReadPixels (x, y, width, height, format, type, pixels); }
   static delegate* unmanaged<int, int, int, int, EPixelFormat, EPixelType, Ptr, void> glReadPixels;
   public static void ReadPixels<T> (int x, int y, int width, int height, EPixelFormat format, EPixelType ptype, T[] data) where T : struct {
      GCHandle pixelptr = GCHandle.Alloc (data, GCHandleType.Pinned);
      try { ReadPixels (x, y, width, height, format, ptype, pixelptr.AddrOfPinnedObject ()); } finally { pixelptr.Free (); }
   }

   // Set pixel storage modes
   public static void PixelStore (EPixelStoreParam pname, int param) { if (Web) WebGL.PixelStore ((int)pname, param); else glPixelStorei (pname, param); }
   static delegate* unmanaged<EPixelStoreParam, int, void> glPixelStorei;

   // Set the scale and units used to calculate depth values
   public static void PolygonOffset (float factor, float units) { if (Web) WebGL.PolygonOffset (factor, units); else glPolygonOffset (factor, units); }
   static delegate* unmanaged<float, float, void> glPolygonOffset;

   // Define the scissor box
   public static void Scissor (int x, int y, int width, int height) { if (Web) WebGL.Scissor (x, y, width, height); else glScissor (x, y, width, height); }
   static delegate* unmanaged<int, int, int, int, void> glScissor;

   // Specify a two-dimensional texture image
   public static void TexImage2D (ETexTarget target, int level, EPixelInternalFormat publicformat, int width, int height, int border, EPixelFormat format, EPixelType type, void* pixels) {
      if (Web) WebGL.TexImage2D ((int)target, level, (int)publicformat, width, height, (int)format, (int)type, (int)(Ptr)pixels);
      else glTexImage2D (target, level, publicformat, width, height, border, format, type, pixels);
   }
   static delegate* unmanaged<ETexTarget, int, EPixelInternalFormat, int, int, int, EPixelFormat, EPixelType, void*, void> glTexImage2D;
   public static void TexImage2D (ETexTarget target, EPixelInternalFormat infmt, int width, int height, EPixelFormat fmt, EPixelType type, byte[] data)
      {  fixed (byte* p = &data[0]) TexImage2D (target, 0, infmt, width, height, 0, fmt, type, p); }
   public static void TexImage2D (ETexTarget target, EPixelInternalFormat infmt, int width, int height, EPixelFormat fmt, EPixelType type, byte[,] data)
      { fixed (byte* p = &data[0, 0]) TexImage2D (target, 0, infmt, width, height, 0, fmt, type, p); }

   // Set texture parameters
   public static void TexParameter (ETexTarget target, ETexParam pname, int param) { if (Web) WebGL.TexParameter ((int)target, (int)pname, param); else glTexParameteri (target, pname, param); }
   static delegate* unmanaged<ETexTarget, ETexParam, int, void> glTexParameteri;

   // Specify the value of a uniform variable
   public static void Uniform (int location, float f0) { if (Web) WebGL.Uniform1f (location, f0); else glUniform1f (location, f0); }
   static delegate* unmanaged<int, float, void> glUniform1f;
   public static void Uniform (int location, float f0, float f1) { if (Web) WebGL.Uniform2f (location, f0, f1); else glUniform2f (location, f0, f1); }
   static delegate* unmanaged<int, float, float, void> glUniform2f;
   public static void Uniform (int location, float f0, float f1, float f2, float f3) { if (Web) WebGL.Uniform4f (location, f0, f1, f2, f3); else glUniform4f (location, f0, f1, f2, f3); }
   static delegate* unmanaged<int, float, float, float, float, void> glUniform4f;
   public static void Uniform (int location, bool transpose, float* value) { if (Web) WebGL.UniformMatrix4fv (location, transpose, (int)(Ptr)value); else glUniformMatrix4fv (location, 1, transpose, value); }
   static delegate* unmanaged<int, int, bool, float*, void> glUniformMatrix4fv;
   public static void Uniform1i (int location, int n) { if (Web) WebGL.Uniform1i (location, n); else glUniform1i (location, n); }
   static delegate* unmanaged<int, int, void> glUniform1i;

   // Release the mapping of a buffer object's data store (see MapBufferRange for the web emulation)
   public static void UnmapBuffer (EBufferTarget target) {
      if (Web) {
         fixed (byte* p = sMapStaging) WebGL.BufferSubData (sMapTarget, sMapOffset, sMapLength, (int)(Ptr)p);
         return;
      }
      glUnmapBuffer (target);
   }
   static delegate* unmanaged<EBufferTarget, void> glUnmapBuffer;

   // This sets the program object to use for rendering
   public static void UseProgram (HProgram program) { if (Web) WebGL.UseProgram ((int)program); else glUseProgram (program); }
   static delegate* unmanaged<HProgram, void> glUseProgram;

   // Defines an element in a Vertex specification (integral type)
   public static void VertexAttribIPointer (int index, int size, EDataType type, int stride, int offset) { if (Web) WebGL.VertexAttribIPointer (index, size, (int)type, stride, offset); else glVertexAttribIPointer (index, size, type, stride, offset); }
   static delegate* unmanaged<int, int, EDataType, int, Ptr, void> glVertexAttribIPointer;

   // Defines an element in a Vertex specification (float type)
   public static void VertexAttribPointer (int index, int size, EDataType type, bool normalized, int stride, int offset) { if (Web) WebGL.VertexAttribPointer (index, size, (int)type, normalized, stride, offset); else glVertexAttribPointer (index, size, type, normalized, stride, offset); }
   static delegate* unmanaged<int, int, EDataType, bool, int, int, void> glVertexAttribPointer;

   // Specify an attribute as 'per-instance' rather than 'per-vertex'
   public static void VertexAttribDivisor (int index, int divisor) { if (Web) WebGL.VertexAttribDivisor (index, divisor); else glVertexAttribDivisor (index, divisor); }
   static delegate* unmanaged<int, int, void> glVertexAttribDivisor;

   // Set the viewport
   public static void Viewport (int x, int y, int width, int height) { if (Web) WebGL.Viewport (x, y, width, height); else glViewport (x, y, width, height); }
   static delegate* unmanaged<int, int, int, int, void> glViewport;

   // Implementation -----------------------------------------------------------
   static GL () {
      if (Web) return;    // On browser-wasm, everything routes through GLWeb - no proc addresses
      glActiveTexture = (delegate* unmanaged<ETexUnit, void>)Get ("glActiveTexture");
      glAttachShader = (delegate* unmanaged<HProgram, HShader, void>)Get ("glAttachShader");
      glBindBuffer = (delegate* unmanaged<EBufferTarget, HBuffer, void>)Get ("glBindBuffer");
      glBindFramebuffer = (delegate* unmanaged<EFrameBufferTarget, HFrameBuffer, void>)Get ("glBindFramebuffer");
      glBindRenderbuffer = (delegate* unmanaged<ERenderBufferTarget, HRenderBuffer, void>)Get ("glBindRenderbuffer");
      glBindTexture = (delegate* unmanaged<ETexTarget, HTexture, void>)Get ("glBindTexture");
      glBindVertexArray = (delegate* unmanaged<HVertexArray, void>)Get ("glBindVertexArray");
      glBlendFunc = (delegate* unmanaged<EBlendFactor, EBlendFactor, void>)Get ("glBlendFunc");
      glBufferData = (delegate* unmanaged<EBufferTarget, int, Ptr, EBufferUsage, void>)Get ("glBufferData");
      glCheckFramebufferStatus = (delegate* unmanaged<EFrameBufferTarget, EFrameBufferStatus>)Get ("glCheckFramebufferStatus");
      glClear = (delegate* unmanaged<EBuffer, void>)Get ("glClear");
      glClearColor = (delegate* unmanaged<float, float, float, float, void>)Get ("glClearColor");
      glCreateProgram = (delegate* unmanaged<HProgram>)Get ("glCreateProgram");
      glCreateShader = (delegate* unmanaged<EShader, HShader>)Get ("glCreateShader");
      glCompileShader = (delegate* unmanaged<HShader, void>)Get ("glCompileShader");
      glDisable = (delegate* unmanaged<ECap, void>)Get ("glDisable");
      glDisableVertexAttribArray = (delegate* unmanaged<int, void>)Get ("glDisableVertexAttribArray");
      glDeleteBuffers = (delegate* unmanaged<int, HBuffer*, void>)Get ("glDeleteBuffers");
      glDeleteTextures = (delegate* unmanaged<int, HTexture*, void>)Get ("glDeleteTextures");
      glDeleteVertexArrays = (delegate* unmanaged<int, HVertexArray*, void>)Get ("glDeleteVertexArrays");
      glDebugMessageCallback = (delegate* unmanaged<GLDEBUGPROC, void*, void>)Get ("glDebugMessageCallback");
      glDebugMessageControl = (delegate* unmanaged<uint, uint, ESeverity, int, uint*, byte, void>)Get ("glDebugMessageControl");
      glDrawArrays = (delegate* unmanaged<EMode, int, int, void>)Get ("glDrawArrays");
      glDrawArraysInstanced = (delegate* unmanaged<EMode, int, int, int, void>)Get ("glDrawArraysInstanced");
      glDrawElementsBaseVertex = (delegate* unmanaged<EMode, int, EIndexType, Ptr, int, void>)Get ("glDrawElementsBaseVertex");
      glDrawBuffers = (delegate* unmanaged<int, uint*, void>)Get ("glDrawBuffers");
      glEnable = (delegate* unmanaged<ECap, void>)Get ("glEnable");
      glEnableVertexAttribArray = (delegate* unmanaged<int, void>)Get ("glEnableVertexAttribArray");
      glFinish = (delegate* unmanaged<void>)Get ("glFinish");
      glFramebufferRenderbuffer = (delegate* unmanaged<EFrameBufferTarget, EFrameBufferAttachment, ERenderBufferTarget, HRenderBuffer, void>)Get ("glFramebufferRenderbuffer");
      glGenBuffers = (delegate* unmanaged<int, HBuffer*, void>)Get ("glGenBuffers");
      glGenFramebuffers = (delegate* unmanaged<int, HFrameBuffer*, void>)Get ("glGenFramebuffers");
      glGenRenderbuffers = (delegate* unmanaged<int, HRenderBuffer*, void>)Get ("glGenRenderbuffers");
      glGenTextures = (delegate* unmanaged<int, HTexture*, void>)Get ("glGenTextures");
      glGenVertexArrays = (delegate* unmanaged<int, HVertexArray*, void>)Get ("glGenVertexArrays");
      glGetActiveAttrib = (delegate* unmanaged<HProgram, int, int, out int, out int, out EDataType, Ptr, void>)Get ("glGetActiveAttrib");
      glGetActiveUniform = (delegate* unmanaged<HProgram, int, int, out int, out int, out EDataType, Ptr, void>)Get ("glGetActiveUniform");
      glGetAttribLocation = (delegate* unmanaged<HProgram, string, int>)Get ("glGetAttribLocation");
      glGetProgramiv = (delegate* unmanaged<HProgram, EProgramParam, int*, void>)Get ("glGetProgramiv");
      glGetProgramInfoLog = (delegate* unmanaged<HProgram, int, int*, byte*, void>)Get ("glGetProgramInfoLog");
      glGetShaderiv = (delegate* unmanaged<HShader, EShaderParam, int*, void>)Get ("glGetShaderiv");
      glGetShaderInfoLog = (delegate* unmanaged<HShader, int, int*, byte*, void>)Get ("glGetShaderInfoLog");
      glGetUniformLocation = (delegate* unmanaged<HProgram, string, int>) Get ("glGetUniformLocation");
      glLinkProgram = (delegate* unmanaged<HProgram, void>)Get ("glLinkProgram");
      glMapBufferRange = (delegate* unmanaged<EBufferTarget, Ptr, Ptr, EMapAccess, Ptr>)Get ("glMapBufferRange");
      glPatchParameteri = (delegate* unmanaged<EPatchParam, int, void>)Get ("glPatchParameteri");
      glPrimitiveRestartIndex = (delegate* unmanaged<uint, void>)Get ("glPrimitiveRestartIndex");
      glRenderbufferStorage = (delegate* unmanaged<ERenderBufferTarget, ERenderBufferFormat, int, int, void>)Get ("glRenderbufferStorage");
      glShaderSource = (delegate* unmanaged<HShader, int, byte**, int*, void>)Get ("glShaderSource");
      glStencilFuncSeparate = (delegate* unmanaged<EFace, EStencilFunc, int, uint, void>)Get ("glStencilFuncSeparate");
      glStencilOpSeparate = (delegate* unmanaged<EFace, EStencilOp, EStencilOp, EStencilOp, void>)Get ("glStencilOpSeparate");
      glReadPixels = (delegate* unmanaged<int, int, int, int, EPixelFormat, EPixelType, Ptr, void>)Get ("glReadPixels");
      glReadBuffer = (delegate* unmanaged<EFrameBufferAttachment, void>)Get ("glReadBuffer");
      glPixelStorei = (delegate* unmanaged<EPixelStoreParam, int, void>)Get ("glPixelStorei");
      glPolygonOffset = (delegate* unmanaged<float, float, void>)Get ("glPolygonOffset");
      glScissor = (delegate* unmanaged<int, int, int, int, void>)Get ("glScissor");
      glTexImage2D = (delegate* unmanaged<ETexTarget, int, EPixelInternalFormat, int, int, int, EPixelFormat, EPixelType, void*, void>)Get ("glTexImage2D");
      glTexParameteri = (delegate* unmanaged<ETexTarget, ETexParam, int, void>)Get ("glTexParameteri");
      glUniform1f = (delegate* unmanaged<int, float, void>)Get ("glUniform1f");
      glUniform2f = (delegate* unmanaged<int, float, float, void>)Get ("glUniform2f");
      glUniform4f = (delegate* unmanaged<int, float, float, float, float, void>)Get ("glUniform4f");
      glUniformMatrix4fv = (delegate* unmanaged<int, int, bool, float*, void>)Get ("glUniformMatrix4fv");
      glUniform1i = (delegate* unmanaged<int, int, void>)Get ("glUniform1i");
      glUnmapBuffer = (delegate* unmanaged<EBufferTarget, void>)Get ("glUnmapBuffer");
      glUseProgram = (delegate* unmanaged<HProgram, void>)Get ("glUseProgram");
      glVertexAttribIPointer = (delegate* unmanaged<int, int, EDataType, int, Ptr, void>)Get ("glVertexAttribIPointer");
      glVertexAttribPointer = (delegate* unmanaged<int, int, EDataType, bool, int, int, void>)Get ("glVertexAttribPointer");
      glVertexAttribDivisor = (delegate* unmanaged<int, int, void>)Get ("glVertexAttribDivisor");
      glViewport = (delegate* unmanaged<int, int, int, int, void>)Get ("glViewport");
   }
   static nint Get (string name) => Hub.OpenGL.GetGLProcAddress (name);
}
