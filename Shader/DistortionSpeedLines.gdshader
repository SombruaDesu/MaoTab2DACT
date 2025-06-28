shader_type canvas_item;

/* 噪声贴图（建议 512~1024 灰度或 RG 噪声，Repeat 打开） */
uniform sampler2D noise : repeat_enable, filter_linear_mipmap;

/* ----------- 可调参数 ----------- */
uniform float rain_speed       : hint_range(0.1, 10.0) = 4.0;   // 垂直下落速度
uniform float wind             : hint_range(-1.0,  1.0) = 0.15; // 横向风（负=向左）
uniform float streak_density   : hint_range(0.2,  5.0) = 1.8;   // 竖向重复次数
uniform float streak_width     : hint_range(0.0,  1.0) = 0.25;  // 线条粗细
uniform float distortion_power : hint_range(0.0, 0.05) = 0.015; // 折射强度
uniform float blur_strength    : hint_range(0.0, 0.02) = 0.006; // 模糊半径

/* 整体总开关 —— 0 没有特效，1 全强度 */
uniform float effect_strength  : hint_range(0.0, 1.0) = 1.0;

/* 可选：第二层大水滴 */
uniform bool  big_drops_enable = true;
uniform float big_drops_scale  : hint_range(0.5, 4.0) = 1.5;
uniform float big_drops_power  : hint_range(0.0, 0.05) = 0.02;

/* 屏幕纹理（Godot 自动提供） */
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

/* ---------- 生成雨线遮罩 ---------- */
float rain_streak_mask(vec2 uv) {
    vec2 n_uv = vec2(uv.x, uv.y * streak_density);   // Y 向拉伸
    n_uv.y += TIME * -rain_speed;                    // 向上滚动 → 视觉向下
    n_uv.x += TIME * wind;                           // 风偏移
    float n    = texture(noise, n_uv).r;             // 灰度
    float mask = smoothstep(0.5 - streak_width,
                            0.5 + streak_width, n);  // 0/1 线条
    return mask;                                     // 0=无雨线 1=雨线内部
}

/* ---------- 生成随机大水滴 ---------- */
float big_drops(vec2 uv) {
    vec2 n_uv = uv * big_drops_scale
              + TIME * vec2(wind, rain_speed * 0.5);
    return texture(noise, n_uv).r;                   // 灰度 0-1
}

/* ---------------- 主函数 ---------------- */
void fragment() {
    /* 1. 雨线强度 0-1 */
    float streak = rain_streak_mask(UV);

    /* 2. 基础折射方向（顺风向下） */
    vec2 dir = normalize(vec2(wind, 1.0));

    /* 3. 位移量（折射） */
    float displacement = streak * distortion_power * effect_strength;

    /* 4. 叠加大水滴位移 */
    if (big_drops_enable) {
        float drops     = big_drops(UV);
        float drop_mask = pow(max(drops - 0.5, 0.0) * 2.0, 2.0); // 稀疏
        displacement   += drop_mask * big_drops_power * effect_strength;
    }

    /* 5. 扭曲取样屏幕 */
    vec2 distorted_uv = UV + dir * displacement;
    vec4 base_color   = texture(SCREEN_TEXTURE, distorted_uv);

    /* 6. 环形模糊（营造雨雾） */
    vec4 blur_color = vec4(0.0);
    const int SAMPLES = 8;
    for (int i = 0; i < SAMPLES; i++) {
        float angle  = (float(i) / float(SAMPLES)) * PI * 2.0;
        vec2  offset = vec2(cos(angle), sin(angle)) * blur_strength;
        blur_color  += texture(SCREEN_TEXTURE,
                               distorted_uv + offset * streak);
    }
    blur_color /= float(SAMPLES);

    /* 7. 根据雨线强度把模糊混进来 */
    vec4 rain_effect = mix(base_color, blur_color, streak * 0.6);

    /* 8. 与原始画面做总混合（effect_strength 全局开关） */
    vec4 original = texture(SCREEN_TEXTURE, UV);
    COLOR = mix(original, rain_effect, effect_strength);
}
