﻿Shader "FluidAutomata/Standard"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _FluidTexture ("Fluid Texture", 2D) = "white" {}
        _AlphaMultiplier ("Alpha Multiplier", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _FluidTexture;

        struct Input
        {
            float2 uv_MainTex;
        };

        float _Glossiness;
        float _Metallic;
        float4 _Color;
        float _AlphaMultiplier;


        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float4 fluid = tex2D(_FluidTexture, IN.uv_MainTex);
            o.Albedo = _Color.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = _Color.rgb;
            o.Alpha = abs(fluid.a) * _AlphaMultiplier;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
