Shader "Custom/Sprite/Indirect Instanced"
{
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

            struct Sprite
            {
                float2 position;
                /*float opacity;*/
                float rotation;
                int index;
                /*float2 scale;
                float dummy;*/
            };

            StructuredBuffer<Sprite> _SpriteBlock;
            StructuredBuffer<float2> _PosBlock;

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

                if (sprite.index == -1)
                    return _f;

                _f.vertex = UnityObjectToClipPos(_MainPos + float4(sprite.position.x + uv.x, sprite.position.y + uv.y, -sprite.position.y / 1024.0f, 1.0f));
                _f.uv = uv;
                return _f;
            }

            fixed4 frag(f _f) : SV_Target
            {
                InitIndirectDrawArgs(0);

                uint instanceID = GetIndirectInstanceID(_f.svInstanceID);
                Sprite sprite = _SpriteBlock[instanceID];

                if (sprite.index == -1)
                    return fixed4(0, 0, 0, 0);

                float3 uv = float3(_f.uv, sprite.index);
                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv) * _MainTint;
                col.rgb *= col.a;

                return col;
            }
            ENDCG
        }
    }
}
