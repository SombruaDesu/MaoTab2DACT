shader_type canvas_item;

/* ------------- 全局速度 ------------- */
uniform float shader_speed           = 1.0;   // 0 = 静止
uniform float ripple_clock;                     // 脚本传入
uniform vec2  inv_scale              = vec2(1); // 脚本传入

/* ------------- 常规参数 ------------- */
uniform sampler2D screen_texture : hint_screen_texture;
uniform float     y_scale               = 1.0;
uniform sampler2D distortion_noise : repeat_enable;
uniform float     distortion_strength   = 0.02;
uniform float     distortion_speed      = 0.05;
uniform sampler2D reflection_noise : repeat_enable;
uniform float     reflection_speed      = 0.01;

/* ------------- 涟漪控制 ------------- */
uniform bool  ripple_enabled      = true;

uniform float rain_density        = 25.0;
uniform float ripple_spawn_chance = 1.0;   // 0-1
uniform float ripple_alpha_rand   = 0.6;

uniform float rain_speed          = 1.6;
uniform float ripple_amplitude    = 0.03;
uniform float ripple_thickness    = 0.15;
uniform float ripple_brightness   = 0.35;

uniform float ripple_size_min     = 0.6;
uniform float ripple_size_max     = 1.2;

/* ------------- 透视 ------------- */
uniform bool  ripple_perspective_enabled = true;
uniform float ripple_size_near = 1.4;
uniform float ripple_size_far  = 0.6;

/* ------------- 淡出 ------------- */
uniform float ripple_fade_begin = 0.75;

/* ------------- 每圈是否换位置 ------------- */
uniform bool ripple_move_each_cycle = true;

/* ------------- 更轻量的 hash ------------- */
/* 出自 Dave Hoskins，小幅修改 */
float hash12_fast(in vec2 p){
    p = fract(p*0.3183099 + vec2(0.71,0.113));
    p *= 17.0;
    return fract(p.x*p.y*(p.x+p.y));
}
vec2 hash22_fast(in vec2 p){
    float n = hash12_fast(p);
    return fract(vec2(n, n*34.0));
}

/* ------------- 常量邻居偏移表 (编译器会展开) ------------- */
const vec2 OFFSETS[9] = vec2[](
    vec2(-1,-1), vec2( 0,-1), vec2( 1,-1),
    vec2(-1, 0), vec2( 0, 0), vec2( 1, 0),
    vec2(-1, 1), vec2( 0, 1), vec2( 1, 1)
);

void fragment()
{
    /* ========== 预计算 ========== */
    float T = TIME * shader_speed;

    /* 折射噪声只采样一次 */
    float distortion_value = texture(distortion_noise,
        vec2(UV.x * inv_scale.x - T*distortion_speed, UV.y)).r;
    distortion_value = (distortion_value - 0.5) * distortion_strength;

    vec2  total_disp = vec2(0.0);
    float total_ring = 0.0;

    /* ========== 涟漪 ========== */
    if (ripple_enabled){

        vec2 grid_uv   = (UV * inv_scale) * rain_density;
        vec2 cell_id   = floor(grid_uv);
        vec2 local_uv  = fract(grid_uv);

        /* 遍历 3×3 邻居 */
        for (int i = 0; i < 9; i++){
            vec2 nbr_id  = cell_id + OFFSETS[i];

            /* 随机丢弃一部分格子，直接用乘法，无分支 */
            float spawn_ok = step(hash12_fast(nbr_id + 19.73), ripple_spawn_chance);
            if (spawn_ok == 0.0) { continue; }

            /* 相位 / 圈号 / 进度 */
            float phase      = hash12_fast(nbr_id);
            float cycle_idx  = floor(ripple_clock + phase);
            float life       = fract(ripple_clock + phase); // 0-1

            /* 每圈重新随机 α / size / 位置 */
            float alpha_seed = ripple_move_each_cycle ? cycle_idx + 88.31 : 88.31;
            float size_seed  = ripple_move_each_cycle ? cycle_idx + 37.17 : 37.17;
            float pos_seed   = ripple_move_each_cycle ? cycle_idx         : 0.0;

            float alpha_rand = 1.0 - ripple_alpha_rand * hash12_fast(nbr_id + alpha_seed);
            vec2  drop_cen   = hash22_fast(nbr_id + pos_seed) * 0.8 + 0.1;
            float size_rng   = mix(ripple_size_min, ripple_size_max,
                                   hash12_fast(nbr_id + size_seed));

            /* 透视缩放 */
            float persp_scale = 1.0;
            if (ripple_perspective_enabled){
                float drop_world_y = (nbr_id.y + drop_cen.y) / rain_density;
                persp_scale = mix(ripple_size_far, ripple_size_near, drop_world_y);
            }

            float radius = life * size_rng * persp_scale;

            /* 把早期 distance > radius+thickness 的像素直接跳过 */
            vec2  nbr_uv = local_uv - OFFSETS[i];
            vec2  v      = nbr_uv - drop_cen;
            float dist   = length(v);

            float edge_min = radius - ripple_thickness;
            float edge_max = radius + ripple_thickness;
            float mask     = step(edge_min, dist) * step(dist, edge_max); // 仅在环附近为 1

            if (mask == 0.0) { continue; }

            /* 更紧凑的环形权重 */
            float t = abs(dist - radius) / ripple_thickness; // 0 在中心
            float ring = (1.0 - t) * (1.0 - t);              // 近似 smoothstep

            /* 淡出 */
            float fade = 1.0 - smoothstep(ripple_fade_begin, 1.0, life);
            ring *= fade * alpha_rand * mask;

            /* 位移累加 */
            vec2 dir = normalize(v + 1e-4);
            total_disp += dir * ring;
            total_ring += ring;
        }
        total_disp *= ripple_amplitude;
    }

    /* ========== 倒影 + sparkle ========== */
    float tex_screen_ratio = SCREEN_PIXEL_SIZE.y /
                             (TEXTURE_PIXEL_SIZE.y / y_scale);

    vec2 reflection_uv = vec2(SCREEN_UV.x,
                             SCREEN_UV.y - 2.0 * UV.y * tex_screen_ratio)
                             + vec2(distortion_value) + total_disp;

    vec4 reflection_color = texture(screen_texture, reflection_uv);

    /* 原贴图颜色 */
    vec4 base_color = texture(TEXTURE, UV);

    /* 镜面混合 */
    vec4 out_color = mix(base_color, reflection_color, 0.5);

    /* sparkle (一次采样) */
    float sparkle = texture(reflection_noise,
        vec2(UV.x*0.1 - T*reflection_speed, UV.y) + distortion_value).r;
    sparkle = smoothstep(0.8, 1.0, sparkle);
    out_color = mix(out_color, vec4(1.0), sparkle);

    /* 涟漪高光 */
    out_color = mix(out_color, vec4(1.0),
                    clamp(total_ring,0.0,1.0)*ripple_brightness);

    COLOR = out_color;
}
