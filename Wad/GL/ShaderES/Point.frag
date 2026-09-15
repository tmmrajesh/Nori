#version 300 es
precision highp float;

uniform vec4 DrawColor;
uniform float PointSize;

in vec2 gSTCoord;
out vec4 gFragColor;

void main () {
   float radius = PointSize / 2.0;
   float dist = distance (gSTCoord, vec2 (0.0, 0.0));
   float a = 1.0 - smoothstep (radius - 1.0, radius, dist);
   gFragColor = vec4 (DrawColor.rgb, a);
}
