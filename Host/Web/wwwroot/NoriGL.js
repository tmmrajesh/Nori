// ────── ╔╗
// ╔═╦╦═╦╦╬╣ NoriGL.js
// ║║║║╬║╔╣║ WebGL2 backend for Lux's GL facade (the JS half of Nori.Lux/OpenGLWeb.cs).
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
// Desktop GL identifies objects by integer names and reads vertex data through raw pointers.
// WebGL2 identifies objects by JS references and reads from ArrayBuffers. This module bridges
// the two: handle tables map integers to WebGL objects, and data pointers are treated as
// offsets into the WASM linear memory (read via getDotnetRuntime(0).localHeapView*).
//
// It also emulates the desktop features WebGL2 lacks:
//  - drawExpanded: geometry-shader replacement - N source vertices per instance, drawn as
//    instanced 4-vertex triangle strips with the original attributes rebound as per-instance
//    attributes (locations duplicated per source vertex)
//  - drawQuads: GL_QUADS via a synthesized index buffer
//  - drawElementsBaseVertex: attribute offset rebinding
//  - readPixels BGRA (swizzle) and DEPTH_COMPONENT (zero-fill: not readable in WebGL2)

import { getGL } from "./NoriHost.js";

let runtime = null;
function heapU8 () { runtime ??= globalThis.getDotnetRuntime (0); return runtime.localHeapViewU8 (); }
function heapF32 (ptr, len) { const u8 = heapU8 (); return new Float32Array (u8.buffer, ptr, len); }

// Handle tables: index 0 is reserved for 'no object' (GL name 0)
const objs = [null];
const alloc = o => { objs.push (o); return objs.length - 1; };
const obj = h => (h === 0 ? null : objs[h]);

// Per-program uniform-location tables (dense int handles over WebGLUniformLocation objects)
let curProgram = null;           // { pgm, locs, byName }
const progInfo = new Map ();     // handle -> { pgm, locs, byName }

// Per-VAO attribute layout tracking (for the draw emulations)
// state = { attribs: [], element: 0 } ; attribs[i] = {size,type,norm,stride,offset,buffer,integral}
const vaoState = new Map ();
vaoState.set (0, { attribs: [], element: 0 });
let curVAO = 0;
let curArrayBuffer = 0;
const vao = () => vaoState.get (curVAO);

// GL constant shims
const TEX_RECT = 0x84F5, TEXTURE_2D = 0x0DE1;
const texTarget = t => (t === TEX_RECT ? TEXTURE_2D : t);
const ALLOWED_CAPS = new Set ([0xBE2, 0xB71, 0x8037, 0xC11, 0xB44, 0xB90]);   // blend, depth, polyoffset, scissor, cull, stencil

export function activeTexture (unit) { getGL ().activeTexture (unit); }
export function attachShader (p, s) { getGL ().attachShader (obj (p), obj (s)); }

export function bindBuffer (target, buffer) {
   getGL ().bindBuffer (target, obj (buffer));
   if (target === 0x8892) curArrayBuffer = buffer;         // ARRAY_BUFFER
   else if (target === 0x8893) vao ().element = buffer;    // ELEMENT_ARRAY_BUFFER (VAO state)
}

export function bindFramebuffer (target, buffer) { getGL ().bindFramebuffer (0x8D40, obj (buffer)); }
export function bindRenderbuffer (target, buffer) { getGL ().bindRenderbuffer (0x8D41, obj (buffer)); }
export function bindTexture (target, tex) { getGL ().bindTexture (texTarget (target), obj (tex)); }

export function bindVertexArray (v) {
   getGL ().bindVertexArray (obj (v));
   curVAO = v;
   if (!vaoState.has (v)) vaoState.set (v, { attribs: [], element: 0 });
}

export function blendFunc (src, dst) { getGL ().blendFunc (src, dst); }

export function bufferData (target, size, ptr, usage) {
   const gl = getGL ();
   if (ptr === 0) gl.bufferData (target, size, usage);
   else gl.bufferData (target, heapU8 ().subarray (ptr, ptr + size), usage);
}

export function bufferSubData (target, offset, size, ptr) {
   getGL ().bufferSubData (target, offset, heapU8 (), ptr, size);
}

