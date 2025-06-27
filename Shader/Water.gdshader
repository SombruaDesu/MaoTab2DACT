shader_type canvas_item;

/* ─── 全局速度 ─── */
uniform float shader_speed = 1.0;          // 0 = 静止

/* ─── 常规参数 ─── */
uniform sampler2D screen_texture : hint_screen_texture;
uniform float     y_scale                = 1.0;
uniform sampler2D distortion_noise : repeat_enable;
uniform float     distortion_strength    = 0.02;
uniform float     distortion_speed       = 0.05;
uniform sampler2D reflection_noise : repeat_enable;
uniform float     reflection_speed       = 0.01;

/* ─── 涟漪控制 ─── */
uniform bool  ripple_enabled      = true;

uniform float rain_density        = 25.0;  // 控制一个波纹能扩散多大
uniform float ripple_spawn_chance = 1.0;   // 0-1：有多少格子真正生成
uniform float ripple_alpha_rand   = 0.6;   // 0-1：随机淡化

uniform float rain_speed          = 1.6;
uniform float ripple_amplitude    = 0.03;
uniform float ripple_thickness    = 0.15;
uniform float ripple_brightness   = 0.35;

uniform float ripple_size_min     = 0.6;   // 随机尺寸区间
uniform float ripple_size_max     = 1.2;

/* ─── 近大远小（透视） ─── */
uniform bool  ripple_perspective_enabled = true;
uniform float ripple_size_near = 1.4;      // 贴图底部（近处）的额外倍率
uniform float ripple_size_far  = 0.6;      // 顶部（远处）的额外倍率

/* ─── hash ─── */
float hash12(vec2 p){
    vec3 p3 = fract(vec3(p, p.x)*0.1031);
    p3 += dot(p3, p3.yzx+33.33);
    return fract((p3.x+p3.y)*p3.z);
}
vec2 hash22(vec2 p){
    float n = hash12(p);
    return fract(vec2(n*7.3, n*13.7));
}

void fragment()
{
    /* 统一时间缩放 */
    float T = TIME * shader_speed;

    /* 折射噪声 */
    float distortion_value = texture(distortion_noise,
        vec2(UV.x - T*distortion_speed, UV.y)).r;
    distortion_value = (distortion_value - 0.5) * distortion_strength;

    vec2  total_disp = vec2(0.0);
    float total_ring = 0.0;

    if (ripple_enabled) {

        vec2 grid_uv  = UV * rain_density;
        vec2 cell_id  = floor(grid_uv);
        vec2 local_uv = fract(grid_uv);

        for (int jy=-1; jy<=1; jy++){
            for (int jx=-1; jx<=1; jx++){

                vec2 offset  = vec2(float(jx),float(jy));
                vec2 nbr_id  = cell_id + offset;

                /* 随机跳过格子（控制数量） */
                if (hash12(nbr_id + 19.73) > ripple_spawn_chance)
                    continue;

                /* 随机透明度 */
                float alpha = 1.0 - ripple_alpha_rand * hash12(nbr_id + 88.31);

                /* 各种随机信息 */
                vec2  drop_cen = hash22(nbr_id) * 0.8 + 0.1;
                float phase    = hash12(nbr_id);
                float size_rng = mix(ripple_size_min, ripple_size_max,
                                     hash12(nbr_id + 37.17));

                /* ----------- 透视倍率 ----------- */
                float persp_scale = 1.0;
                if (ripple_perspective_enabled){
                    float drop_world_y = (nbr_id.y + drop_cen.y) / rain_density; // 0=顶 1=底
                    persp_scale = mix(ripple_size_far, ripple_size_near, drop_world_y);
                }

                /* 半径随时间(0-1)、随机尺寸、透视倍率增长 */
                float base_radius = fract(T * rain_speed + phase);
                float radius      = base_radius * size_rng * persp_scale;

                /* 计算环形掩码 */
                vec2  nbr_uv = local_uv - offset;
                float dist   = length(nbr_uv - drop_cen);

                float ring = smoothstep(radius + ripple_thickness,
                                        radius, dist) *
                             smoothstep(radius - ripple_thickness,
                                        radius, dist);

                ring *= alpha;

                if (ring > 1e-4){
                    vec2 dir = normalize(nbr_uv - drop_cen + 1e-4);
                    total_disp += dir * ring;
                }
                total_ring += ring;
            }
        }
        total_disp *= ripple_amplitude;
    }

    /* 倒影取样 */
    float tex_screen_ratio = SCREEN_PIXEL_SIZE.y /
            (TEXTURE_PIXEL_SIZE.y / y_scale);

    vec2 reflection_uv = vec2(SCREEN_UV.x,
        SCREEN_UV.y - 2.0 * UV.y * tex_screen_ratio) +
        vec2(distortion_value) + total_disp;

    vec4 reflection_color = texture(screen_texture, reflection_uv);

    /* 最终颜色 */
    vec4 tex_color = texture(TEXTURE, UV);
    vec4 out_color = mix(tex_color, reflection_color, 0.5);

    float sparkle = texture(reflection_noise,
        vec2(UV.x*0.1 - T*reflection_speed, UV.y)+distortion_value).r;
    sparkle = smoothstep(0.8, 1.0, sparkle);
    out_color = mix(out_color, vec4(1.0), sparkle);

    out_color = mix(out_color, vec4(1.0),
                    clamp(total_ring,0.0,1.0)*ripple_brightness);

    COLOR = out_color;
}
