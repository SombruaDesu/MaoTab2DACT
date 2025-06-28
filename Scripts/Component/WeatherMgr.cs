/*
 * @Author: MaoT
 * @Description: 天气管理器，处理天气相关视觉特效、逻辑
 */

using System;
using Godot;

namespace MaoTab.Scripts.Component;

public partial class WeatherMgr : Node2D
{
    // 跟踪对象
    private Node2D _followTarget;
    
    private Vector2 _followPosition
    {
        set;
        get => _followTarget?.Position + new Vector2(0,-500) ?? field;
    }
    
    [Export] private GpuParticles2D RainParticles;
    [Export] private GpuParticles2D RainNoCollisionParticles;

    private ParticleProcessMaterial ParticleMaterial;
    private ParticleProcessMaterial NoCollisionParticleMaterial;
        
    [Export] Curve TrailLifetimeWeightCurve;
    [Export] Curve SpeedWeightCurve;
    [Export] Curve GravityWeightCurve;
    [Export] Curve RippleSpeedCurve;

    [Export] private AnimationPlayer LightningAnimation;
    
    private float currentStrength;
    
    /// <summary>
    /// 当前粒子处理效果
    /// </summary>
    private float currentSpeed;
    
    [Export] private ColorRect PostLayer;
    private ShaderMaterial PostFxMaterial;
    
    private ShaderMaterial _seaFaceShader;
    private Sprite2D       _seaFace;

    public bool HasLightning;
    
    public void Lightning()
    {
        LightningAnimation.Play("Lightning");
    }
    
    public void Init(Node2D camera)
    {
        _followTarget = camera;
        
        // 获取粒子发射材质
        ParticleMaterial            = RainParticles.ProcessMaterial as ParticleProcessMaterial;
        NoCollisionParticleMaterial = RainNoCollisionParticles.ProcessMaterial as ParticleProcessMaterial;
        
        PostFxMaterial = PostLayer.Material as ShaderMaterial;
    }

    public void Refresh(Sprite2D seaFace)
    {
        if(seaFace == null)
        {
            _seaFace = null;
            _seaFaceShader =  null;
            return;
        }

        _seaFace = seaFace;
        _seaFaceShader  = seaFace.Material as ShaderMaterial;
    }

    private void SetGravity(float value)
    {
        if(value.AetF(ParticleMaterial.Gravity.X,0.01f)) return;
        ParticleMaterial.Gravity            = ParticleMaterial.Gravity with { X = value };
        NoCollisionParticleMaterial.Gravity = ParticleMaterial.Gravity with { X = value };
    }
    
    private void SetSpeedScale(float value)
    {
        if(value.AetF( (float)RainParticles.SpeedScale  ,0.01f)) return;
        RainParticles.SpeedScale            = value;
        RainNoCollisionParticles.SpeedScale = value;
    }
    
    private void SetLifeTime(float value)
    {
        if(value.AetF( (float)RainParticles.TrailLifetime  ,0.01f)) return;
        RainParticles.TrailLifetime            = value;
        RainNoCollisionParticles.TrailLifetime = value;
    }

    private double timer;
    private double PreprocessTimer;

    private float curPostStrength;
    
    /// <summary>
    /// 着色器驱动帧
    /// </summary>
    private float rippleClock;
    
    private double lightningTimer;
    
    private Random rand = new();
    private double nextlightningTime;
    
