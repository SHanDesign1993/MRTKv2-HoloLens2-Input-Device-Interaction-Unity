/*
	This is part of the Sinuous Sci-Fi Signs v1.6 package
	Copyright (c) 2014-2018 Thomas Rasor
	E-mail : thomas.ir.rasor@gmail.com

	NOTE : 
	This is the control script for the SSFS PostProcessing Effect.
*/
using UnityEngine;

namespace SSFS
{
	//[ImageEffectAllowedInSceneView]
	[ExecuteInEditMode]
	[RequireComponent( typeof( Camera ) )]
	[AddComponentMenu( "Image Effects/SSFS Post" )]
	public class SSFS_PostEffect : MonoBehaviour
	{
		public Camera transitionCamera = null;
		public bool forceUpdate = false;

		Material _m = null;
		Material m
		{
			get
			{
				if ( _m == null )
				{
					_m = SSFSCore.newMaterial;
					_m.name = "SSFS_POST_TEMP_MATERIAL";
					_m.SetInt( "_ZWrite" , 0 );
					_m.SetInt( "_ZTest" , 8 );
					_m.SetInt( "_Cull" , 0 );
					_m.SetInt( "_BlendSrc" , 1 );
					_m.SetInt( "_BlendDst" , 0 );
					_m.EnableKeyword( "POST" );
					_m.renderQueue = -1;
				}
				return _m;
			}
		}

		[System.Serializable]
		public class SSFSShellVars
		{
			public Color mainColor, effectColor;
			public Vector2 mainTextureOffset, mainTextureScale, mainTexture2Offset, mainTexture2Scale;
			public Texture mainTexture, mainTexture2, noiseTexture;
			public Vector2 tileCount, scaling, scalingCenter, RotationRadial;
			public float phase, sharpness, overbright, aberration, effectAberration, flash, flicker, idleAmount, idleSpeed, idleRand;
			public float scattering, scanlineIntensity, scanlineScale, scanlineShift, scanlineSpeed, scaleAroundTile, backfaceVisibility;
			public bool complex, squareTiles, invertPhase, invertIdle, clipTiles, roundClipping, twoSided;

			public SSFSShellVars()
			{
				mainColor = Color.white;
				effectColor = Color.white;
				mainTextureOffset = Vector2.zero;
				mainTextureScale = Vector2.one;
				mainTexture2Offset = Vector2.zero;
				mainTexture2Scale = Vector2.one;
				mainTexture = null;
				mainTexture2 = null;
				noiseTexture = null;

				tileCount = new Vector2( 42f , 42f );
				scaling = Vector2.zero;
				scalingCenter = Vector2.one * 0.5f;
				RotationRadial = new Vector2( 0.5f , 0f );
				scattering = 0.25f;
				scanlineIntensity = 0.2f;
				scanlineScale = 1f;
				scanlineShift = 0f;
				scaleAroundTile = 0f;
				backfaceVisibility = 0.5f;

				phase = 1f;
				sharpness = 0.6f;
				overbright = 0f;
				aberration = 0.2f;
				effectAberration = 0.5f;
				flash = 0.1f;
				flicker = 0.1f;
				idleAmount = 0.2f;
				idleSpeed = 0.2f;
				idleRand = 0f;
				complex = true;
				squareTiles = false;
				invertPhase = false;
				invertIdle = false;
				clipTiles = false;
				roundClipping = false;
				twoSided = true;
			}

			public void Apply( ref Material m )
			{
				if ( m == null ) return;

				m.SyncKeyword( "COMPLEX" , complex );

				m.SetFloat( "_Phase" , phase );
				m.SetFloat( "_PhaseSharpness" , sharpness );
				m.SetFloat( "_Overbright" , overbright );

				m.SyncKeyword( "IDLE" , idleAmount > 0f );

				m.SetVector( "_IdleData" , new Vector4( idleAmount , idleSpeed , idleRand , invertIdle ? 1f : 0f ) );

				m.SetFloat( "_Idle" , idleAmount );
				m.SetFloat( "_IdleSpeed" , idleSpeed );
				m.SetFloat( "_IdleRand" , idleRand );

				m.SetColor( "_Color" , mainColor );
				m.SetColor( "_Color2" , effectColor );

				m.SyncKeyword( "TEXTURE_SWAP" , mainTexture2 != null );
				m.SetTexture( "_MainTex2" , mainTexture2 );

				m.SetTexture( "_Noise" , noiseTexture );

				m.SetVector( "_TileCount" , tileCount );
				m.SetVector( "_Scaling" , scaling.Append( scalingCenter ) );

				m.SetFloat( "_Scattering" , scattering );
				m.SetFloat( "_BackfaceVisibility" , backfaceVisibility );
				m.SetFloat( "_FlashAmount" , flash );
				m.SetFloat( "_Flicker" , flicker );

				m.SyncKeyword( "RADIAL" , RotationRadial.y > 0f );
				m.SetVector( "_PhaseDirection" , RotationRadial );

				m.SyncKeyword( "SCALE_AROUND_TILE" , scaleAroundTile > 0f );
				m.SetFloat( "_ScaleAroundTile" , scaleAroundTile );

				m.SetFloat( "_SquareTiles" , squareTiles ? 1f : 0f );
				m.SetFloat( "_InvertPhase" , invertPhase ? 1f : 0f );
				m.SetFloat( "_InvertIdle" , invertIdle ? 1f : 0f );

				m.SyncKeyword( "CLIPPING" , clipTiles );
				m.SetFloat( "_ClippedTiles" , clipTiles ? 1f : 0f );
				m.SetFloat( "_RoundClipping" , roundClipping ? 1f : 0f );

				m.SyncKeyword( "ABERRATION" , aberration > 0f || effectAberration > 0f );
				m.SetFloat( "_Aberration" , aberration );
				m.SetFloat( "_EffectAberration" , effectAberration );

				m.SyncKeyword( "SCAN_LINES" , scanlineIntensity > 0f || scanlineShift > 0f );
				m.SetVector( "_ScanlineData" , new Vector4( scanlineIntensity , scanlineScale , scanlineShift , scanlineSpeed ) );
			}
		}
		public SSFSShellVars vars = new SSFSShellVars();

		private void OnEnable()
		{
			UpdateMaterialVars();
		}

		private void OnDisable()
		{
			if ( _m != null )
#if UNITY_EDITOR
				DestroyImmediate( _m );
#else
				Destroy( _m );
#endif
		}

		public void UpdateMaterialVars()
		{
			if ( transitionCamera != null )
			{
				if ( transitionCamera.targetTexture == null )
					transitionCamera.targetTexture = new RenderTexture( Screen.width , Screen.height , 16 );
				vars.mainTexture2 = transitionCamera.targetTexture;
			}
			else
				vars.mainTexture2 = null;

			vars.Apply( ref _m );
		}

		private void OnRenderImage( RenderTexture src , RenderTexture dst )
		{
			if ( m == null )
			{
				Graphics.Blit( src , dst );
				return;
			}

			if ( forceUpdate )
				UpdateMaterialVars();
			else
				m.SetFloat( "_Phase" , vars.phase );

			Graphics.Blit( src , dst , m );
		}
	}
}