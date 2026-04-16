shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back;

uniform vec4 albedo : source_color = vec4(1.0);
uniform sampler2D texture_albedo : source_color, hint_default_white;

uniform vec3 player_positions[4];
uniform int player_count = 0;
uniform float radius : hint_range(0.0, 1.0) = 0.15;
uniform float edge_softness : hint_range(0.001, 0.1) = 0.02;

void fragment() {
    vec4 tex = texture(texture_albedo, UV);
    ALBEDO = tex.rgb * albedo.rgb;

    float min_dist = 9999.0;

    for (int i = 0; i < player_count; i++) {
        // Project player world position to clip space
        vec4 clip_pos = PROJECTION_MATRIX * VIEW_MATRIX * vec4(player_positions[i], 1.0);

        // Perspective divide to get NDC (-1 to 1)
        vec2 ndc = clip_pos.xy / clip_pos.w;

        // Convert current fragment to NDC
        // SCREEN_UV goes 0-1, convert to -1 to 1
        vec2 frag_ndc = SCREEN_UV * 2.0 - 1.0;

        // Distance in screen space
        vec2 diff = frag_ndc - ndc;

        // Correct for aspect ratio so it's a circle not an ellipse
        diff.x *= VIEWPORT_SIZE.x / VIEWPORT_SIZE.y;

        float dist = length(diff);
        min_dist = min(min_dist, dist);
    }

    float alpha = smoothstep(radius - edge_softness, radius, min_dist);
    ALPHA = tex.a * alpha;
}