/*
 * @Author: MaoT
 * @Description: 天气管理器，处理天气相关视觉特效、逻辑
 */

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
    [Export] private GpuParticles2D SeaFaceParticles;

    private ParticleProcessMaterial ParticleMaterial;
    private ParticleProcessMaterial NoCollisionParticleMaterial;
    private ParticleProcessMaterial SeaFaceParticleMaterial;
    
    private const float MinSpeedScale = 3.75f;
    private const float MaxSpeedScale = 35.0f;
    
    /// <summary>
    /// 雨迹重力，决定雨的方向，为0时垂直
    /// </summary>
    private const float MinGravity = 0f;
    private const float MaxGravity = 250.0f;
    
    [Export] Curve TrailLifetimeWeightCurve;
    [Export] Curve SpeedWeightCurve;
    [Export] Curve GravityWeightCurve;
    [Export] Curve RippleSpeedCurve;

    [Export] private AnimationPlayer LightningAnimation;
    
    private const float MinTrailLifetime = 0.05f;
    private const float MaxTrailLifetime = 0.3f;
    
    
    private float currentStrength;
    
    /// <summary>
    /// 当前粒子处理效果
    /// </summary>
    private float currentSpeed;
    
    
    private ShaderMaterial _seaFaceShader;

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
        SeaFaceParticleMaterial = SeaFaceParticles.ProcessMaterial as ParticleProcessMaterial;
        
        if(ParticleMaterial == null || NoCollisionParticleMaterial == null) return;
        
        RainParticles.SpeedScale    = MinSpeedScale;
        RainNoCollisionParticles.SpeedScale = MinSpeedScale;
        
        RainParticles.TrailLifetime = MinTrailLifetime;
        RainNoCollisionParticles.TrailLifetime = MinTrailLifetime;
        
        ParticleMaterial.Gravity    = ParticleMaterial.Gravity with { X = 200 };
        NoCollisionParticleMaterial.Gravity    = ParticleMaterial.Gravity with { X = 200 };
    }

    public void Refresh(float seaFacePosition,Sprite2D seaFace)
    {
        SeaFaceParticles.GlobalPosition = SeaFaceParticles.GlobalPosition with { Y = seaFacePosition };
        
        _seaFaceShader = seaFace.Material as ShaderMaterial;
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
    
    public void Tick()
    {
        var shaderRippleSpeed = RippleSpeedCurve.Sample(currentStrength);
        rippleClock += (float)Game.PhysicsDelta * shaderRippleSpeed;
        _seaFaceShader.SetShaderParameter("ripple_clock", rippleClock);
        
        // 降低更新频率
        timer += Game.PhysicsDelta;
        if (timer >= 0.1f)
        {
            PreprocessTimer += Game.PhysicsDelta;
            if (currentStrength > 0.1f && PreprocessTimer >= 5f && !RainParticles.Preprocess.AetD(2,0.1d))
            {
                PreprocessTimer                              = 0;
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
        SeaFaceParticleMaterial.EmissionBoxExtents = resExtents;
        
        // 将粒子效果跟随镜头
        RainParticles.Position            = resPosition;
        RainNoCollisionParticles.Position = resPosition;
        
        // 海平面仅更新水平轴
        SeaFaceParticles.Position = SeaFaceParticles.Position with { X = _followPosition.X };
        
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
            Game.Camera.SetFixedTrauma(weight * 0.2f);
        }
        else
        {
            Game.Camera.SetFixedTrauma(0);
        }
        
        SeaFaceParticles.SpeedScale = runningSpeed;
        
        float speed   = SpeedWeightCurve.Sample(weight) * Mathf.Clamp(runningSpeed,0.01f,1);
        float gravity = GravityWeightCurve.Sample(weight);
        float time    = TrailLifetimeWeightCurve.Sample(weight);

        if (weight.AetF(0, 0.01f))
        {
            RainParticles.Emitting   = false;
            RainParticles.Preprocess = 0f;
            
            RainNoCollisionParticles.Emitting = false;
            RainNoCollisionParticles.Preprocess = 0f;
            
            SeaFaceParticles.Emitting = false;
        }
        else
        {
            RainParticles.Emitting            = true;
            RainNoCollisionParticles.Emitting = true;
            SeaFaceParticles.Emitting = true;
        }
        
        SetSpeedScale(speed);
        SetGravity(gravity);
        SetLifeTime(time);
        
        if(_seaFaceShader == null) return;
        
        _seaFaceShader.SetShaderParameter("ripple_enabled", !weight.AetF(0, 0.01f));
        _seaFaceShader.SetShaderParameter("rain_speed", shaderRippleSpeed);
        _seaFaceShader.SetShaderParameter("ripple_spawn_chance", weight);
        _seaFaceShader.SetShaderParameter("shader_speed", runningSpeed);
    }

    private float rippleClock;
}