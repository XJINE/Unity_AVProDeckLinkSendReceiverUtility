// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

Shader "AVProDeckLink/CompositeV210"
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
#pragma target 4.0
#pragma only_renderers d3d11 
#pragma multi_compile __ APPLY_LINEAR
//#pragma exclude_renderers flash
//#pragma fragmentoption ARB_precision_hint_fastest 
//#pragma multi_compile SWAP_RED_BLUE_ON SWAP_RED_BLUE_OFF
#include "UnityCG.cginc"
#include "AVProDeckLink_Shared.cginc"

uniform sampler2D _MainTex;
float _TextureWidth;
float4 _MainTex_TexelSize;

#if UNITY_VERSION >= 530
uniform float4 _MainTex_ST2;
#else
uniform float4 _MainTex_ST;
#endif

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
  
  o.uv.z = v.vertex.x;
  

  return o;
}

float3 repack(uint4 src)
{
 uint3 res = uint3(0,0,0);
 src.xyzw = src.wzyx;

// Bitwise operions only supported by DX11 in Unity
//#if SHADER_API_D3D11
 //res.x = ((src.x&255) * 16) + ((src.y&255) / 16)&1023;
 //res.y = ((src.y&15) * 64) + ((src.z&255) / 4)&1023;
 //res.z = ((src.z&3) * 256) + (src.w&255)&1023;
 res.x = (src.x << 4) | (src.y >> 4);
 res.y = (((src.y & 0x0f) << 6) | (src.z >> 2));
 res.z = (((src.z & 0x3) << 8) | src.w);
 //res.x = 0;
 //res.y = 0;
 //res.z = 0;
//#endif
 
 //return float3(res.x / 1024.0, res.y / 1024.0, res.z / 1024.0);
 return res.xyz / 1024.0;
}


float4 frag (v2f i) : COLOR
{
	float4 oCol = 0;
	
	float4 uv = i.uv;
	
	//
	/*
	// 4 yuv10bit DWORDS after unpacking have 6 pixels
	
	uint4 c2 = (tex2D(_MainTex, uv.xy + float2(_MainTex_TexelSize.x, 0)) * 255);
	uint4 c3 = (tex2D(_MainTex, uv.xy + float2(_MainTex_TexelSize.x * 2, 0)) * 255);
	uint4 c4 = (tex2D(_MainTex, uv.xy + float2(_MainTex_TexelSize.x * 3, 0)) * 255);
	

	float3 r3 = repack(c3); // Cb2-Y3-Cr1
	float3 r4 = repack(c4); // Y5-Cr2-Y4
	*/
	
	uint x = (uv.z * _TextureWidth);
	x = x%6;
	
	
	
	if (x == 0)
	{	
		uint4 c1 = (tex2D(_MainTex, uv.xy) * 255);
		float3 r1 = repack(c1); // Cr0-Y0-Cb0
		oCol.rgb = convertYUV_HD(r1.g, r1.b, r1.r).rgb; // y0-cb0-cr0
	}
	
	if (x == 1)
	{
		uint4 c1 = (tex2D(_MainTex, uv.xy) * 255);
		uint4 c2 = (tex2D(_MainTex, uv.xy + float2(_MainTex_TexelSize.x, 0)) * 255);
		float3 r1 = repack(c1); // Cr0-Y0-Cb0
		float3 r2 = repack(c2); // Y2-Cb1-Y1
		oCol.rgb = convertYUV_HD(r2.b, r1.b, r1.r).rgb; // y1-cb0-cr0
	}
	
	if (x == 2)
	{
		//uv.x -= _MainTex_TexelSize.x;
		uint4 c1 = (tex2D(_MainTex, uv.xy) * 255);
		uint4 c2 = (tex2D(_MainTex, uv.xy + float2(_MainTex_TexelSize.x, 0)) * 255);
		float3 r1 = repack(c1); // Cr0-Y0-Cb0
		float3 r2 = repack(c2); // Y2-Cb1-Y1
		oCol.rgb = convertYUV_HD(r1.r, r1.g, r2.b).rgb; // y2-cb1-cr1
	}
	
	if (x == 3)
	{
		uv.x -= _MainTex_TexelSize.x*1;
		uint4 c1 = (tex2D(_MainTex, uv.xy) * 255);
		uint4 c2 = (tex2D(_MainTex, uv.xy + float2(_MainTex_TexelSize.x, 0)) * 255);
		float3 r1 = repack(c1); // Cr0-Y0-Cb0
		float3 r2 = repack(c2); // Y2-Cb1-Y1
		oCol.rgb = convertYUV_HD(r2.g, r1.g, r2.b).rgb; // y3-cb1-cr1
	}
	
	if (x == 4)
	{
		//uv.x -= _MainTex_TexelSize.x * 1;
		uint4 c1 = (tex2D(_MainTex, uv.xy) * 255);
		uint4 c2 = (tex2D(_MainTex, uv.xy + float2(_MainTex_TexelSize.x, 0)) * 255);
		float3 r1 = repack(c1); // Cr0-Y0-Cb0
		float3 r2 = repack(c2); // Y2-Cb1-Y1
		oCol.rgb = convertYUV_HD(r2.b, r1.r, r2.g).rgb; // y4-cb2-cr2
	}
	
	if (x == 5)
	{
		uv.x -= _MainTex_TexelSize.x * 1;
		uint4 c1 = (tex2D(_MainTex, uv.xy) * 255);
		uint4 c2 = (tex2D(_MainTex, uv.xy + float2(_MainTex_TexelSize.x, 0)) * 255);
		float3 r1 = repack(c1); // Cr0-Y0-Cb0
		float3 r2 = repack(c2); // Y2-Cb1-Y1
		oCol.rgb = convertYUV_HD(r2.r, r1.r, r2.g).rgb; // y5-cb2-cr2
	}
	  
	/*if (x == 0)
	oCol.rgb = convertYUV_HD(r1.g, r1.b, r1.r).rgb; // y0-cb0-cr0
	if (x == 1)
	oCol.rgb = convertYUV_HD(r2.b, r1.b, r1.r).rgb; // y1-cb0-cr0
	if (x == 2)
	oCol.rgb = convertYUV_HD(r2.r, r2.g, r3.b).rgb; // y2-cb1-cr1
	
	if (x == 3)
	oCol.rgb = convertYUV_HD(r3.g, r2.g, r3.b).rgb; // y3-cb1-cr1
	if (x == 4)
	oCol.rgb = convertYUV_HD(r4.b, r3.r, r4.b).rgb; // y4-cb2-cr2
	if (x == 5)
	oCol.rgb = convertYUV_HD(r4.r, r3.r, r4.b).rgb; // y5-cb2-cr2*/
    
  //oCol.rgb = r2;
  
  //oCol = x / 5.0;
  
  //oCol.rgb = repack(uint4(255, 0, 0, 255));

  /*int x = uv.x * _TextureWidth;
  x = x%6;
  
  float4 ca1 = tex2D(_MainTex, uv.xy);
  float bb = (  ((asuint(ca1.y) & 15) << 4) | ((asuint(ca1.z) >> 2) & 255)  );
  oCol.rgb = bb / 4024.0;*/
  //oCol.rgb = x / 6.0;
  
  //if (x != 0)
  	//oCol.rgb = 0;
  
  //oCol.rgb = tex2D(_MainTex, uv.xy + _MainTex_TexelSize.x * 3).rgb;
  
  
  // Swap red and blue
  oCol.rgb = oCol.bgr;  

  oCol.a = 1;

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