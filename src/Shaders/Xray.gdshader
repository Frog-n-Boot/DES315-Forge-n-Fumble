shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back;

uniform vec4 albedo : source_color = vec4(1.0);
uniform sampler2D texture_albedo : source_color, hint_default_white;

// Up to 4 players — extend if needed
uniform vec3 player_positions[4];
uniform int player_count = 0;
uniform float radius : hint_range(0.0, 5.0) = 1.5;
uniform float edge_softness : hint_range(0.01, 2.0) = 0.4;

void fragment() {
    vec4 tex = texture(texture_albedo, UV);
    ALBEDO = tex.rgb * albedo.rgb;

    // Get this fragment's world position
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

    float min_dist = 9999.0;

    for (int i = 0; i < player_count; i++) {
        // Flatten to XZ so the hole is vertical (cylinder shape)
        vec2 diff = world_pos.xz - player_positions[i].xz;
        float dist = length(diff);
        min_dist = min(min_dist, dist);
    }

    // Smooth step: 1.0 at edge, 0.0 at center
    float alpha = smoothstep(radius - edge_softness, radius, min_dist);
    ALPHA = tex.a * alpha;
}