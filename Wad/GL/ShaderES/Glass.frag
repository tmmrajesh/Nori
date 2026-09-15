#version 300 es
precision highp float;

uniform vec4 DrawColor;

in vec4 vLightIntensity;
out vec4 gFragColor;

void main () {
   ivec2 coord = ivec2 (gl_FragCoord.xy - 0.5);
   if (fract (float (coord.x + coord.y) / 2.0) < 0.5) discard;
   gFragColor = vec4 (vLightIntensity.rgb, DrawColor.a);
}
