#version 300 es
precision highp float;

// The pick fragment shader of the web: WebGL2 cannot read the depth buffer back
// (desktop picking does a second ReadPixels on DEPTH_COMPONENT), so this writes the
// fragment depth, RGBA-packed, into color attachment 1 (see Lux.BeginRender /
// EndRender for the MRT setup and the matching decode).

uniform vec4 DrawColor;

layout (location = 0) out vec4 gFragColor;    // The false-color VNode id
layout (location = 1) out vec4 gDepth;        // RGBA-packed gl_FragCoord.z

void main () {
   gFragColor = DrawColor;
   vec4 enc = fract (vec4 (1.0, 255.0, 65025.0, 16581375.0) * gl_FragCoord.z);
   enc -= enc.yzww * vec4 (1.0 / 255.0, 1.0 / 255.0, 1.0 / 255.0, 0.0);
   gDepth = enc;
}
