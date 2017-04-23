#ifndef GPUSKINNING_SURFACE
#define GPUSKINNING_SURFACE

#include "UnityStandardInput.cginc"
 
// just for NormalizePerPixelNormal()
#include "UnityStandardCore.cginc"
 
// LightingStandard(), LightingStandard_GI(), LightingStandard_Deferred() and
// struct SurfaceOutputStandard are defined here.
#include "UnityPBSLighting.cginc"
 
struct appdata_vert {
   float4 vertex : POSITION;
   half3 normal : NORMAL;
   float2 uv0 : TEXCOORD0;
   float4 uv1 : TEXCOORD1;
   float4 uv2 : TEXCOORD2;
   float4 tangent : TANGENT;
   UNITY_VERTEX_INPUT_INSTANCE_ID
};
 
struct Input {
   float4 texcoords;
};
 
void vert (inout appdata_vert v, out Input o) {
	UNITY_SETUP_INSTANCE_ID(v);
   UNITY_INITIALIZE_OUTPUT(Input,o);
   o.texcoords.xy = TRANSFORM_TEX(v.uv0, _MainTex); // Always source from uv0
}
 
void surf (Input IN, inout SurfaceOutputStandard o) {
   float4 texcoords = IN.texcoords;
   half alpha = Alpha(texcoords.xy);
#if defined(_ALPHATEST_ON)
   clip (alpha - _Cutoff);
#endif
   o.Albedo = _Color.rgb * tex2D (_MainTex, texcoords.xy).rgb;
#ifdef _NORMALMAP
   o.Normal = NormalInTangentSpace(texcoords);
   o.Normal = NormalizePerPixelNormal(o.Normal);
#endif
   o.Emission = Emission(texcoords.xy);
   half2 metallicGloss = MetallicGloss(texcoords.xy);
   o.Metallic = metallicGloss.x; // _Metallic;
   o.Smoothness = metallicGloss.y; // _Glossiness;
   o.Occlusion = Occlusion(texcoords.xy);
   o.Alpha = alpha;
}

void surfSpecular (Input IN, inout SurfaceOutputStandardSpecular o) {
   float4 texcoords = IN.texcoords;
   half alpha = Alpha(texcoords.xy);
#if defined(_ALPHATEST_ON)
   clip (alpha - _Cutoff);
#endif
   o.Albedo = _Color.rgb * tex2D (_MainTex, texcoords.xy).rgb;
#ifdef _NORMALMAP
   o.Normal = NormalInTangentSpace(texcoords);
   o.Normal = NormalizePerPixelNormal(o.Normal);
#endif
   o.Emission = Emission(texcoords.xy);
   half4 specGloss = SpecularGloss(texcoords.xy);
   o.Specular = specGloss.rgb; // _Specular;
   o.Smoothness = specGloss.a; // _Glossiness;
   o.Occlusion = Occlusion(texcoords.xy);
   o.Alpha = alpha;
}
 
void final (Input IN, SurfaceOutputStandard o, inout fixed4 color)
{
   color = OutputForward(color, color.a);
}

void finalSpecular (Input IN, SurfaceOutputStandardSpecular o, inout fixed4 color)
{
   color = OutputForward(color, color.a);
}

#endif