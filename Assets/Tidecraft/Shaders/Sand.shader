Shader "Tidecraft/Sand"
{
 Properties { _BaseColor("Tint", Color) = (1,1,1,1) }
 SubShader
 {
  Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   struct A { float4 p:POSITION; float3 n:NORMAL; half4 c:COLOR; };
   struct V { float4 p:SV_POSITION; float3 w:TEXCOORD0; float3 n:TEXCOORD1; half4 c:COLOR; };
   V vert(A a) { V v; v.w=TransformObjectToWorld(a.p.xyz); v.p=TransformWorldToHClip(v.w); v.n=TransformObjectToWorldNormal(a.n); v.c=a.c; return v; }
   half4 frag(V v):SV_Target
   {
    float3 n=normalize(v.n); Light l=GetMainLight(TransformWorldToShadowCoord(v.w));
    half diffuse=saturate(dot(n,l.direction));
    half3 light=SampleSH(n)+l.color*(0.22+0.78*diffuse)*lerp(0.55,1,l.shadowAttenuation);
    float noise=frac(sin(dot(floor(v.w.xz*90),float2(12.9898,78.233)))*43758.5453);
    return half4(v.c.rgb*_BaseColor.rgb*light*(0.97+0.06*noise),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
}
