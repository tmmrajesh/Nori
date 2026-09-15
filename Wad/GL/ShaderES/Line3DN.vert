#version 300 es
// Line3D.vert for the Vec3F_Vec3H mesh-wire pipelines (BlackLine / GlassLine): each
// source vertex carries (position, normal), so the expanded per-instance attributes
// land at locations (Pos0@0, Norm0@1, Pos1@2, Norm1@3) - see NoriGL.js drawExpanded.
// The normals are not used for line drawing; only locations 0 and 2 are declared.

uniform mat4 Xfm;
uniform vec2 VPScale;
uniform float LineWidth;

layout (location = 0) in vec3 P0;
layout (location = 2) in vec3 P1;

out float gDist;

void main () {
   vec2 invScale = 1.0 / VPScale;
   vec4 v0 = Xfm * vec4 (P0, 1.0);
   vec4 v1 = Xfm * vec4 (P1, 1.0);
   vec2 p0 = v0.xy * invScale;      // Now in pixel coordinates
   vec2 p1 = v1.xy * invScale;

   float width = LineWidth / 2.0;
   vec2 dir = normalize (p1 - p0) * width;
   vec2 perpdir = vec2 (dir.y, -dir.x);
   dir *= 0.25;

   int vid = gl_VertexID;
   vec2 p = (vid == 0) ? p0 + perpdir - dir
          : (vid == 1) ? p1 + perpdir + dir
          : (vid == 2) ? p0 - perpdir - dir
          :              p1 - perpdir + dir;
   float z = (vid == 0 || vid == 2) ? v0.z : v1.z;
   gDist = (vid < 2 ? 1.0 : -1.0) * width * 2.9;
   gl_Position = vec4 (VPScale * p, z, 1.0);
}
