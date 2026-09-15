#version 300 es
precision highp float;

uniform vec4 DrawColor;
uniform float LineWidth;

in float gDist;
out vec4 gFragColor;

void main () {
   float d = abs (gDist) / LineWidth;
   float a = exp2 (-2.0 * d * d);
   gFragColor = vec4 (DrawColor.rgb, a * DrawColor.a);
}
