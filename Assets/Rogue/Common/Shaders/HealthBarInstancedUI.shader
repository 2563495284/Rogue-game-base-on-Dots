Shader "Custom/HealthBarInstancedUI"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _BackgroundColor("Background Color", Color) = (0.2, 0.2, 0.2, 0.8)
        _HealthColor("Health Color", Color) = (0.8, 0.2, 0.2, 1)
        _BorderColor("Border Color", Color) = (0.1, 0.1, 0.1, 1)
        _BorderWidth("Border Width", Range(0, 0.1)) = 0.02
        _CornerRadius("Corner Radius", Range(0, 0.5)) = 0.1
        _HealthBarWidth("Health Bar Width", Range(0, 1)) = 0.9
        _HealthBarHeight("Health Bar Height", Range(0, 1)) = 0.6
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" "IgnoreProjector"="True" }
        
        Pass
        {
            Name "HealthBarInstanced"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest Always
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            // Instance属性
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _HealthData)
                UNITY_DEFINE_INSTANCED_PROP(float4, _PositionData)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorOverride)
                UNITY_DEFINE_INSTANCED_PROP(float4, _UITransform)
            UNITY_INSTANCING_BUFFER_END(Props)
            
            sampler2D _MainTex;
            float4 _BackgroundColor;
            float4 _HealthColor;
            float4 _BorderColor;
            float _BorderWidth;
            float _CornerRadius;
            float _HealthBarWidth;
            float _HealthBarHeight;
            
            // 圆角矩形SDF函数
            float roundedBoxSDF(float2 centerPos, float2 size, float radius)
            {
                return length(max(abs(centerPos) - size + radius, 0.0)) - radius;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                // 获取instance数据
                float4 uiTransform = UNITY_ACCESS_INSTANCED_PROP(Props, _UITransform);
                
                // 计算屏幕位置
                float2 screenPos = uiTransform.xy;
                float2 size = uiTransform.zw;
                
                // 顶点变换
                float2 localPos = (v.vertex.xy - 0.5) * size;
                float2 finalScreenPos = screenPos + localPos;
                
                // 转换为NDC坐标
                float2 ndc = (finalScreenPos / _ScreenParams.xy) * 2.0 - 1.0;
                ndc.y *= -1;
                
                o.vertex = float4(ndc, 0, 1);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                // 获取instance数据
                float4 healthData = UNITY_ACCESS_INSTANCED_PROP(Props, _HealthData);
                float4 colorOverride = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorOverride);
                
                float healthPercent = healthData.x;
                float2 uv = i.uv;
                float2 center = uv - 0.5;
                
                // 计算边框
                float borderDist = roundedBoxSDF(center, float2(0.5, 0.5), _CornerRadius);
                float borderMask = smoothstep(0.0, _BorderWidth, -borderDist);
                
                // 计算血条区域
                float2 healthBarSize = float2(_HealthBarWidth, _HealthBarHeight) * 0.5;
                float healthBarDist = roundedBoxSDF(center, healthBarSize, _CornerRadius * 0.5);
                float healthBarMask = smoothstep(0.0, 0.01, -healthBarDist);
                
                // 计算血量条
                float healthBarLeft = -healthBarSize.x;
                float healthBarRight = healthBarLeft + (_HealthBarWidth * healthPercent);
                float healthMask = (uv.x >= (0.5 + healthBarLeft) && uv.x <= (0.5 + healthBarRight)) ? 1.0 : 0.0;
                
                // 颜色混合
                float4 finalColor = _BackgroundColor;
                float4 healthColor = lerp(_HealthColor, colorOverride, colorOverride.a);
                finalColor = lerp(finalColor, healthColor, healthMask * healthBarMask);
                finalColor = lerp(finalColor, _BorderColor, borderMask * (1.0 - healthBarMask));
                finalColor.a *= borderMask;
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}