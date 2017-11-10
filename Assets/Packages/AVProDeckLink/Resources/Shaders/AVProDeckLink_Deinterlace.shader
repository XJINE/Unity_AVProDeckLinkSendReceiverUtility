// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

Shader "AVProDeckLink/Deinterlace" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_TextureHeight ("Texure Height", Float) = 256.0
	}

	SubShader 
	{
		// 0 None
		Pass 
		{
			Name "None"
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
			
					
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma exclude_renderers flash xbox360 ps3 gles
			#pragma fragmentoption ARB_precision_hint_nicest
			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform half4 _MainTex_TexelSize;

			fixed4 frag (v2f_img i) : COLOR
			{
				return tex2D(_MainTex, i.uv);
			}
			ENDCG
		}

		// 1 Blend
		Pass 
		{
			Name "Blend"
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
			
					
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma exclude_renderers flash xbox360 ps3 gles
			#pragma fragmentoption ARB_precision_hint_nicest
			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform half4 _MainTex_TexelSize;

			fixed4 frag (v2f_img i) : COLOR
			{
				float4 c0 = tex2D(_MainTex, i.uv);

				float2 h = float2(0 , _MainTex_TexelSize.y);
				
				float4 c1 = tex2D(_MainTex, i.uv - h);
				float4 c2 = tex2D(_MainTex, i.uv + h);

				return (c0*2+c1+c2)/4;
			}
			ENDCG
		}
		
		// 2 Discard
		Pass 
		{
			Name "Discard"
			ZTest Less Cull Off ZWrite On
			Fog { Mode off }
			
					
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma exclude_renderers flash xbox360 ps3 gles
			#pragma fragmentoption ARB_precision_hint_nicest
			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform float4 _MainTex_TexelSize;	
			uniform float _TextureHeight;

			struct v2f 
			{
				float4 pos : POSITION;
				float4 uv : TEXCOORD0;
			};

			v2f vert( appdata_img v )
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = float4(TRANSFORM_TEX(v.texcoord, _MainTex), 0, 0);
				
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
				
				o.uv.z = v.vertex.y * _TextureHeight * 0.5;

				return o;
			}


			fixed4 frag (v2f i) : COLOR
			{
				float2 h = float2(0 , _MainTex_TexelSize.y);
				float4 oCol = tex2D(_MainTex, i.uv.xy);
				float4 prevLine = tex2D(_MainTex, i.uv.xy-h);
				if (frac(i.uv.z) > 0.5)
				{
					oCol = prevLine;
				}

				return oCol;
			}
			ENDCG
		}
	
		// 3 Discard-Smooth
		Pass 
		{
			Name "Discard-Smooth"
			ZTest Less Cull Off ZWrite On
			Fog { Mode off }
			
					
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma exclude_renderers flash xbox360 ps3 gles
			#pragma fragmentoption ARB_precision_hint_nicest
			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform float4 _MainTex_TexelSize;	
			uniform float _TextureHeight;

			struct v2f 
			{
				float4 pos : POSITION;
				float4 uv : TEXCOORD0;
			};

			v2f vert( appdata_img v )
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = float4(TRANSFORM_TEX(v.texcoord, _MainTex), 0, 0);
				
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
				
				o.uv.z = v.vertex.y * _TextureHeight * 0.5;

				return o;
			}


			fixed4 frag (v2f i) : COLOR
			{
				float2 h = float2(0 , _MainTex_TexelSize.y);
				float4 oCol = tex2D(_MainTex, i.uv.xy);
				float4 prevLine = tex2D(_MainTex, i.uv.xy-h);
				float4 nextLine = tex2D(_MainTex, i.uv.xy+h);
				if (frac(i.uv.z) > 0.5)
				{
					oCol = (prevLine+nextLine)/2;
				}

				return oCol;
			}
			ENDCG
		}			
	}

	Fallback off

}