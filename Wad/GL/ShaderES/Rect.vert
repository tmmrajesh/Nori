#version 300 es
// WebGL2 replacement for Rect.vert + Rect.geom: each pixel-space box (1 source vertex as
// a per-instance attribute) becomes a 4-vertex triangle strip quad. The math mirrors the
// desktop pair exactly (integer center division, odd-size half-pixel adjust, y flip).
// Attributes:
//    Box : Pixel coordinates of the box (lower left corner in xy, top right corner in zw)

uniform vec2 VPScale;

layout (location = 0) in ivec4 Box;

void main () {
   ivec2 vSize = Box.zw - Box.xy;                        // Size, in pixels
   vec2 pxCenter = vec2 ((Box.xy + Box.zw) / 2);         // Center position, in pixels

   vec2 adjust = mod (vec2 (vSize), 2.0) / 2.0;
   vec2 pix = (pxCenter + adjust) * VPScale;
   vec2 center = vec2 (pix.x - 1.0, 1.0 - pix.y);
   vec2 half4 = vec2 (vSize) * VPScale / 2.0;            // Quad half-size, in NDC

   int vid = gl_VertexID;
   vec2 sgn = (vid == 0) ? vec2 (-1.0, -1.0)
            : (vid == 1) ? vec2 (1.0, -1.0)
            : (vid == 2) ? vec2 (-1.0, 1.0)
            :              vec2 (1.0, 1.0);
   gl_Position = vec4 (center + sgn * half4, 0.0, 1.0);
}
