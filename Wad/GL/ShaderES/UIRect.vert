#version 300 es
// UI Rect shader (GLSL ES 3.00 port - manual unpackUnorm4x8, which ES 3.00 lacks)

// Constants
uniform vec2 VPScale;      // = 2.0 / ViewportSize

// Inputs
layout (location = 0) in ivec2 Center;
layout (location = 1) in ivec2 HSize;
layout (location = 2) in uint FillColor;
layout (location = 3) in uint BorderColor;

layout (location = 4) in int Radius;
layout (location = 5) in int BorderThickness;

// Outputs
out vec2 vLocalPos;
flat out vec2 vHSize;
flat out float vRadius;
flat out float vBorderThickness;

flat out vec4 vFillColor;
flat out vec4 vBorderColor;

vec4 Unpack (uint c) {
   return vec4 (float (c & 255u), float ((c >> 8) & 255u),
                float ((c >> 16) & 255u), float ((c >> 24) & 255u)) / 255.0;
}

// Generate quad corners procedurally
vec2 GetCorner (int vertexID) {
   switch (vertexID) {
      case 0: return vec2 (-1, -1);
      case 1: return vec2 (1, -1);
      case 2: return vec2 (1, 1);
      default: return vec2 (-1, 1);
   }
}

void main () {
   // Expand for shadows and anti-aliasing
   float expand = 30.0;
   vec2 expandedHSize = vec2 (HSize) + expand;

   // Get the corner (based on instanced vertex ID)
   vec2 corner = GetCorner (gl_VertexID);
   vLocalPos = corner * expandedHSize;    // Output to fragment shader

   // Screen space position, and in NDC
   vec2 screenPos = vec2 (Center) + vLocalPos;
   vec2 ndc = screenPos * VPScale - 1.0;
   ndc.y = -ndc.y;      // Origin is top left
   gl_Position = vec4 (ndc, 0.0, 1.0);

   // Pass through parameters
   vHSize = vec2 (HSize);
   vRadius = float (Radius);
   vBorderThickness = float (BorderThickness);
   vFillColor = Unpack (FillColor);
   vBorderColor = Unpack (BorderColor);
}
