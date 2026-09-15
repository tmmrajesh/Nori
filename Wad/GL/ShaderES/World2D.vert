#version 300 es

uniform mat4 Xfm;

layout (location = 0) in vec2 VertexPos;

void main () {
   gl_Position = Xfm * vec4 (VertexPos, 0.0, 1.0);
}
