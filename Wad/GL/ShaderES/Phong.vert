#version 300 es
// Straight ES port of the desktop Phong.vert (per-fragment lighting; no geometry stage)

uniform mat4 Xfm;
uniform mat4 NormalXfm;

layout (location = 0) in vec3 Position;
layout (location = 1) in vec3 Normal;
out vec3 vNormal;

void main (void) {
   vNormal = normalize ((NormalXfm * vec4 (Normal, 1.0)).xyz);
   gl_Position = Xfm * vec4 (Position, 1.0);
}
