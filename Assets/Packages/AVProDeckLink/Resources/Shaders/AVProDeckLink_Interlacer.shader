// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

Shader "AVProDeckLink/Interlacer"
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_TextureHeight ("Texure Height", Float) = 256.0
	}
	SubShader 
	{
		Pass
		{ 
			ZTest Always Cull Off ZWrite On
			Fog { Mode off }
		
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma exclude_renderers flash xbox360 ps3 gles
//#pragma fragmentoption ARB_precision_hint_fastest
#pragma fragmentoption ARB_precision_hint_nicest
//#pragma only_renderers d3d11 
//#pragma fragmentoption ARB_precision_hint_fastest 
#include "UnityCG.cginc"
#include "AVProDeckLink_Shared.cginc"

uniform sampler2D _MainTex;
float _TextureHeight;
float4 _MainTex_ST;
float4 _MainTex_TexelSize;

struct v2f {
	float4 pos : POSITION;
	float4 uv : TEXCOORD0;
};

v2f vert( appdata_img v )
{
	v2f o;
	o.pos = UnityObjectToClipPos (v.vertex);
	o.uv = float4(TRANSFORM_TEX(v.texcoord, _MainTex), 0, 0);
	
	//o.uv.y = 1-o.uv.y;
	o.uv.z = v.vertex.y * _TextureHeight * 0.5;	

	// On D3D when AA is used, the main texture & scene depth texture
	// will come out in different vertical orientations.
	// So flip sampling of the texture when that is the case (main texture
	// texel size will have negative Y).
	/*#if UNITY_UV_STARTS_AT_TOP
	if (_MainTex_TexelSize.y < 0)
	{
		o.uv.y = 1-o.uv.y;
		o.uv.z = (1-v.vertex.y) * _TextureHeight * 0.5;
	}
	#endif*/
	

	return o;
}

float4 frag (v2f i) : COLOR
{
	float4 uv = i.uv;	
	
	float4 col1 = tex2D(_MainTex, uv.xy);
	
	float4 oCol = col1;
	if (frac(uv.z) < 0.5)
	{
		clip(-1);
	}
	
	return oCol;	
} 
ENDCG
		}
		
		Pass
		{ 
			ZTest Always Cull Off ZWrite On
			Fog { Mode off }
		
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma exclude_renderers flash xbox360 ps3 gles
//#pragma fragmentoption ARB_precision_hint_fastest
#pragma fragmentoption ARB_precision_hint_nicest
//#pragma only_renderers d3d11 
//#pragma fragmentoption ARB_precision_hint_fastest 
#include "UnityCG.cginc"
#include "AVProDeckLink_Shared.cginc"

uniform sampler2D _MainTex;
float _TextureHeight;
float4 _MainTex_ST;
float4 _MainTex_TexelSize;

struct v2f {
	float4 pos : POSITION;
	float4 uv : TEXCOORD0;
};

v2f vert( appdata_img v )
{
	v2f o;
	o.pos = UnityObjectToClipPos (v.vertex);
	o.uv = float4(TRANSFORM_TEX(v.texcoord, _MainTex), 1, 1);
	
	//o.uv.y = 1-o.uv.y;
	o.uv.z = v.vertex.y * _TextureHeight * 0.5;
	// On D3D when AA is used, the main texture & scene depth texture
	// will come out in different vertical orientations.
	// So flip sampling of the texture when that is the case (main texture
	// texel size will have negative Y).
	/*#if UNITY_UV_STARTS_AT_TOP
	if (_MainTex_TexelSize.y < 0)
	{
		o.uv.y = 1-o.uv.y;
		o.uv.z = (1-v.vertex.y) * _TextureHeight * 0.5;
	}
	#endif*/
	
	

	return o;
}

float4 frag (v2f i) : COLOR
{
	float4 uv = i.uv;	
	
	float4 col1 = tex2D(_MainTex, uv.xy);
	
	float4 oCol = col1;
	if (frac(uv.z) >= 0.5)
	{
		discard;
	}
	
	return oCol;	
} 
ENDCG
		}		
	}
	
	FallBack Off
}