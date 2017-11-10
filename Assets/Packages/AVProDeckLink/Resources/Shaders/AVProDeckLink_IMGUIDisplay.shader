// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

Shader "AVProDeckLink/IMGUIDisplay"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag 
			#pragma multi_compile __ APPLY_GAMMA
			#pragma multi_compile SCALE_TO_FIT SCALE_AND_CROP STRETCH_TO_FILL

			#include "UnityCG.cginc"
			#include "AVProDeckLink_Shared.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float _width;
			float _height;
			float _rectWidth;
			float _rectHeight;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				float rectAspect = _rectWidth / _rectHeight;
				float texAspect = _width / _height;
#if SCALE_TO_FIT
				float2 multiplier = rectAspect <= texAspect ? float2(1, texAspect / rectAspect) : float2(rectAspect / texAspect, 1);
				float2 newuv = o.uv * multiplier;
				float2 dif = float2(1, 1) - multiplier;
				o.uv = newuv + dif / 2;
#elif SCALE_AND_CROP
				float2 multiplier = rectAspect <= texAspect ? float2(rectAspect / texAspect, 1) : float2(1, texAspect / rectAspect);
				float2 newuv = o.uv * multiplier;
				float2 dif = multiplier - float2(1, 1);
				o.uv = newuv - dif / 2;
#elif STRETCH_TO_FILL
				//nothing needs to be done for stretch
#endif

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				bool inBounds = i.uv.x >= 0 && i.uv.y >= 0 && i.uv.x <= 1 && i.uv.y <= 1;

				fixed4 col = inBounds ? tex2D(_MainTex, i.uv) : fixed4(0, 0, 0, 0);
#if APPLY_GAMMA
				col = linearToGamma(col);
#endif
				//return fixed4(i.uv, 0, 1);
				return col;
			}
			ENDCG
		}
	}
}
