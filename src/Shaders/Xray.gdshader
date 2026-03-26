shader_type spatial;

render_mode cull_back;

// ── Uniforms ─────────────────────────────────────────────────
uniform vec3  player_positions[4];
uniform int   player_count      = 1;
uniform float xray_radius       : hint_range(0.01, 0.5)  = 0.01;
uniform float dither_band       : hint_range(0.005, 0.2) = 0.2;
uniform vec2  viewport_size     = vec2(1280.0, 720.0);

uniform sampler2D albedo_texture : source_color, hint_default_white;
uniform vec4      albedo_tint    : source_color = vec4(1.0);
uniform sampler2D normal_texture : hint_normal;
uniform float     metallic       : hint_range(0.0, 1.0) = 0.0;
uniform float     roughness      : hint_range(0.0, 1.0) = 0.8;

// ── Varyings ─────────────────────────────────────────────────
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
    // ── X-ray discard ─────────────────────────────────────────
    float min_dist = 1.0e10;
    for (int i = 0; i < player_count; i++) {
        float d  = screen_dist(SCREEN_UV, v_player_screen_uv[i]);
        min_dist = min(min_dist, d);
    }

    float outer_edge = xray_radius + dither_band;

    if (min_dist < xray_radius) {
        discard;
    } else if (min_dist < outer_edge) {
        float t = (min_dist - xray_radius) / dither_band;
        if (t < bayer_4x4(FRAGCOORD.xy)) {
            discard;
        }
    }

    // ── Surface ───────────────────────────────────────────────
    vec4 tex  = texture(albedo_texture, UV);
    ALBEDO    = tex.rgb * albedo_tint.rgb;
    ALPHA     = tex.a   * albedo_tint.a;
    METALLIC  = metallic;
    ROUGHNESS = roughness;
    NORMAL_MAP = texture(normal_texture, UV).rgb;
}
