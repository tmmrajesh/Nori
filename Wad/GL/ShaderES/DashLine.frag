#version 300 es
precision highp float;

uniform vec4 DrawColor;
uniform float LineWidth;
uniform float LineType;
uniform sampler2D LTypeTexture;

in float gDist;
in float gTexCoord;
out vec4 gFragColor;

void main () {
   float d = abs (gDist) / LineWidth;
   float a1 = exp2 (-2.0 * d * d);
   float a2 = texture (LTypeTexture, vec2 (gTexCoord, LineType)).r;
   gFragColor = vec4 (DrawColor.rgb, a1 * a2);
}
