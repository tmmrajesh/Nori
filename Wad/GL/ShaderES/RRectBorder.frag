#version 300 es
precision highp float;

uniform vec4 DrawColor;
uniform vec4 BorderColor;

in vec2 gPos;
flat in ivec2 gSize;
flat in int gRadius;
flat in int gBorder;
out vec4 gFragColor;

void main () {
   vec2 size = vec2 (gSize);
   float d = length (max (abs (gPos), size) - size) - float (gRadius) - 1.5;
   float border = float (gBorder);
   if (d < -border) gFragColor = mix (DrawColor, BorderColor, clamp (d + border + 1.0, 0.0, 1.0));
   else gFragColor = vec4 (BorderColor.rgb, clamp (-d, 0.0, 1.0));
}
