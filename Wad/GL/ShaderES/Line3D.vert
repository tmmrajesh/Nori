#version 300 es
// WebGL2 replacement for World3D.vert + Line3D.geom: each 3D line (2 source vertices,
// delivered as per-instance attributes P0 / P1) is expanded into a 4-vertex triangle
// strip, with the corner selected by gl_VertexID. The expansion math is identical to
// the desktop geometry shader (z carried through per endpoint).

uniform mat4 Xfm;
uniform vec2 VPScale;
uniform float LineWidth;

layout (location = 0) in vec3 P0;
layout (location = 1) in vec3 P1;

out float gDist;

void main () {
   vec2 invScale = 1.0 / VPScale;
   vec4 v0 = Xfm * vec4 (P0, 1.0);
   vec4 v1 = Xfm * vec4 (P1, 1.0);
   vec2 p0 = v0.xy * invScale;      // Now in pixel coordinates
   vec2 p1 = v1.xy * invScale;

   float width = LineWidth / 2.0;
   vec2 dir = normalize (p1 - p0) * width;
   vec2 perpdir = vec2 (dir.y, -dir.x);
   dir *= 0.25;

   int vid = gl_VertexID;
   vec2 p = (vid == 0) ? p0 + perpdir - dir
          : (vid == 1) ? p1 + perpdir + dir
          : (vid == 2) ? p0 - perpdir - dir
          :              p1 - perpdir + dir;
   float z = (vid == 0 || vid == 2) ? v0.z : v1.z;
   gDist = (vid < 2 ? 1.0 : -1.0) * width * 2.9;
   gl_Position = vec4 (VPScale * p, z, 1.0);
}
