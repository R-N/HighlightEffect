Shader "Custom/Highlight" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Main Texture", 2D) = "black" {}
		_OccludeMap ("Occlusion Map", 2D) = "black" {}
		[Toggle(DEFFERED)] _DEFFERED ("Using Deffered Rendering Path", Float) = 0
	}
	
	SubShader {

		
		
		// OVERLAY GLOW
		
		Pass {
		ZTest Always Cull Off ZWrite Off Fog { Mode Off }
		Blend SrcAlpha One
			CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"
			
				sampler2D _MainTex;
				sampler2D _OccludeMap;
			
				fixed4 frag(v2f_img IN) : COLOR 
				{
				#if SHADER_API_D3D9 || SHADER_API_D3D11
				
					fixed3 overCol = tex2D(_OccludeMap, float2(IN.uv.x, 1 - IN.uv.y));
					#else
					fixed3 overCol = tex2D(_OccludeMap, IN.uv) ;
					#endif
					return tex2D (_MainTex, IN.uv) + fixed4(overCol, 1.0);
				}
			ENDCG
		}
		
		// OVERLAY SOLID
		
		Pass {
		ZTest Always Cull Off ZWrite Off Fog { Mode Off }
			CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"
			
				sampler2D _MainTex;
				sampler2D _OccludeMap;
			
				fixed4 frag(v2f_img IN) : COLOR 
				{
					fixed4 mCol = tex2D (_MainTex, IN.uv);
				#if SHADER_API_D3D9 || SHADER_API_D3D11
           			 fixed oCol = tex2D(_OccludeMap, float2(IN.uv.x, 1 - IN.uv.y));
           			 #else
					fixed oCol = tex2D (_OccludeMap, IN.uv).r;
					#endif
					return mCol;
				}
			ENDCG
		}
	
		
		// OCCLUSION
		
		Pass {
		ZTest Always Cull Off ZWrite Off Fog { Mode Off }
				CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"
			
				sampler2D _MainTex;
				sampler2D _OccludeMap;
			
				fixed4 frag(v2f_img IN) : COLOR 
				{
            
					return tex2D (_MainTex, IN.uv) - tex2D(_OccludeMap, IN.uv);
				}
			ENDCG
		}
		
		Pass {
        
		Cull Off
           	Tags {"RenderType"="Opaque"}
           	Blend SrcAlpha One
        	ZWrite On
        	ZTest LEqual
        	Fog { Mode Off }
        
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float4 vert(float4 v:POSITION) : POSITION {
                return mul (UNITY_MATRIX_MVP, v);
            }

				fixed4 _Color;
            fixed4 frag() : COLOR {
                return _Color;
            }

            ENDCG
        }	
    	
        Pass {        	
           	Tags {"Queue"="Transparent"}
		Fog { Mode Off }
		AlphaTest Greater 0.001
            Cull Back
            Lighting Off
            ZWrite On
            ZTest On
            ColorMask RGBA
            Blend SrcAlpha One

        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _CameraDepthTexture;
				fixed4 _Color;
            
            struct v2f {
                float4 vertex : POSITION;
                float4 projPos : TEXCOORD1;
            };
     
            v2f vert( float4 v : POSITION ) {        
                v2f o;
                o.vertex = mul( UNITY_MATRIX_MVP, v );
                o.projPos = ComputeScreenPos(o.vertex);             
                return o;
            }

            fixed4 frag( v2f i ) : COLOR {       
               
                float depthVal = LinearEyeDepth (tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)).r);
                float zPos = i.projPos.z;
                float occlude = step( zPos, depthVal );
                return fixed4(occlude * _Color.rgb * _Color.a, occlude);
            }
            ENDCG
        }
		Pass {
			CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"
			
				sampler2D _MainTex;
				sampler2D _OccludeMap;
			
				fixed4 frag(v2f_img IN) : COLOR 
				{
					fixed3 overCol = tex2D(_OccludeMap, IN.uv) ;
					return tex2D (_MainTex, IN.uv) + fixed4(overCol, 1.0);
				}
			ENDCG
		}
	} 
}
