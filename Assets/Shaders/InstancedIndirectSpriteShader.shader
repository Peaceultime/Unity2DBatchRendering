Shader"Custom/Sprite/Indirect Instanced"
{
    Properties
    {
        _NoiseTex("Noise texture", 2D) = "white" {}
        _FogWhite("Fog White color", Color) = (1, 1, 1, 1)
        _FogBlack("Fog Black color", Color) = (0, 0, 0, 1)
        _FadeProgress("Fade Progress", Float) = 0
        _Size("Size", Int) = 0
        _Strength("Strength", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }
        LOD 200
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            #pragma enable_d3d11_debug_symbols

            UNITY_DECLARE_TEX2DARRAY(_MainTex);
            float4 _MainTint;
            float4 _MainPos;
            float _FadeProgress;
            int _Size;
            float _Strength;
            
            fixed4 _FogWhite;
            fixed4 _FogBlack;

            struct Sprite
            {
                float2 position;
                int index;
                int distance;
            };
            struct Visibility
            {
                bool visible;
                bool explored;
            };

            StructuredBuffer<Sprite> _SpriteBlock;
            StructuredBuffer<Visibility> _VisibilityBlock;
            StructuredBuffer<float2> _PosBlock;

            sampler2D _NoiseTex;

            struct f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                uint svInstanceID: SV_InstanceID;
            };

            f vert(uint svVertexID: SV_VertexID, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);

                f _f;

                uint instanceID = GetIndirectInstanceID(svInstanceID);
                _f.svInstanceID = svInstanceID;

                float2 uv = _PosBlock[GetIndirectVertexID(svVertexID)];
                Sprite sprite = _SpriteBlock[instanceID];
                Visibility visibility = _VisibilityBlock[instanceID];

                _f.vertex = UnityObjectToClipPos(_MainPos + float4(sprite.position.x + uv.x, sprite.position.y + uv.y, -sprite.position.y / 1024.0f, 1.0f));
                _f.uv = uv;
                return _f;
            }

            float hash(float2 uv)
            {
                return frac(sin(7.289 * uv.x + 11.23 * uv.y) * 23758.5453);
            }

            float unlerp(float x, float a, float b)
            {
                return (x - a) / (b - a);
            }
            
            float fade(float noise, float fadeValue, float fadeContrast)
            {
                return saturate(fadeValue*(fadeContrast+1)+(noise-1)*fadeContrast);
            }

            float pixelNoise(float2 uv, int power, float offset)
            {
                int p = pow(2, power);
                uv += floor(p * offset) / p;
                return tex2D(_NoiseTex, floor(uv * p) / p).r;
            }

            fixed4 frag(f _f) : SV_Target
            {
                InitIndirectDrawArgs(0);

                uint instanceID = GetIndirectInstanceID(_f.svInstanceID);
                Sprite sprite = _SpriteBlock[instanceID];

                if (sprite.index == -1)
                    return fixed4(0, 0, 0, 0);

                float3 uv = float3(_f.uv, sprite.index);
                fixed4 tex = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv) * _MainTint;

                // I suck at vfx
                float progress = clamp(unlerp(sprite.distance - _FadeProgress, 0, 8), 0, 1);

                float noise = 1;

                [unroll]
                for(int i = 0; i < 3; i ++)
                {
                    noise *= pixelNoise(_f.vertex.xy / _Size, 5, _Time.y * (hash(float2(i, i)) - 0.5));
                }

                float fadeVal = fade(noise, progress, _Strength);

                //fixed4 col = lerp(tex, lerp(_FogBlack, _FogWhite, noise), fadeVal);
                fixed4 col = tex;

                //col.rgb *= col.a;

                return col;
            }
            ENDCG
        }
    }
}
