#version 300 es
precision highp float;

uniform vec4 DrawColor;

in vec2 gPos;
flat in ivec2 gSize;
flat in int gRadius;
flat in int gSide;
out vec4 gFragColor;

void main () {
   vec2 size = vec2 (gSize);
   float d = length (max (abs (gPos), size) - size) - float (gRadius) - 1.5;
   float a = clamp (-d, 0.0, 1.0);
   switch (gSide) {
      case 0: if (gPos.x > 0.0) a = 1.0; break;
      case 1: if (gPos.y < 0.0) a = 1.0; break;
      case 2: if (gPos.x < 0.0) a = 1.0; break;
      case 3: if (gPos.y > 0.0) a = 1.0; break;
   }
   if (a < 0.001) discard;
   gFragColor = vec4 (DrawColor.rgb, a);
}
