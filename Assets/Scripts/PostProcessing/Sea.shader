Shader "Custom/Sea"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewVector : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

			    float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
			    o.viewVector = mul(unity_CameraToWorld, float4(viewVector,0));
                return o;
            }

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 originalCol = tex2D(_MainTex, i.uv);
                float nonLinearDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float sceneDepth = LinearEyeDepth(nonLinearDepth) * length(i.viewVector);

                return originalCol;
                return sceneDepth / 1000;
            }
            ENDCG
        }
    }
}
