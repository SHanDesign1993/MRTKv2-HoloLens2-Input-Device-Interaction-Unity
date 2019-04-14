/*
	This is part of the Sinuous Sci-Fi Signs v1.5 package
	Copyright (c) 2014-2018 Thomas Rasor
	E-mail : thomas.ir.rasor@gmail.com
*/

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using gui = UnityEngine.GUILayout;
using egui = UnityEditor.EditorGUILayout;
using sed = SSFS.SSFS_Editor_Drawers;
using style = SSFS.SSFS_Styles;

namespace SSFS
{
	[CustomEditor( typeof( SSFS_PostEffect ) )]
	public class SSFS_Post_Editor : Editor
	{
		SSFS_PostEffect _e;
		public SSFS_PostEffect e
		{
			get
			{
				if ( _e == null )
					_e = ( SSFS_PostEffect )target;
				return _e;
			}
		}

		public bool isChangingScalingCenter = false;
		public bool isChangingPhaseRotation = false;
		public bool showImages = false;
		public static int currentTab = 0;

		public override void OnInspectorGUI()
		{
			Undo.RecordObject( target , "Edit SSFS Post Effect" );
			EditorGUI.BeginChangeCheck();

			gui.BeginHorizontal();
			e.forceUpdate = egui.Toggle( "Constant Update" , e.forceUpdate );
			if ( !e.forceUpdate )
			{
				if ( gui.Button( "Update" ) ) e.UpdateMaterialVars();
			}
			else
				gui.FlexibleSpace();
			gui.EndHorizontal();

			e.vars.phase = egui.Slider( "Phase" , e.vars.phase , 0f , 1f );
			egui.Space();
			egui.BeginHorizontal();
			sed.Tab( "General" , 0 , ref currentTab );
			sed.Tab( "Transition" , 1 , ref currentTab );
			sed.Tab( "Idle" , 2 , ref currentTab );
			sed.Tab( "Effects" , 3 , ref currentTab );
			sed.Tab( "Scanlines" , 4 , ref currentTab );
			egui.EndHorizontal();
			egui.Space();

			egui.BeginVertical( style.params_box );
			switch ( currentTab )
			{
				case 0: DrawGeneralTab(); break;
				case 1: DrawTransitionTab(); break;
				case 2: DrawIdleTab(); break;
				case 3: DrawEffectsTab(); break;
				case 4: DrawScanlinesTab(); break;
			}
			egui.EndVertical();
			egui.Space();
			egui.Space();

			if ( isChangingPhaseRotation || isChangingScalingCenter || EditorGUI.EndChangeCheck() )
				EditorUtility.SetDirty( e );
		}

		#region OptionTabs

		public void DrawGeneralTab()
		{
			e.vars.twoSided = egui.Toggle( "Two Sided" , e.vars.twoSided );
			e.vars.mainColor = egui.ColorField( "Global Tint" , e.vars.mainColor );

			showImages = EditorGUI.Foldout( egui.GetControlRect() , showImages , "Textures" , true );
			if ( showImages )
			{
				//e.vars.mainTexture2 = ( Texture )egui.ObjectField( "Swap Texture" , e.vars.mainTexture2 , typeof( Texture ) , false );
				e.transitionCamera = ( Camera )egui.ObjectField( "Swap Texture Camera" , e.transitionCamera , typeof( Camera ) , true );
				e.vars.noiseTexture = ( Texture )egui.ObjectField( "Scatter Map" , e.vars.noiseTexture , typeof( Texture ) , false );
			}

			e.vars.tileCount = egui.Vector2Field( "Tile Count" , e.vars.tileCount );
			e.vars.squareTiles = egui.Toggle( "Square Tiles" , e.vars.squareTiles );
			e.vars.scaling = egui.Vector2Field( "Tile Scaling" , e.vars.scaling );

			e.vars.scaleAroundTile = egui.Slider( "Tile Centric Scaling" , e.vars.scaleAroundTile , 0f , 1f );

			e.vars.complex = egui.Toggle( "Use Complex Effects" , e.vars.complex );
			if ( e.vars.complex )
			{
				e.vars.flicker = egui.Slider( "Flicker" , e.vars.flicker , 0f , 1f );
				e.vars.backfaceVisibility = egui.Slider( "Backface Visibility" , e.vars.backfaceVisibility , 0f , 1f );
			}
		}

