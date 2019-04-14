﻿Shader "IATK/PCPShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Size ("Size", Range(0, 30)) = 0.5
		_MinSize("_MinSize",Float) = 0
		_MaxSize("_MaxSize",Float) = 0
		_MinX("_MinX",Range(0, 1)) = 0
		_MaxX("_MaxX",Range(0, 1)) = 1.0
		_MinY("_MinY",Range(0, 1)) = 0
		_MaxY("_MaxY",Range(0, 1)) = 1.0
		_MinZ("_MinZ",Range(0, 1)) = 0
		_MaxZ("_MaxZ",Range(0, 1)) = 1.0		
		_MinNormX("_MinNormX",Range(0, 1)) = 0.0
		_MaxNormX("_MaxNormX",Range(0, 1)) = 1.0
		_MinNormY("_MinNormY",Range(0, 1)) = 0.0
		_MaxNormY("_MaxNormY",Range(0, 1)) = 1.0
		_MinNormZ("_MinNormZ",Range(0, 1)) = 0.0
		_MaxNormZ("_MaxNormZ",Range(0, 1)) = 1.0
		_SrcBlend ("__src", Float) = 1.0
		_DstBlend ("__dst", Float) = 0.0
		_MySrcMode("_SrcMode", Float) = 5
		_MyDstMode("_DstMode", Float) = 10
	}
	SubShader
	{
			Tags { "RenderType"="Transparent" }
			//Blend func : Blend Off : turns alpha blending off
			//#ifdef(VISUAL_ACCUMULATION)
			//Blend SrcAlpha One
			//#else
			//Blend [_SrcBlend] [_DstBlend]

			Blend[_MySrcMode][_MyDstMode]
			//#endif
			
			//AlphaTest Greater .01
			Cull Off
			ZWrite On
			//Lighting On
		//	Zwrite On
			LOD 200

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;
				float3 uv_MainTex : TEXCOORD0; // index, vertex size, filtered
			};

			struct v2g
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;
				float  isBrushed : FLOAT;
			};

			struct g2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float2 tex0	: TEXCOORD0;
				float  isBrushed : FLOAT;
			};

			float _Size;
			float _MinSize;
			float _MaxSize;
			
			sampler2D _BrushedTexture;

			float showBrush;
			float4 brushColor;

			//Sampler2D _MainTexSampler;

			//SamplerState sampler_MainTex;

			float _DataWidth;
			float _DataHeight;

			//*******************
			// RANGE FILTERING
			//*******************

			float _MinX;
			float _MaxX;
			float _MinY;
			float _MaxY;
			float _MinZ;
			float _MaxZ;

			// ********************
			// Normalisation ranges
			// ********************

			float _MinNormX;
			float _MaxNormX;
			float _MinNormY;
			float _MaxNormY;
			float _MinNormZ;
			float _MaxNormZ;

			//*********************************
			// helper functions
			//*********************************
			float normaliseValue(float value, float i0, float i1, float j0, float j1)
			{
				float L = (j0 - j1) / (i0 - i1);
				return (j0 - (L * i0) + (L * value));
			}

			v2g vert (appdata v)
			{
				v2g o;
				float idx = v.uv_MainTex.x;
				float size = v.uv_MainTex.y;
				float isFiltered = v.uv_MainTex.z;

				//lookup the texture to see if the vertex is brushed...
				float2 indexUV = float2((v.normal.x % _DataWidth) / _DataWidth, ((v.normal.x / _DataWidth) / _DataHeight));
				float4 brushValue = tex2Dlod(_BrushedTexture, float4(indexUV, 0.0, 0.0));

				o.isBrushed = brushValue.r;// > 0.001;
				
				float4 normalisedPosition = float4(
					normaliseValue(v.vertex.x, _MinNormX, _MaxNormX, 0, 1),
					normaliseValue(v.vertex.y, _MinNormY, _MaxNormY, 0, 1),
					normaliseValue(v.vertex.z, _MinNormZ, _MaxNormZ, 0, 1), 1.0);

				float4 vert = UnityObjectToClipPos(normalisedPosition);

				//TODO: handle filtering of PCPs axes

				o.vertex = vert;
				//o.normal = v.normal;
				o.normal = float3(idx,size,isFiltered);

				o.color =  v.color;
				if(isFiltered) 
				{
					o.color.w = 0;
				}
				return o;
			}

			[maxvertexcount(6)]
			void geom(line v2g points[2], inout TriangleStream<g2f> triStream)
			{
				//handle brushing line topoolgy
				if (points[0].color.w == 0) points[1].color.w = 0;
				if (points[1].color.w == 0) points[0].color.w = 0;

				//line geometry
				float4 p0 = points[0].vertex;
				float4 p1 = points[1].vertex;

				float w0 = p0.w;
				float w1 = p1.w;

				p0.xyz /= p0.w;
				p1.xyz /= p1.w;

				float3 line01 = p1 - p0;
				float3 dir = normalize(line01);

				// scale to correct window aspect ratio
				float3 ratio = float3(1024, 768, 0);
				ratio = normalize(ratio);

				float3 unit_z = normalize(float3(0, 0, -1));

				float3 normal = normalize(cross(unit_z, dir) * ratio);

				float width = _Size * normaliseValue(points[0].normal.y, 0.0, 1.0, _MinSize, _MaxSize) * 0.025;

				g2f v[4];

				float3 dir_offset = dir * ratio * width;
				float3 normal_scaled = normal * ratio * width;

				float3 p0_ex = p0 - dir_offset;
				float3 p1_ex = p1 + dir_offset;

				v[0].vertex = float4(p0_ex - normal_scaled, 1) * w0;
				v[0].tex0 = float2(1,0);
				v[0].color = points[0].color;
				v[0].isBrushed = points[0].isBrushed;// || points[1].isBrushed;

				v[1].vertex = float4(p0_ex + normal_scaled, 1) * w0;
				v[1].tex0 = float2(0,0);
				v[1].color = points[0].color;
				v[1].isBrushed = points[0].isBrushed;// || points[1].isBrushed;

				v[2].vertex = float4(p1_ex + normal_scaled, 1) * w1;
				v[2].tex0 = float2(1,1);
				v[2].color = points[1].color;
				v[2].isBrushed = points[0].isBrushed;// || points[1].isBrushed;

				v[3].vertex = float4(p1_ex - normal_scaled, 1) * w1;
				v[3].tex0 = float2(0,1);
				v[3].color = points[1].color;
				v[3].isBrushed = points[0].isBrushed;// || points[1].isBrushed;

				triStream.Append(v[2]);
				triStream.Append(v[1]);
				triStream.Append(v[0]);

				triStream.RestartStrip();

				triStream.Append(v[3]);
				triStream.Append(v[2]);
				triStream.Append(v[0]);

				triStream.RestartStrip();

			} 
			
			fixed4 frag (g2f i) : SV_Target
			{
				if (i.isBrushed > 0.0 && showBrush > 0.0)
				return brushColor;
				else
				if(i.color.w>0)
				return i.color;	
				else {discard; return 	i.color;	
		}
			}
			ENDCG
		}
	}
}
