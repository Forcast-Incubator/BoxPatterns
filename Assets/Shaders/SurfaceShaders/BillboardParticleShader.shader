﻿Shader "Custom/BillboardParticleShader" {

	Properties
	{
		_Sprite("Sprite", 2D) = "white" {}
		_Tint("Tint", Color) = (1,1,1,1)
		_Size("Size", Vector) = (1,1,0,0)
	}

	SubShader
	{
		Tags { "Queue" = "Overlay+100" "RenderType" = "Transparent" }

		LOD 100
		Blend SrcAlpha One
		Cull off
		ZWrite off

		Pass
		{
		
		CGPROGRAM
		#pragma target 5.0
		#pragma vertex vert
		#pragma geometry geom
		#pragma	fragment frag
		#pragma multi_compile_fog
		#include "UnityCG.cginc"

		sampler2D _Sprite;
		float4 _Tint = float4(1.0f, 1.0f, 1.0f, 1.0f);
		float2 _Size = float2(1.0f, 1.0f);
		float3 _worldPos;

		int _StaticCylinderSpherical = 0; // 0 = static, 1 = cylinder, 2 = spherical

		struct particle
		{
			float3 pos;
			float3 col;
		};

		// buffer containing array of points we want to draw
		StructuredBuffer<particle> points;

		struct input 
		{
			float4 pos : SV_POSITION;
			float4 col : COLOR;
			float2 uv : TEXCOORD0;
			UNITY_FOG_COORDS(1)
		};

		input vert(uint id : SV_VertexID)
		{
			input o;
			o.pos = float4(points[id].pos + _worldPos, 1.0f);
			o.col = float4(points[id].col.r, points[id].col.g, points[id].col.b, 1.0);
			return o;
		}

		float4 RotPoint(float4 p, float3 offset, float3 sideVector, float3 upVector) {
			float3 finalPos = p.xyz;

			finalPos += offset.x * sideVector;
			finalPos += offset.y * upVector;

			return float4(finalPos, 1);
		}

		[maxvertexcount(4)]
		void geom(point input p[1], inout TriangleStream<input> triStream)
		{
			float2 halfS = _Size;

			float4 v[4];

			if (_StaticCylinderSpherical == 0)
			{
				// static facing
				v[0] = p[0].pos.xyzw + float4(-halfS.x, -halfS.y, 0, 0);
				v[1] = p[0].pos.xyzw + float4(-halfS.x, halfS.y, 0, 0);
				v[2] = p[0].pos.xyzw + float4(halfS.x, -halfS.y, 0, 0);
				v[3] = p[0].pos.xyzw + float4(halfS.x, halfS.y, 0, 0);
			}
			else {
				float3 up = normalize(float3(0, 1, 0));
				float3 look = _WorldSpaceCameraPos - p[0].pos.xyz;

				// cylinder facing (otherwise spherical facing)
				if (_StaticCylinderSpherical == 1)
					look.y = 0;
				
				
				look = normalize(look);
				float3 right = normalize(cross(look, up));
				up = normalize(cross(right, look));

				v[0] = RotPoint(p[0].pos, float3(-halfS.x, -halfS.y, 0), right, up);
				v[1] = RotPoint(p[0].pos, float3(-halfS.x, halfS.y, 0), right, up);
				v[2] = RotPoint(p[0].pos, float3(halfS.x, -halfS.y, 0), right, up);
				v[3] = RotPoint(p[0].pos, float3(halfS.x, halfS.y, 0), right, up);
			}

			input pIn;
			
			pIn.col = p[0].col;

			pIn.pos = mul(UNITY_MATRIX_VP, v[0]);
			pIn.uv = float2(0.0f, 0.0f);
			UNITY_TRANSFER_FOG(pIn, pIn.pos);
			triStream.Append(pIn);

			pIn.pos = mul(UNITY_MATRIX_VP, v[1]);
			pIn.uv = float2(0.0f, 1.0f);
			UNITY_TRANSFER_FOG(pIn, pIn.pos);
			triStream.Append(pIn);

			pIn.pos = mul(UNITY_MATRIX_VP, v[2]);
			pIn.uv = float2(1.0f, 0.0f);
			UNITY_TRANSFER_FOG(pIn, pIn.pos);
			triStream.Append(pIn);

			pIn.pos = mul(UNITY_MATRIX_VP, v[3]);
			pIn.uv = float2(1.0f, 1.0f);
			UNITY_TRANSFER_FOG(pIn, pIn.pos);
			triStream.Append(pIn);

		}

		float4 frag(input i) : COLOR
		{
			fixed4 col = tex2D(_Sprite, i.uv) * (_Tint * i.col); //* ((_Tint + i.col)*0.5);
			UNITY_APPLY_FOG(i.fogCoord, col);

			return col;
		}

		ENDCG
		}
	}
	Fallback Off
}
