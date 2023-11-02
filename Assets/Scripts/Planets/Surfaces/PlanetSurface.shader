Shader "Custom/PlanetSurface"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

        [Header(Noise)]
        [NoScaleOffset] _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseScale("Noise Scale", Float) = 1
        _NoiseScale2("Noise Scale 2", Float) = 1

        [Header(Other)]
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.5

        #include "../Include/noise.gcinc"

        sampler2D _MainTex;

        half _Glossiness;
        half _Metallic;

        float3 _HeightMinMax;

		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float4 terrainData;
			float3 vertPos;
			float3 normal;
			float4 tangent;
		};

        float easeInOut(float x, float r) {
            if (x < 0.5) {
                return (2 * r) * pow(x, r);
            } else {
                return 1 - pow(-2 * x + 2, r) / 2;
            } 
        }
        
        float3 shiftColor(float3 startColor, float3 endColor, float dist) {
            float3 returnColor;
            returnColor.x = startColor.x + ((endColor.x - startColor.x) * dist);
            returnColor.y = startColor.y + ((endColor.y - startColor.y) * dist);
            returnColor.z = startColor.z + ((endColor.z - startColor.z) * dist);
            return returnColor;
        }


		void vert (inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.vertPos = v.vertex;
			o.normal = v.normal;
			o.terrainData = v.texcoord;
			o.tangent = v.tangent;

			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			float3 bodyWorldCentre = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
			float camRadiiFromSurface = length(bodyWorldCentre - _WorldSpaceCameraPos.xyz) - 1;
			float3 viewDir = normalize(worldPos - _WorldSpaceCameraPos.xyz);
			float3 normWorld = normalize(mul(unity_ObjectToWorld, float4(v.normal,0)));
		}

        // Settings
        float _ShoreHeight;
        float _PlanesHeight;
        float _MountainHeight;

        // Noise / Biome settings
        float _BiomeSize = 1;
        float _BiomeSharpness = 1;
        
        float _NoiseShift = .2;
        float _NoiseSharpness = 1;

        // Colors
        float3 _FlatUnderwater;
        float3 _SteepUnderwater;

        float3 _Shores;
        float3 _SteepA;
        float3 _SteepB;
        float3 _GrassA;
        float3 _GrassB;
        float3 _Snow;

        // Flat To Steep
        float _SteepnessThreshold;
        float _SteepnessSharpness;
        float _SteepnessDropoff;

        sampler2D _NoiseTex;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 sphereNormal = normalize(IN.vertPos);
            float steepness = 1 - remap01(dot(sphereNormal, IN.normal), 0, _SteepnessThreshold);
            steepness = remap01(steepness, 1 - _SteepnessDropoff, _SteepnessDropoff);
            steepness = pow(steepness, _SteepnessSharpness); 
            
            float height = length(IN.vertPos);
            float heightFromShore = height - _HeightMinMax.y;

            float3 flatColor = _GrassA;

            float shoreWeight = 1 - remap01(heightFromShore, _ShoreHeight, _PlanesHeight);
            float underwaterWeight = sqrt(1 - remap01(height, _HeightMinMax.x, _HeightMinMax.y + _ShoreHeight)); 
            flatColor = lerp(flatColor, _Shores, shoreWeight * shoreWeight);
            flatColor = lerp(flatColor, _FlatUnderwater, underwaterWeight);

            float3 steepColor = _SteepA;

            steepColor = lerp(steepColor, _SteepUnderwater, underwaterWeight * underwaterWeight);

            float3 compositeColor;

            if (steepness > .7) 
            {
                compositeColor = steepColor;
            } else 
            {
                compositeColor = flatColor;
            }

            compositeColor = shiftColor(flatColor, steepColor, steepness);

            o.Albedo = compositeColor;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