		//ABERRATION COMPLEX IDLE POST RADIAL SCANLINES TEXTURE_SWAP _CLIPPEDTILES_ON _SCALEAROUNDTILE_ON

		public void DrawTransitionTab()
		{
			e.vars.effectColor = egui.ColorField( "Effect Tint" , e.vars.effectColor );
			e.vars.sharpness = egui.Slider( "Transition Sharpness" , e.vars.sharpness , 0f , 1f );
			e.vars.scattering = egui.Slider( "Scatter Map Influence" , e.vars.scattering , 0f , 1f );
			e.vars.invertPhase = egui.Toggle( "Invert Phase" , e.vars.invertPhase );
			e.vars.flash = egui.Slider( "Effect Flash" , e.vars.flash , 0f , 1f );

			egui.Space();
			sed.DrawHelp( "_Radial" );
			egui.BeginHorizontal();
			gui.FlexibleSpace();
			gui.Label( "Hold Shift To Ignore Snapping On Grids." , EditorStyles.boldLabel );
			gui.FlexibleSpace();
			egui.EndHorizontal();

			egui.BeginHorizontal();
			gui.FlexibleSpace();

			e.vars.RotationRadial = sed.RotationField( e.vars.RotationRadial , true , ref isChangingPhaseRotation , "Start Location" , 100f , 16f , Color.cyan );

			gui.FlexibleSpace();
			e.vars.scalingCenter = sed.GridVector2Field( e.vars.scalingCenter , ref isChangingScalingCenter , "Scaling Center" , 100f , 16f , Color.cyan );

			gui.FlexibleSpace();
			if ( isChangingPhaseRotation || isChangingScalingCenter )
				Repaint();
			egui.EndHorizontal();
		}

		public void DrawIdleTab()
		{
			e.vars.idleAmount = egui.Slider( "Idle Animation Strength" , e.vars.idleAmount , 0f , 1f );
			e.vars.idleSpeed = egui.Slider( "Idle Animation Speed" , e.vars.idleSpeed , 0f , 1f );
			e.vars.idleRand = egui.Slider( "Idle Animation Noise" , e.vars.idleRand , 0f , 1f );
			e.vars.invertIdle = egui.Toggle( "Reverse Idle Animation" , e.vars.invertIdle );
		}

		public void DrawEffectsTab()
		{
			e.vars.overbright = egui.Slider( "Global Overbrighten" , e.vars.overbright , 0f , 1f );

			e.vars.aberration = egui.Slider( "Color Aberration" , e.vars.aberration , 0f , 1f );
			e.vars.effectAberration = egui.Slider( "Effect Aberration" , e.vars.effectAberration , 0f , 1f );
			e.vars.clipTiles = egui.Toggle( "Clip Tile Content" , e.vars.clipTiles );
			if ( e.vars.clipTiles )
				e.vars.roundClipping = egui.Toggle( "Round Tile Clipping" , e.vars.roundClipping );
		}

		public void DrawScanlinesTab()
		{
			e.vars.scanlineIntensity = egui.Slider( "Scanline Intensity" , e.vars.scanlineIntensity , 0f , 1f );
			e.vars.scanlineScale = egui.Slider( "Scanline Scale" , e.vars.scanlineScale , 0f , 1f );
			e.vars.scanlineSpeed = egui.Slider( "Scanline Speed" , e.vars.scanlineSpeed , 0f , 1f );
			e.vars.scanlineShift = egui.Slider( "Scanline Shift" , e.vars.scanlineShift , 0f , 1f );
		}
		#endregion
	}
}