Shader "Tidecraft/Water"
{
 Properties { _Deep("Deep",Color)=(.08,.35,.43,1) _Shallow("Shallow",Color)=(.4,.8,.75,1) _Foam("Foam",Color)=(.9,1,.95,1) }
 SubShader
 {
  Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }
  Pass
  {
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Deep, _Shallow, _Foam;
   CBUFFER_END
   struct A { float4 p:POSITION; half4 c:COLOR; };
   struct V { float4 p:SV_POSITION; float3 w:TEXCOORD0; half4 c:COLOR; };
   V vert(A a) { V v; v.w=TransformObjectToWorld(a.p.xyz); v.p=TransformWorldToHClip(v.w); v.c=a.c; return v; }
   half4 frag(V v):SV_Target
   {
    float t=_Time.y; float d=v.c.r;
    float waves=sin(v.w.x*2+v.w.z*1.4-t*1.3)*sin(v.w.z*3-t*.8);
    half3 col=lerp(_Shallow.rgb,_Deep.rgb,saturate(d*.48));
    col+=waves*.025;
    float foam=(1-smoothstep(.02,.38,d))*smoothstep(.25,.8,sin(d*25-t*2+sin(v.w.z*2)*.8));
    float glint=pow(saturate(sin(v.w.x*2.7+v.w.z*3.3+t*1.6)*sin(v.w.z*4.1-t)),24)*.1;
    return half4(lerp(col,_Foam.rgb,foam*.8)+glint,1);
   }
   ENDHLSL
  }
 }
}
