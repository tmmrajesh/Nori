#version 300 es
// WebGL2 replacement for World2D.vert + Bezier.tctrl + Bezier.teval + Line2D.geom.
//
// The desktop tessellates each bezier on the GPU, every frame, into a segment count taken from
// its on-screen length - so an arc gets two segments at sheet zoom and dozens when zoomed in.
// WebGL2 has no tessellation stage, so the same thing is done with instancing: each bezier (4
// control points, delivered as per-instance attributes P0..P3 with a divisor of SEGS) is drawn
// as SEGS instances of a 4-vertex triangle strip, one per potential segment. This shader picks
// the segment count exactly as Bezier.tctrl does (one segment per 10 pixels of control polygon,
// at least 2), evaluates the two ends of its own segment, and expands them into a quad the way
// Line2D.vert does. Instances beyond the segment count collapse to a point and rasterize nothing.
//
// Discretizing on the CPU at record time cannot do this: it has to pick a segment count without
// knowing the zoom, and a segment that ends up a fraction of a pixel long covers no pixel centre -
// so small arcs simply vanish from a zoomed-out sheet.

uniform mat4 Xfm;
uniform vec2 VPScale;
uniform float LineWidth;

layout (location = 0) in vec2 P0;
layout (location = 1) in vec2 P1;
layout (location = 2) in vec2 P2;
layout (location = 3) in vec2 P3;

out float gDist;

// Instances drawn per bezier, and so the most segments one can have (the desktop's ceiling too)
const int SEGS = 64;

vec2 bezier (vec2 p0, vec2 p1, vec2 p2, vec2 p3, float u) {
   float u1 = 1.0 - u, u2 = u * u;
   return p0 * (u1 * u1 * u1) + p1 * (3.0 * u * u1 * u1) + p2 * (3.0 * u2 * u1) + p3 * (u2 * u);
}

void main () {
   // Control points in clip coordinates (the bezier is affine-invariant, so it can be evaluated here)
   vec2 c0 = (Xfm * vec4 (P0, 0.0, 1.0)).xy, c1 = (Xfm * vec4 (P1, 0.0, 1.0)).xy;
   vec2 c2 = (Xfm * vec4 (P2, 0.0, 1.0)).xy, c3 = (Xfm * vec4 (P3, 0.0, 1.0)).xy;
   vec2 invScale = 1.0 / VPScale;

   // The segment count, as Bezier.tctrl computes it: none at all for a bezier wholly off-screen
   vec2 minv = min (min (c0, c1), min (c2, c3)), maxv = max (max (c0, c1), max (c2, c3));
   float level = 0.0;
   if (!(minv.x > 1.0 || minv.y > 1.0 || maxv.x < -1.0 || maxv.y < -1.0)) {
      float len = length ((c1 - c0) * invScale) + length ((c3 - c2) * invScale);
      level = clamp (ceil (len / 10.0), 2.0, float (SEGS));
   }
   int seg = gl_InstanceID % SEGS;
   if (float (seg) >= level) {
      // A segment this bezier does not need: collapse the whole quad to a point
      gl_Position = vec4 (c3, 0.0, 1.0); gDist = 0.0;
      return;
   }

   // The two ends of this segment, in pixel coordinates, then the Line2D quad around them
   vec2 p0 = bezier (c0, c1, c2, c3, float (seg) / level) * invScale;
   vec2 p1 = bezier (c0, c1, c2, c3, float (seg + 1) / level) * invScale;

   float width = LineWidth / 2.0;
   vec2 dir = normalize (p1 - p0) * width;
   vec2 perpdir = vec2 (dir.y, -dir.x);
   dir *= 0.0625;

   int vid = gl_VertexID;
   vec2 p = (vid == 0) ? p0 + perpdir - dir
          : (vid == 1) ? p1 + perpdir + dir
          : (vid == 2) ? p0 - perpdir - dir
          :              p1 - perpdir + dir;
   gDist = (vid < 2 ? 1.0 : -1.0) * width * 2.9;
   gl_Position = vec4 (VPScale * p, 0.0, 1.0);
}
