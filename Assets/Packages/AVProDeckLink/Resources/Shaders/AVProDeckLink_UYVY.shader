// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

Shader "AVProDeckLink/CompositeUYVY"
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_TextureWidth ("Texure Width", Float) = 256.0
	}
	SubShader 
	{
		Pass
		{ 
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
		
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma exclude_renderers flash xbox360 ps3 gles
//#pragma fragmentoption ARB_precision_hint_fastest
#pragma fragmentoption ARB_precision_hint_nicest
//#pragma only_renderers d3d11 
//#pragma fragmentoption ARB_precision_hint_fastest 
#pragma multi_compile SWAP_RED_BLUE_ON SWAP_RED_BLUE_OFF
#pragma multi_compile CHROMA_NOLERP CHROMA_LERP CHROMA_SMARTLERP
#pragma multi_compile __ APPLY_LINEAR
#include "UnityCG.cginc"
#include "AVProDeckLink_Shared.cginc"

uniform sampler2D _MainTex;
float _TextureWidth;

#if UNITY_VERSION >= 530
uniform float4 _MainTex_ST2;
#else
uniform float4 _MainTex_ST;
#endif

float4 _MainTex_TexelSize;

struct v2f {
	float4 pos : POSITION;
	float4 uv : TEXCOORD0;
};

v2f vert( appdata_img v )
{
	v2f o;
	o.pos = UnityObjectToClipPos (v.vertex);
	o.uv = float4(0, 0, 0, 0);
	
#if UNITY_VERSION >= 530
	o.uv.xy = (v.texcoord.xy * _MainTex_ST2.xy + _MainTex_ST2.zw);
#else
	o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
#endif
	
	// On D3D when AA is used, the main texture & scene depth texture
	// will come out in different vertical orientations.
	// So flip sampling of the texture when that is the case (main texture
	// texel size will have negative Y).
	#if SHADER_API_D3D9
	if (_MainTex_TexelSize.y < 0)
	{
		o.uv.y = 1-o.uv.y;
	}
	#endif
	
	o.uv.z = v.vertex.x * _TextureWidth * 0.5;

	return o;
}

float4 frag (v2f i) : COLOR
{
	float4 uv = i.uv;
	
	float4 col = tex2D(_MainTex, uv.xy);

#if defined(SWAP_RED_BLUE_ON)
	col = col.bgra;
#endif

	//uyvy
	float y = col.y;
	float u = col.x;
	float v = col.z;
	
	if (frac(uv.z) > 0.5 )
	{
#if defined(CHROMA_NOLERP)
		// ODD PIXELS
		y = col.w;
#endif
	
#if defined(CHROMA_LERP)
		// ODD PIXELS
		y = col.w;
		
		// Interpolate chroma
		float4 col2 = tex2D(_MainTex, uv.xy + float2(_MainTex_TexelSize.x, 0.0));
#if defined(SWAP_RED_BLUE_ON)
		col2 = col2.bgra;
#endif
		u = (col.x + col2.x) * 0.5;
		v = (col.z + col2.z) * 0.5;
#endif
#if defined(CHROMA_SMARTLERP)
		// Left Side
		float l1 = y;
		float u1 = u;
		float v1 = v;
		
		// Right Side
		float4 col2 = tex2D(_MainTex, uv.xy + float2(_MainTex_TexelSize.x, 0.0));
#if defined(SWAP_RED_BLUE_ON)
		col2 = col2.bgra;
#endif		
		float l2 = col2.y;
		float u2 = col2.x;
		float v2 = col2.z;
		
		// Current Pixel
		float l0 = col.w;
		float u0 = 0;
		float v0 = 0;
		
		// Interpolation Factors
		float lrange = abs(l2 - l0) + abs(l1 - l0);
		if (lrange != 0)
		{
			float k1 = abs(l2 - l0) / lrange;
			float k2 = abs(l1 - l0) / lrange;
			
			// Interpolate
			u0 = (k1 * u1) + (k2 * u2);
			v0 = (k1 * v1) + (k2 * v2);	
			
			// Assign
			y = l0;
			u = u0;
			v = v0;
			//u = (u / 2) + 0.5;
			//v = (v / 2) + 0.5;
		}
		else
		{
			y = l0;
			u = (u1 + u2) * 0.5;
			v = (v1 + v2) * 0.5;
		}
#endif		
	}

    float4 oCol = convertYUV_HD(y, u, v);

#if APPLY_LINEAR
	oCol = gammaToLinear(oCol);
#endif

	return oCol;	 
} 
ENDCG
		}
	}
	
	FallBack Off
}