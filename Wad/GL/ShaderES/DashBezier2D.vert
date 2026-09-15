#version 300 es
// WebGL2 replacement for World2D.vert + Bezier.tctrl + Bezier.teval + DashLine2D.geom: the
// instanced tessellation of Bezier2D.vert (see there), with the dash texture coordinate that
// DashLine2D.vert adds. As on the desktop, where DashBezier2D shares DashLine2D.geom, the dash
// phase restarts on every tessellated segment.

uniform mat4 Xfm;
uniform vec2 VPScale;
uniform float LineWidth;
uniform float LTScale;

layout (location = 0) in vec2 P0;
layout (location = 1) in vec2 P1;
layout (location = 2) in vec2 P2;
layout (location = 3) in vec2 P3;

out float gDist;
out float gTexCoord;

const int SEGS = 64;

vec2 bezier (vec2 p0, vec2 p1, vec2 p2, vec2 p3, float u) {
   float u1 = 1.0 - u, u2 = u * u;
   return p0 * (u1 * u1 * u1) + p1 * (3.0 * u * u1 * u1) + p2 * (3.0 * u2 * u1) + p3 * (u2 * u);
}

void main () {
   vec2 c0 = (Xfm * vec4 (P0, 0.0, 1.0)).xy, c1 = (Xfm * vec4 (P1, 0.0, 1.0)).xy;
   vec2 c2 = (Xfm * vec4 (P2, 0.0, 1.0)).xy, c3 = (Xfm * vec4 (P3, 0.0, 1.0)).xy;
   vec2 invScale = 1.0 / VPScale;

   vec2 minv = min (min (c0, c1), min (c2, c3)), maxv = max (max (c0, c1), max (c2, c3));
   float level = 0.0;
   if (!(minv.x > 1.0 || minv.y > 1.0 || maxv.x < -1.0 || maxv.y < -1.0)) {
      float len = length ((c1 - c0) * invScale) + length ((c3 - c2) * invScale);
      level = clamp (ceil (len / 10.0), 2.0, float (SEGS));
   }
   int seg = gl_InstanceID % SEGS;
   if (float (seg) >= level) {
      gl_Position = vec4 (c3, 0.0, 1.0); gDist = 0.0; gTexCoord = 0.0;
      return;
   }

   vec2 p0 = bezier (c0, c1, c2, c3, float (seg) / level) * invScale;
   vec2 p1 = bezier (c0, c1, c2, c3, float (seg + 1) / level) * invScale;

   float width = LineWidth / 2.0;
   vec2 dir = p1 - p0;
   float len = length (dir);
   dir = normalize (dir) * width;
   vec2 perpdir = vec2 (dir.y, -dir.x);
   dir *= 0.0625;

   int vid = gl_VertexID;
   vec2 p = (vid == 0) ? p0 + perpdir - dir
          : (vid == 1) ? p1 + perpdir + dir
          : (vid == 2) ? p0 - perpdir - dir
          :              p1 - perpdir + dir;
   gDist = (vid < 2 ? 1.0 : -1.0) * width * 2.9;
   gTexCoord = (vid == 1 || vid == 3) ? len / LTScale : 0.0;
   gl_Position = vec4 (VPScale * p, 0.0, 1.0);
}
