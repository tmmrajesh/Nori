#version 300 es
precision highp float;

uniform vec4 DrawColor;
out vec4 gFragColor;

void main () {
   gFragColor = DrawColor;
}
