#version 300 es
// Draws one pixel with a color that is passed in as part of vertex data.
precision highp float;

in vec4 vColor;
out vec4 gFragColor;

void main () {
   gFragColor = vColor;
}
