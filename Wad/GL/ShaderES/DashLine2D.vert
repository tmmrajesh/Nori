#version 300 es
// WebGL2 replacement for World2D.vert + DashLine2D.geom (see Line2D.vert for the pattern)

uniform mat4 Xfm;
uniform vec2 VPScale;
uniform float LineWidth;
uniform float LTScale;

layout (location = 0) in vec2 P0;
layout (location = 1) in vec2 P1;

out float gDist;
out float gTexCoord;

void main () {
   vec2 invScale = 1.0 / VPScale;
   vec2 p0 = (Xfm * vec4 (P0, 0.0, 1.0)).xy * invScale;   // Now in pixel coordinates
   vec2 p1 = (Xfm * vec4 (P1, 0.0, 1.0)).xy * invScale;

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
