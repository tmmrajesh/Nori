#version 300 es
// WebGL2 replacement for TextPx.vert + Text2D.geom: each glyph cell (one point vertex,
// delivered as per-instance attributes) becomes a 4-vertex triangle strip quad with the
// corner selected by gl_VertexID.

uniform vec2 VPScale;

layout (location = 0) in ivec4 CharBoxN;
layout (location = 1) in int TexOffset;

flat out ivec2 gCellSize;
flat out int gTexOffset;
out vec2 gTexCoord;

void main () {
   gCellSize = ivec2 (CharBoxN.z - CharBoxN.x, CharBoxN.w - CharBoxN.y);
   gTexOffset = TexOffset;
   vec2 pix1 = (vec2 (CharBoxN.xw) + vec2 (0.01, 0.01)) * VPScale;
   vec2 pix2 = (vec2 (CharBoxN.zy) + vec2 (0.01, 0.01)) * VPScale;
   vec4 box = vec4 (pix1.x - 1.0, 1.0 - pix1.y, pix2.x - 1.0, 1.0 - pix2.y);

   int vid = gl_VertexID;
   vec2 p = (vid == 0) ? box.xy : (vid == 1) ? box.zy : (vid == 2) ? box.xw : box.zw;
   gTexCoord = (vid == 0) ? vec2 (0.0, float (gCellSize.y))
             : (vid == 1) ? vec2 (gCellSize)
             : (vid == 2) ? vec2 (0.0, 0.0)
             :              vec2 (float (gCellSize.x), 0.0);
   gl_Position = vec4 (p, 0.0, 1.0);
}
