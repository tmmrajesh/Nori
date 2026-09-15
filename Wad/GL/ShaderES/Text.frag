#version 300 es
precision highp float;
precision highp int;

uniform sampler2D FontTexture;
uniform vec4 DrawColor;

flat in ivec2 gCellSize;
flat in int gTexOffset;
in vec2 gTexCoord;
layout (location = 0) out vec4 FragColor;

void main () {
   int x = int (gTexCoord.x);
   int y = int (gTexCoord.y);
   int offset = y * gCellSize.x + x + gTexOffset;
   ivec2 st = ivec2 (offset % 8192, offset / 8192);
   float r = texelFetch (FontTexture, st, 0).r;
   if (r < 0.001) discard;
   FragColor = vec4 (DrawColor.rgb, r);
}