export function checkFramebufferStatus (target) { return getGL ().checkFramebufferStatus (0x8D40); }
export function clear (mask) { getGL ().clear (mask); }
export function clearColor (r, g, b, a) { getGL ().clearColor (r, g, b, a); }
export function compileShader (s) { getGL ().compileShader (obj (s)); }

export function createProgram () {
   const h = alloc (getGL ().createProgram ());
   progInfo.set (h, { pgm: objs[h], locs: [], byName: new Map () });
   return h;
}

export function createShader (type) { return alloc (getGL ().createShader (type)); }
export function createBuffer () { return alloc (getGL ().createBuffer ()); }
export function createFramebuffer () { return alloc (getGL ().createFramebuffer ()); }
export function createRenderbuffer () { return alloc (getGL ().createRenderbuffer ()); }
export function createTexture () { return alloc (getGL ().createTexture ()); }
export function createVertexArray () { return alloc (getGL ().createVertexArray ()); }

export function deleteBuffer (h) { getGL ().deleteBuffer (obj (h)); objs[h] = null; }
export function deleteTexture (h) { getGL ().deleteTexture (obj (h)); objs[h] = null; }
export function deleteVertexArray (h) { getGL ().deleteVertexArray (obj (h)); objs[h] = null; vaoState.delete (h); }

export function disable (cap) { if (ALLOWED_CAPS.has (cap)) getGL ().disable (cap); }
export function enable (cap) { if (ALLOWED_CAPS.has (cap)) getGL ().enable (cap); }

export function disableVertexAttribArray (i) { getGL ().disableVertexAttribArray (i); }
export function enableVertexAttribArray (i) { getGL ().enableVertexAttribArray (i); }

export function drawArrays (mode, start, count) { getGL ().drawArrays (mode, start, count); }
export function drawArraysInstanced (mode, start, count, instances) { getGL ().drawArraysInstanced (mode, start, count, instances); }

// Selects the first n color attachments as the draw-buffer set (n > 1 means MRT)
export function drawBuffers (n) {
   const bufs = [];
   for (let i = 0; i < n; i++) bufs.push (0x8CE0 + i);    // COLOR_ATTACHMENT0 + i
   getGL ().drawBuffers (bufs);
}

// Selects the color attachment that readPixels reads from
export function readBuffer (attachment) { getGL ().readBuffer (attachment); }

// Applies one tracked attribute spec at the given location with overridden stride/offset
function applyAttrib (gl, loc, a, stride, offset, divisor) {
   gl.bindBuffer (0x8892, obj (a.buffer));
   if (a.integral) gl.vertexAttribIPointer (loc, a.size, a.type, stride, offset);
   else gl.vertexAttribPointer (loc, a.size, a.type, a.norm, stride, offset);
   gl.enableVertexAttribArray (loc);
   gl.vertexAttribDivisor (loc, divisor);
}

// Restores the current VAO's original attribute layout (after an emulated draw disturbed it)
function restoreLayout (gl) {
   const st = vao ();
   for (let i = 0; i < st.attribs.length; i++) {
      const a = st.attribs[i];
      if (a) applyAttrib (gl, i, a, a.stride, a.offset, 0);
   }
   gl.bindBuffer (0x8892, obj (curArrayBuffer));
}

// The geometry-shader replacement: draws count/nPer instances of a 4-vertex triangle strip.
// The original per-vertex attributes (locations 0..nA-1) are rebound as per-instance
// attributes: source vertex v of each instance appears at locations v*nA .. v*nA+nA-1.
// The ES vertex shader positions the 4 strip corners using gl_VertexID.
// With sub > 1 each set of nPer vertices is drawn sub times over (the attribute divisor), which
// is how a bezier gets its tessellation: one strip per potential segment, and the vertex shader
// collapses the ones it does not need (Bezier2D.vert).
export function drawExpanded (nPer, first, count, sub) {
   const gl = getGL ();
   const st = vao ();
   const attribs = st.attribs.filter (a => a);
   const nA = attribs.length;
   for (let v = 0; v < nPer; v++)
      for (let i = 0; i < nA; i++) {
         const a = attribs[i];
         applyAttrib (gl, v * nA + i, a, a.stride * nPer, a.offset + (first + v) * a.stride, sub);
      }
   gl.drawArraysInstanced (5, 0, 4, (count / nPer) * sub);      // 5 = TRIANGLE_STRIP
   for (let loc = nA; loc < nPer * nA; loc++) { gl.vertexAttribDivisor (loc, 0); gl.disableVertexAttribArray (loc); }
   restoreLayout (gl);
}

