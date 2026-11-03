Shader "Tidecraft/Surge"
{
 SubShader
 {
  Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 p:POSITION; half4 c:COLOR; };
   struct V { float4 p:SV_POSITION; float3 w:TEXCOORD0; half4 c:COLOR; };
   V vert(A a) { V v; v.w=TransformObjectToWorld(a.p.xyz); v.p=TransformWorldToHClip(v.w); v.c=a.c; return v; }
   half4 frag(V v):SV_Target
   {
    float ripple=sin(v.w.x*7+v.w.z*2-_Time.y*5)*sin(v.w.z*9+_Time.y*3);
    return half4(v.c.rgb*(1+ripple*.055),v.c.a);
   }
   ENDHLSL
  }
 }
}
