#version 300 es
// WebGL2 replacement for Text2D.vert + Text2D.geom (see TextPx.vert for the pattern)

uniform mat4 Xfm;
uniform vec2 VPScale;

layout (location = 0) in vec2 VertexPos;
layout (location = 1) in ivec4 CharBoxN;
layout (location = 2) in int TexOffset;

flat out ivec2 gCellSize;
flat out int gTexOffset;
out vec2 gTexCoord;

void main () {
   gCellSize = ivec2 (CharBoxN.z - CharBoxN.x, CharBoxN.w - CharBoxN.y);
   gTexOffset = TexOffset;
   vec2 xyref = (Xfm * vec4 (VertexPos, 0.0, 1.0)).xy;    // xy0 now in clip space
   xyref = floor ((xyref / VPScale) + vec2 (0.01, 0.01));
   xyref *= VPScale;
   vec2 pix1 = vec2 (CharBoxN.xw) * VPScale;
   vec2 pix2 = vec2 (CharBoxN.zy) * VPScale;
   vec4 box = vec4 (pix1.x + xyref.x, -pix1.y + xyref.y, pix2.x + xyref.x, -pix2.y + xyref.y);

   int vid = gl_VertexID;
   vec2 p = (vid == 0) ? box.xy : (vid == 1) ? box.zy : (vid == 2) ? box.xw : box.zw;
   gTexCoord = (vid == 0) ? vec2 (0.0, float (gCellSize.y))
             : (vid == 1) ? vec2 (gCellSize)
             : (vid == 2) ? vec2 (0.0, 0.0)
             :              vec2 (float (gCellSize.x), 0.0);
   gl_Position = vec4 (p, 0.0, 1.0);
}