    public void Tick()
    {
        if (_seaFace != null)
        {
            var shaderRippleSpeed = RippleSpeedCurve.Sample(currentStrength);

            rippleClock += (float)Game.PhysicsDelta * shaderRippleSpeed;
            _seaFaceShader.SetShaderParameter("ripple_clock", rippleClock);
            _seaFaceShader.SetShaderParameter("rain_speed", shaderRippleSpeed);
        }

        if (HasLightning)
        {
            lightningTimer += Game.PhysicsDelta;
            if (lightningTimer >= nextlightningTime)
            {
                Lightning();
                nextlightningTime = rand.NextDouble() * 5;
                lightningTimer    = 0;
            }
        }
        
        // 降低更新频率
        timer += Game.PhysicsDelta;
        if (timer >= 0.1f)
        {
            PreprocessTimer += Game.PhysicsDelta;
            if (currentStrength > 0.1f && PreprocessTimer >= 5f && !RainParticles.Preprocess.AetD(2,0.1d))
            {
                PreprocessTimer = 0;
                
                RainParticles.Preprocess            = 2f;
                RainNoCollisionParticles.Preprocess = 2f;
            }
            
            timer = 0;
        }
        else
        {
            return;
        }
        
        if (_followTarget == null) return;
        
        var zoomAbsPercentage = 1 + Game.Camera.Zoom.X / 1.6f; // 缩放的比例计算实际上是反的，所以需要加值
        
        var resPosition    = new Vector2(_followPosition.X,_followPosition.Y * zoomAbsPercentage);
        var resExtents     = new Vector3(600 * (zoomAbsPercentage + 1), 25.0f, 1);

        // 根据镜头缩放扩张特效范围
        ParticleMaterial.EmissionBoxExtents         = resExtents;
        NoCollisionParticleMaterial.EmissionBoxExtents = resExtents;
        
        // 将粒子效果跟随镜头
        if (!RainParticles.Position.AetV2(resPosition,1))
        {
            RainParticles.Position            = resPosition;
        }
        
        if (!RainNoCollisionParticles.Position.AetV2(resPosition,1))
        {
            RainNoCollisionParticles.Position            = resPosition;
        }
        
        // 处理粒子播放
        currentStrength = (float)Mathf.Lerp(currentStrength , Game.WeatherStrength, Game.PhysicsDelta * 5);
        currentSpeed    = (float)Mathf.Lerp(currentSpeed , Game.WeatherSpeed, Game.PhysicsDelta * 10);

        if (currentSpeed <= 0.3f)
        {
            if (currentSpeed.AetF(0, 0.01f))
            {
                currentSpeed = 0;
            }
            ParticleMaterial.CollisionMode = ParticleProcessMaterial.CollisionModeEnum.Disabled;
        }
        else
        {
            ParticleMaterial.CollisionMode = ParticleProcessMaterial.CollisionModeEnum.HideOnContact;
        }
        
        var weight = currentStrength / 100f;
        var runningSpeed = currentSpeed / 100f;
        
        if (weight >= 0.8f)
        {
            curPostStrength = Mathf.Lerp(curPostStrength, 1, (float)Game.PhysicsDelta* 5);
            PostFxMaterial.SetShaderParameter("effect_strength", curPostStrength);
            
            Game.Camera.SetFixedTrauma(weight * 0.2f);
        }
        else
        {
            curPostStrength = Mathf.Lerp(curPostStrength, 0, (float)Game.PhysicsDelta* 5);
            PostFxMaterial.SetShaderParameter("effect_strength", curPostStrength);
            
            Game.Camera.SetFixedTrauma(0);
        }
        
        float speed   = SpeedWeightCurve.Sample(weight) * Mathf.Clamp(runningSpeed,0.01f,1);
        float gravity = GravityWeightCurve.Sample(weight);
        float time    = TrailLifetimeWeightCurve.Sample(weight);

        if (weight.AetF(0, 0.01f))
        {
            RainParticles.Emitting   = false;
            RainParticles.Preprocess = 0f;
            
            RainNoCollisionParticles.Emitting = false;
            RainNoCollisionParticles.Preprocess = 0f;
        }
        else
        {
            RainParticles.Emitting            = true;
            RainNoCollisionParticles.Emitting = true;
        }
        
        SetSpeedScale(speed);
        SetGravity(gravity);
        SetLifeTime(time);
        
        if(_seaFaceShader == null) return;
        
        _seaFaceShader.SetShaderParameter("ripple_enabled", !weight.AetF(0, 0.01f));
        _seaFaceShader.SetShaderParameter("ripple_spawn_chance", weight);
        _seaFaceShader.SetShaderParameter("shader_speed", runningSpeed);
    }
}