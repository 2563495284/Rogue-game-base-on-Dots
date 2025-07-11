Shader "Custom/DamageNumber"
{
    Properties
    {
        _MainTex("Atlas Texture", 2D) = "white" {}
        _AtlasColumns("Columns", Int) = 10
        _AtlasRows("Rows", Int) = 1
    }
    
    SubShader
    {
        Name "DamageNumber"
        Tags { "RenderType"="Transparent" "Queue"="Overlay" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "UnityInstancing.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                uint vid : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            // 实例数据：xy = worldPos , z = scale , w = packed(style<<4 | digit)
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstData)
            UNITY_INSTANCING_BUFFER_END(Props)
            StructuredBuffer<float2> textUv;  // UV坐标缓冲区
            sampler2D _MainTex;
            int _AtlasColumns;
            int _AtlasRows;
            
            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                float4 instData = UNITY_ACCESS_INSTANCED_PROP(Props, _InstData);
                float2 worldPos = instData.xy;
                float  scale    = instData.z;
                uint packed     = (uint)instData.w;
                uint digitIndex = packed & 0xF;
                uint styleIndex = (packed >> 4) & 0xF;
                
                // 使用顶点ID获取本地顶点位置 (quad的四个顶点)
                float2 localVertex = v.vertex.xy;
                
                // 应用缩放
                localVertex *= scale;
                
                // Billboard效果 - 将2D位置转换为3D世界位置
                float4 worldPos3D = float4(worldPos.x + localVertex.x, worldPos.y + localVertex.y, 0, 1);
                o.vertex = mul(UNITY_MATRIX_VP, worldPos3D);
                
                // 根据样式索引、数字索引和顶点ID获取UV坐标
                // 计算方式：index = styleIndex * 40 + digitIndex * 4 + vertexID
                int uvIndex = styleIndex * 40 + digitIndex * 4 + v.vid;
                o.uv = textUv[uvIndex];
                
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - 0.01);
                return col;
            }
            ENDCG
        }
    }
}