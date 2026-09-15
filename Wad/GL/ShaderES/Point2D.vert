#version 300 es
// WebGL2 replacement for World2D.vert + Point2D.geom: each point (1 source vertex as a
// per-instance attribute) becomes a 4-vertex triangle strip quad of PointSize pixels.

uniform mat4 Xfm;
uniform vec2 VPScale;
uniform float PointSize;

layout (location = 0) in vec2 P0;

out vec2 gSTCoord;

void main () {
   vec2 p = (Xfm * vec4 (P0, 0.0, 1.0)).xy / VPScale;     // Now in pixels

   float h = PointSize / 2.0;
   int vid = gl_VertexID;
   vec2 corner = (vid == 0) ? vec2 (h, h)
               : (vid == 1) ? vec2 (h, -h)
               : (vid == 2) ? vec2 (-h, h)
               :              vec2 (-h, -h);
   gSTCoord = corner;
   gl_Position = vec4 (VPScale * (p + corner), 0.0, 1.0);
}