// GL_QUADS emulation: synthesize the (0,1,2, 0,2,3) index pattern and drawElements
let quadIndexBuffer = null, quadIndexCap = 0, quadIndexFirst = -1;
export function drawQuads (first, count) {
   const gl = getGL ();
   const quads = Math.floor (count / 4);
   if (quads <= 0) return;
   quadIndexBuffer ??= gl.createBuffer ();
   gl.bindBuffer (0x8893, quadIndexBuffer);
   if (quads * 6 > quadIndexCap || quadIndexFirst !== first) {
      const idx = new Uint32Array (quads * 6);
      for (let k = 0, n = 0; k < quads; k++) {
         const b = first + k * 4;
         idx[n++] = b; idx[n++] = b + 1; idx[n++] = b + 2;
         idx[n++] = b; idx[n++] = b + 2; idx[n++] = b + 3;
      }
      gl.bufferData (0x8893, idx, 0x88E4);   // STATIC_DRAW
      quadIndexCap = quads * 6; quadIndexFirst = first;
   }
   gl.drawElements (4, quads * 6, 0x1405, 0);            // TRIANGLES, UNSIGNED_INT
   gl.bindBuffer (0x8893, obj (vao ().element));         // restore the VAO's own element buffer
}

// BaseVertex emulation: rebind the attributes shifted by baseVertex*stride
export function drawElementsBaseVertex (mode, count, type, ioffset, baseVertex) {
   const gl = getGL ();
   if (baseVertex !== 0) {
      const st = vao ();
      for (let i = 0; i < st.attribs.length; i++) {
         const a = st.attribs[i];
         if (a) applyAttrib (gl, i, a, a.stride, a.offset + baseVertex * a.stride, 0);
      }
   }
   gl.drawElements (mode, count, type, ioffset);
   if (baseVertex !== 0) restoreLayout (gl);
}

export function finish () { getGL ().finish (); }
export function framebufferRenderbuffer (target, attachment, rbo) { getGL ().framebufferRenderbuffer (0x8D40, attachment, 0x8D41, obj (rbo)); }

export function getActiveAttrib (p, index) {
   const gl = getGL (), pgm = obj (p);
   const info = gl.getActiveAttrib (pgm, index);
   const loc = gl.getAttribLocation (pgm, info.name);
   return `${info.name}|${info.size}|${info.type}|${loc}`;
}

export function getActiveUniform (p, index) {
   const info = getGL ().getActiveUniform (obj (p), index);
   const name = info.name.replace ("[0]", "");
   return `${name}|${info.size}|${info.type}|${getUniformLocation (p, name)}`;
}

export function getAttribLocation (p, name) { return getGL ().getAttribLocation (obj (p), name); }

export function getProgram (p, pname) {
   const gl = getGL (), pgm = obj (p);
   if (pname === 0x8B84) return gl.getProgramInfoLog (pgm).length + 1;    // INFO_LOG_LENGTH shim
   const v = gl.getProgramParameter (pgm, pname);
   return typeof v === "boolean" ? (v ? 1 : 0) : v;
}

export function getProgramInfoLog (p) { return getGL ().getProgramInfoLog (obj (p)) || ""; }

export function getShader (s, pname) {
   const gl = getGL (), sh = obj (s);
   if (pname === 0x8B84) return gl.getShaderInfoLog (sh).length + 1;      // INFO_LOG_LENGTH shim
   const v = gl.getShaderParameter (sh, pname);
   return typeof v === "boolean" ? (v ? 1 : 0) : v;
}

export function getShaderInfoLog (s) { return getGL ().getShaderInfoLog (obj (s)) || ""; }

export function getUniformLocation (p, name) {
   const pi = progInfo.get (p);
   if (pi.byName.has (name)) return pi.byName.get (name);
   const loc = getGL ().getUniformLocation (pi.pgm, name);
   if (loc === null) { pi.byName.set (name, -1); return -1; }
   const h = pi.locs.length;
   pi.locs.push (loc); pi.byName.set (name, h);
   return h;
}

