﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "IATK/Quads" 
{
	Properties 
	{
		_MainTex("Base (RGB)", 2D) = "White" {}
		_Size ("Size", Range(0, 1)) = 0.5
		_MinSize ("Min Size", Range(0, 1)) = 0.01
		_MaxSize ("Max Size", Range(0, 1)) = 1.0
		_BrushSize("BrushSize",Float) = 0.05
		_MinX("_MinX",Range(0, 1)) = 0
		_MaxX("_MaxX",Range(0, 1)) = 1.0
		_MinY("_MinY",Range(0, 1)) = 0
		_MaxY("_MaxY",Range(0, 1)) = 1.0
		_MinZ("_MinZ",Range(0, 1)) = 0
		_MaxZ("_MaxZ",Range(0, 1)) = 1.0
		_data_size("data_size",Float) = 0
		_tl("Top Left", Vector) = (-1,1,0,0)
		_tr("Top Right", Vector) = (1,1,0,0)
		_bl("Bottom Left", Vector) = (-1,-1,0,0)
		_br("Bottom Right", Vector) = (1,-1,0,0)
		_MinNormX("_MinNormX",Range(0, 1)) = 0.0
		_MaxNormX("_MaxNormX",Range(0, 1)) = 1.0
		_MinNormY("_MinNormY",Range(0, 1)) = 0.0
		_MaxNormY("_MaxNormY",Range(0, 1)) = 1.0
		_MinNormZ("_MinNormZ",Range(0, 1)) = 0.0
		_MaxNormZ("_MaxNormZ",Range(0, 1)) = 1.0
		_MySrcMode("_SrcMode", Float) = 5
		_MyDstMode("_DstMode", Float) = 10
	}
    
	SubShader 
	{
		Pass
		{
			Name "Onscreen geometry"
			Tags { "RenderType"="Transparent" }
			//Blend func : Blend Off : turns alpha blending off
			//Blend One One
			//Lighting On
			//Cull Front
			
			// old pragma config

			//Blend SrcAlpha OneMinusSrcAlpha
			//Zwrite On
			//ZTest LEqual     
            
			// new transparent pragma config

			//Cull Off // draw front and back faces
			//ZWrite On // don't write to depth buffer 
            // in order not to occlude other objects
			//Blend SrcAlpha OneMinusSrcAlpha  // additive blending
			
			Blend[_MySrcMode][_MyDstMode]
			//Blend One One
			
			//#ifdef
			//Blend SrcAlpha One
			//#endif

			//AlphaTest Greater .01
			ColorMask RGB
			Cull Off 
			Lighting Off 
			ZWrite On
			Offset -1, -1 // This line is added to default Unlit/Transparent shader
			Fog { Color (0,0,0,0) }

			LOD 200

		
			CGPROGRAM
				//// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
				//#pragma exclude_renderers d3d11 gles
				#pragma target 5.0
				#pragma vertex VS_Main
				#pragma fragment FS_Main
				#pragma geometry GS_Main2
				#include "UnityCG.cginc" 
				#include "Distort.cginc"

				// **************************************************************
				// Data structures												*
				// **************************************************************
				
				//float brusedIndices[65000];

		        struct VS_INPUT {
          		    float4 position : POSITION;
            		float4 color: COLOR;
					float3 normal:	NORMAL;
        		};
				
				struct GS_INPUT
				{
					float4	pos		: POSITION;
					float3	normal	: NORMAL;
					float2  tex0	: TEXCOORD0;
					float4  color		: COLOR;
					float	isBrushed : FLOAT;
				};

				struct FS_INPUT
				{
					float4	pos		: POSITION;
					float2  tex0	: TEXCOORD0;
					//float2	tex1	: TEXCOORD1;
					float4  color		: COLOR;
					float	isBrushed : FLOAT;
					float3	normal	: NORMAL;
				};


				// **************************************************************
				// Vars															*
				// **************************************************************

				float _Size;
				float _MinSize;
				float _MaxSize;

				float _BrushSize;
				
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

				uniform float4x4 _VP;
				sampler2D _BrushedTexture;
				sampler2D _MainTex;

				float showBrush;
				float4 brushColor;

				//Sampler2D _MainTexSampler;

				//SamplerState sampler_MainTex;
				
				float _DataWidth;
				float _DataHeight;

				//float[] brushedIndexes;
				//*********************************
				// helper functions
				//*********************************

				float normaliseValue(float value, float i0, float i1, float j0, float j1)
				{
				float L = (j0 - j1) / (i0 - i1);
				return (j0 - (L * i0) + (L * value));
				}

				// **************************************************************
				// Shader Programs												*
				// **************************************************************

				// Vertex Shader ------------------------------------------------
				GS_INPUT VS_Main(VS_INPUT v)
				{
					GS_INPUT output = (GS_INPUT)0;
					
					//lookup the texture to see if the vertex is brushed...
					float2 indexUV = float2((v.normal.x % _DataWidth) / _DataWidth, ((v.normal.x / _DataWidth) / _DataHeight));
					float4 brushValue = tex2Dlod(_BrushedTexture, float4(indexUV, 0.0, 0.0));

					output.isBrushed = brushValue.r;

					float4 normalisedPosition = float4(
					normaliseValue(v.position.x,_MinNormX, _MaxNormX, 0,1),
					normaliseValue(v.position.y,_MinNormY, _MaxNormY, 0,1),
					normaliseValue(v.position.z,_MinNormZ, _MaxNormZ, 0,1), v.position.w);

					output.pos = normalisedPosition;

					//the normal buffer carries the index of each vertex
					output.tex0 = float2(0, 0);

					output.color = v.color;

				
					//filtering
					if(
					 normalisedPosition.x < _MinX ||
					 normalisedPosition.x > _MaxX || 
					 normalisedPosition.y < _MinY || 
					 normalisedPosition.y > _MaxY || 
					 normalisedPosition.z < _MinZ || 
					 normalisedPosition.z > _MaxZ 	
					 //||

					 //normalisedPosition.x < _MinNormX ||
					 //normalisedPosition.x > _MaxNormX || 
					 //normalisedPosition.y < _MinNormY || 
					 //normalisedPosition.y > _MaxNormY || 
					 //normalisedPosition.z < _MinNormZ || 
					 //normalisedPosition.z > _MaxNormZ			 
					 )
					{
						output.color.w = 0;
					}
					output.normal = v.normal;

					_VP = UNITY_MATRIX_MVP;

					return output;
				}



				// Geometry Shader -----------------------------------------------------
				[maxvertexcount(6)]
				void GS_Main2(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
				{
					float4x4 MV = UNITY_MATRIX_MV;
					float4x4 vp = UNITY_MATRIX_VP;

					float3 up = UNITY_MATRIX_IT_MV[1].xyz;
					float3 right =  -UNITY_MATRIX_IT_MV[0].xyz;

					float dist = length(ObjSpaceViewDir(p[0].pos));

					float sizeFactor = normaliseValue(p[0].normal.y, 0.0, 1.0, _MinSize, _MaxSize);
										
					float halfS = 0.025f * (_Size + (dist * sizeFactor));
							
					float4 v[4];				

					v[0] = float4(p[0].pos + halfS * right - halfS * up, 1.0f);
					v[1] = float4(p[0].pos + halfS * right + halfS * up, 1.0f);
					v[2] = float4(p[0].pos - halfS * right - halfS * up, 1.0f);
					v[3] = float4(p[0].pos - halfS * right + halfS * up, 1.0f);
		
					FS_INPUT pIn;
					
					pIn.isBrushed = p[0].isBrushed;
					pIn.color = p[0].color;
					pIn.normal = p[0].normal;

					//pIn.pos =  mul(vp, v[0]);
					pIn.pos = UnityObjectToClipPos(v[0]);					
					pIn.tex0 = float2(1.0f, 0.0f);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);

					//pIn.pos = mul(vp,v[1]);
					pIn.pos = UnityObjectToClipPos(v[1]);
					pIn.tex0 = float2(1.0f, 1.0f);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);

					//pIn.pos =  mul(vp, v[2]);
					pIn.pos = UnityObjectToClipPos(v[2]);
					pIn.tex0 = float2(0.0f, 0.0f);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);

					//pIn.pos =  mul(vp, v[3]);
					pIn.pos = UnityObjectToClipPos(v[3]);
					pIn.tex0 = float2(0.0f, 1.0f);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);
					
				}

				// Fragment Shader -----------------------------------------------
				float4 FS_Main(FS_INPUT input) : SV_Target0
				{
					//FragmentOutput fo = (FragmentOutput)0;

				float dx = input.tex0.x - 0.5f;
				float dy = input.tex0.y - 0.5f;

				float dt = dx * dx + dy * dy;

				if (input.color.w == 0)
				{			
					discard;
					return float4(0.0, 0.0, 0.0, 0.0);				
				}
				if (input.isBrushed && showBrush>0.0) return brushColor;
				else return float4(input.color.x - dt*0.15,input.color.y - dt*0.15,input.color.z - dt*0.15,input.color.w);

				}

				


			ENDCG
		


		}
	} 
}