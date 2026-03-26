// ============================================================
//  xray_player_overlay.gdshader
// ============================================================
shader_type spatial;

render_mode depth_test_disabled, depth_prepass_alpha, unshaded, cull_disabled;

// ── Uniforms ─────────────────────────────────────────────────
uniform vec3  player_positions[4];
uniform int   player_count      = 1;
uniform float xray_radius       : hint_range(0.01, 0.5)  = 0.01;
uniform float dither_band       : hint_range(0.005, 0.2) = 0.2;
uniform vec4  xray_tint         : source_color = vec4(0.25, 0.85, 1.0, 1.0);
uniform float pulse_speed       : hint_range(0.0, 10.0) = 2.0;
uniform float pulse_amount      : hint_range(0.0, 1.0)  = 0.15;
// Set each frame by XRayManager — avoids using VIEWPORT_SIZE in fragment().
uniform vec2  viewport_size     = vec2(1280.0, 720.0);

// ── Varyings — computed in vertex(), read in fragment() ──────
varying vec2 v_player_screen_uv[4];

// ── Helpers ──────────────────────────────────────────────────
float bayer_4x4(vec2 p) {
    const float M[16] = float[16](
         0.0,  8.0,  2.0, 10.0,
        12.0,  4.0, 14.0,  6.0,
         3.0, 11.0,  1.0,  9.0,
        15.0,  7.0, 13.0,  5.0
    );
    int x = int(mod(p.x, 4.0));
    int y = int(mod(p.y, 4.0));
    return M[y * 4 + x] / 16.0;
}

float screen_dist(vec2 a, vec2 b) {
    vec2 d = a - b;
    d.x   *= viewport_size.x / viewport_size.y;
    return length(d);
}

// ── Vertex ───────────────────────────────────────────────────
void vertex() {
    for (int i = 0; i < 4; i++) {
        vec4 clip             = PROJECTION_MATRIX * VIEW_MATRIX * vec4(player_positions[i], 1.0);
        clip.xyz             /= clip.w;
        v_player_screen_uv[i] = clip.xy * 0.5 + 0.5;
    }
}

// ── Fragment ──────────────────────────────────────────────────
void fragment() {
    float min_dist = 1.0e10;
    for (int i = 0; i < player_count; i++) {
        float d  = screen_dist(SCREEN_UV, v_player_screen_uv[i]);
        min_dist = min(min_dist, d);
    }

    float outer_edge = xray_radius + dither_band;

    if (min_dist >= outer_edge) {
        discard;
    }

    if (min_dist >= xray_radius) {
        float t         = (min_dist - xray_radius) / dither_band;
        float threshold = bayer_4x4(FRAGCOORD.xy);
        if (t >= threshold) {
            discard;
        }
    }

    float pulse = 1.0 + pulse_amount * sin(TIME * pulse_speed);
    ALBEDO      = xray_tint.rgb * pulse;
    ALPHA       = xray_tint.a;
}