export function linkProgram (p) { getGL ().linkProgram (obj (p)); }
export function pixelStore (pname, param) { getGL ().pixelStorei (pname, param); }
export function polygonOffset (factor, units) { getGL ().polygonOffset (factor, units); }

export function readPixels (x, y, w, h, format, type, ptr) {
   const gl = getGL ();
   if (format === 0x1902) {                              // DEPTH_COMPONENT: not readable in WebGL2
      heapU8 ().fill (0, ptr, ptr + w * h * 4);
      return;
   }
   if (format === 32993) {                               // BGRA: read RGBA and swizzle
      const tmp = new Uint8Array (w * h * 4);
      gl.readPixels (x, y, w, h, 0x1908, 0x1401, tmp);
      const u8 = heapU8 ();
      for (let i = 0; i < tmp.length; i += 4) {
         u8[ptr + i] = tmp[i + 2]; u8[ptr + i + 1] = tmp[i + 1];
         u8[ptr + i + 2] = tmp[i]; u8[ptr + i + 3] = tmp[i + 3];
      }
      return;
   }
   const channels = format === 0x1903 ? 1 : format === 0x1907 ? 3 : 4;    // RED / RGB / RGBA
   const len = w * h * channels;
   gl.readPixels (x, y, w, h, format, type, heapU8 ().subarray (ptr, ptr + len));
}

export function renderbufferStorage (format, cx, cy) { getGL ().renderbufferStorage (0x8D41, format, cx, cy); }
export function scissor (x, y, w, h) { getGL ().scissor (x, y, w, h); }
export function shaderSource (s, source) { getGL ().shaderSource (obj (s), source); }
export function stencilFunc (func, value, mask) { getGL ().stencilFunc (func, value, mask); }
export function stencilOp (sfail, dpfail, dppass) { getGL ().stencilOp (sfail, dpfail, dppass); }

export function texImage2D (target, level, internalFormat, w, h, format, type, ptr) {
   const gl = getGL ();
   // Unsized RED is not a valid internal format in WebGL2 - use R8
   const ifmt = internalFormat === 0x1903 ? 0x8229 : internalFormat;
   if (ptr === 0) { gl.texImage2D (texTarget (target), level, ifmt, w, h, 0, format, type, null); return; }
   const channels = format === 0x1903 ? 1 : format === 0x1907 ? 3 : 4;
   const bpc = type === 0x1406 ? 4 : 1;                  // FLOAT : UBYTE
   gl.texImage2D (texTarget (target), level, ifmt, w, h, 0, format, type, heapU8 (), ptr);
   void channels; void bpc;   // length is implied by w*h*format; heap view + srcOffset form is used
}

export function texParameter (target, pname, param) {
   if (param === 10496) param = 33071;                   // GL_CLAMP -> CLAMP_TO_EDGE
   getGL ().texParameteri (texTarget (target), pname, param);
}

function uloc (h) { return h < 0 ? null : curProgram.locs[h]; }
export function uniform1f (h, f0) { getGL ().uniform1f (uloc (h), f0); }
export function uniform2f (h, f0, f1) { getGL ().uniform2f (uloc (h), f0, f1); }
export function uniform4f (h, f0, f1, f2, f3) { getGL ().uniform4f (uloc (h), f0, f1, f2, f3); }
export function uniform1i (h, n) { getGL ().uniform1i (uloc (h), n); }
export function uniformMatrix4fv (h, transpose, ptr) { getGL ().uniformMatrix4fv (uloc (h), transpose, heapF32 (ptr, 16)); }

export function useProgram (p) {
   getGL ().useProgram (obj (p));
   curProgram = p === 0 ? null : progInfo.get (p);
}

export function vertexAttribPointer (index, size, type, normalized, stride, offset) {
   getGL ().vertexAttribPointer (index, size, type, normalized, stride, offset);
   vao ().attribs[index] = { size, type, norm: normalized, stride, offset, buffer: curArrayBuffer, integral: false };
}

export function vertexAttribIPointer (index, size, type, stride, offset) {
   getGL ().vertexAttribIPointer (index, size, type, stride, offset);
   vao ().attribs[index] = { size, type, norm: false, stride, offset, buffer: curArrayBuffer, integral: true };
}

export function vertexAttribDivisor (index, divisor) { getGL ().vertexAttribDivisor (index, divisor); }
export function viewport (x, y, w, h) { getGL ().viewport (x, y, w, h); }
