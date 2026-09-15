#version 300 es
// WebGL2 replacement for RectBorder.vert + RectBorder.geom: a pixel-space box with a
// border, expanded into a quad per instance (see Rect.vert for the layout notes).
// Attributes:
//    Box : Pixel coordinates of the box (lower left corner in xy, top right corner in zw)
//    Border : Border thickness on each side, in pixels

uniform vec2 VPScale;

layout (location = 0) in ivec4 Box;
layout (location = 1) in int Border;

out vec2 gPos;             // Pixel position, relative to center
flat out ivec2 gSize;      // Box half-size
flat out int gBorder;

void main () {
   ivec2 vSize = Box.zw - Box.xy;                        // Size, in pixels
   vec2 pxCenter = vec2 ((Box.xy + Box.zw) / 2);         // Center position, in pixels

   gBorder = Border;
   gSize = vSize / 2;
   vec2 size = vec2 (vSize / 2);

   vec2 adjust = mod (vec2 (vSize), 2.0) / 2.0;
   vec2 pix = (pxCenter + adjust) * VPScale;
   vec2 center = vec2 (pix.x - 1.0, 1.0 - pix.y);
   vec2 half4 = vec2 (vSize) * VPScale / 2.0;            // Quad half-size, in NDC

   int vid = gl_VertexID;
   vec2 sgn = (vid == 0) ? vec2 (-1.0, -1.0)
            : (vid == 1) ? vec2 (1.0, -1.0)
            : (vid == 2) ? vec2 (-1.0, 1.0)
            :              vec2 (1.0, 1.0);
   gPos = sgn * size;
   gl_Position = vec4 (center + sgn * half4, 0.0, 1.0);
}
