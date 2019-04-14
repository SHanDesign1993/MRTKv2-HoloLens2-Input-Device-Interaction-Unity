using UnityEngine;
using System.Collections.Generic;
using gui = UnityEngine.GUI;
using guil = UnityEngine.GUILayout;

#if UNITY_EDITOR
using UnityEditor;
using egui = UnityEditor.EditorGUI;
using eguil = UnityEditor.EditorGUILayout;
#endif

/// <summary>
/// An object that generates SSFS Materials based on its randomized parameters.
/// </summary>
[CreateAssetMenu( fileName = "SSFS Generator" , menuName = "SSFS/Material Generator" , order = 302 )]
public class SSFSGenerator : ScriptableObject
{
	public TextureList textures = new TextureList();
	public TextureList scatters = new TextureList();
	public RandomColor baseTint = new RandomColor();
	public RandomColor transitionTint = new RandomColor();

	public boolchance useTextureSwap = new boolchance( false , 0.1f );
	public boolchance useImageAsScatter = new boolchance( false , 0.1f );
	public boolchance useRandomTileCount = new boolchance( true , 1f );

	public boolchance allowIdleAnimation = new boolchance( true , 0.5f );
	public boolchance allowIdleNoise = new boolchance( true , 0.2f );
	public RandomRange idleNoiseStrength = new RandomRange();
	public RandomRange idleIntensity = new RandomRange();
	public RandomRange idleSpeed = new RandomRange();
	public boolchance allowIdleReverse = new boolchance( true , 0.2f );
	public boolchance allowTileClipping = new boolchance( true , 0.2f );

	public boolchance allowRadial = new boolchance( true , 0.5f );
	public RandomRange phaseDirection = new RandomRange();

	public boolchance separateTileCounts = new boolchance( true , 0.2f );
	public RandomRangeInt tileCountUniform = new RandomRangeInt();
	public RandomRangeInt tileCountX = new RandomRangeInt();
	public RandomRangeInt tileCountY = new RandomRangeInt();

	public boolchance separateAxisScaling = new boolchance( true , 0.2f );
	public boolchance allowTileCentricScaling = new boolchance( true , 0.2f );
	public RandomRange scalingUniform = new RandomRange();
	public RandomRange scalingX = new RandomRange();
	public RandomRange scalingY = new RandomRange();

	public RandomRange scattering = new RandomRange();
	public RandomRange phaseSharpness = new RandomRange();
	public RandomRange overbright = new RandomRange();
	public RandomRange aberration = new RandomRange();
	public RandomRange effectAberration = new RandomRange();
	public RandomRange scanlineIntensity = new RandomRange();
	public RandomRange scanlineDistortion = new RandomRange();
	public RandomRange scanlineScale = new RandomRange();
	public RandomRange scanlineSpeed = new RandomRange();
	public RandomRange flash = new RandomRange();
	public RandomRange flicker = new RandomRange();

	/// <summary>
	/// Generate a new SSFS material using this generator's settings.
	/// </summary>
	public Material GenerateMaterial()
	{
		Material m = SSFS.SSFSCore.newMaterial;
		genmat( ref m );
		return m;
	}

	/// <summary>
	/// Generate an SSFS material, replacing an existing material.
	/// </summary>
	/// <param name="existingMaterial">An existing material that will be replaced by the generated material. Be careful what you pass here.</param>
	public void GenerateMaterial( ref Material existingMaterial )
	{
		if ( existingMaterial == null )
			existingMaterial = SSFS.SSFSCore.newMaterial;
		else if ( existingMaterial.shader != SSFS.SSFSCore.shader )
			existingMaterial.shader = SSFS.SSFSCore.shader;
		genmat( ref existingMaterial );
	}

	private bool SyncKeyword( ref Material m , string keyword , bool value )
	{
		if ( value ) m.EnableKeyword( keyword ); else m.DisableKeyword( keyword );
		return value;
	}

	private void genmat( ref Material m )
	{
		if ( m == null ) { Debug.Log( "Null material passed to SSFSGenerator" ); return; }
		m.EnableKeyword( "COMPLEX" );
		m.DisableKeyword( "POST" );
		m.DisableKeyword( "WORLD_SPACE_SCANLINES" );

		m.SetFloat( "_Cull" , 0 );
		m.SetFloat( "_BlendSrc" , 1 );
		m.SetFloat( "_BlendDst" , 1 );
		m.SetFloat( "_ZWrite" , 0 );
		m.SetFloat( "_ZTest" , ( int )UnityEngine.Rendering.CompareFunction.LessEqual );
		m.SetTexture( "_MainTex" , textures.texture );
		bool texSwap = useTextureSwap.check;
		m.SetTexture( "_MainTex2" , texSwap ? textures.texture : null );
		SyncKeyword( ref m , "TEXTURE_SWAP" , texSwap );
		m.SetTexture( "_Noise" , useImageAsScatter.check ? m.GetTexture( "_MainTex" ) : scatters.texture );
		m.SetColor( "_Color" , baseTint.get_color );
		m.SetColor( "_Color2" , transitionTint.get_color );
		m.SetFloat( "_Phase" , 1f );
		m.SetVector( "_PhaseDirection" , new Vector4( phaseDirection.get_float , allowRadial.check ? 1f : 0f , 0f , 0f ) );
		float iStrength = allowIdleAnimation.check ? idleIntensity.get_float : 0f;
		float iSpeed = idleSpeed.get_float;
		float iNoise = allowIdleNoise.check ? idleNoiseStrength.get_float : 0f;
		float iReverse = allowIdleReverse.check ? 1f : 0f;
		SyncKeyword( ref m , "IDLE" , iStrength > 0f );
		m.SetVector( "_IdleData" , new Vector4( iStrength , iSpeed , iNoise , iReverse ) );

		bool separateTileCountCheck = separateTileCounts.check;
		m.SetFloat( "_SquareTiles" , separateTileCountCheck ? 1f : 0f );
		int tileCount_x = separateTileCountCheck ? tileCountUniform.get_int : tileCountX.get_int;
		int tileCount_y = separateTileCountCheck ? tileCountUniform.get_int : tileCountY.get_int;
		m.SetVector( "_TileCount" , new Vector4( tileCount_x , tileCount_y , 0f , 0f ) );

		bool axisScalingCheck = separateAxisScaling.check;
		float scaling_x = axisScalingCheck ? scalingUniform.get_float : scalingX.get_float;
		float scaling_y = axisScalingCheck ? scalingUniform.get_float : scalingY.get_float;
		m.SetVector( "_Scaling" , new Vector4( scaling_x * 3.5f - 0.5f , scaling_y * 3.5f - 0.5f , 0.5f , 0.5f ) );//scale to [-0.5,3.0]
		m.SetFloat( "_ScaleAroundTile" , SyncKeyword( ref m , "SCALE_AROUND_TILE" , allowTileCentricScaling.check ) ? 1f : 0f );

		m.SetFloat( "_Scattering" , scattering.get_float );
		m.SetFloat( "_PhaseSharpness" , phaseSharpness.get_float );
		m.SetFloat( "_Overbright" , overbright.get_float );

		float ab1 = aberration.get_float;
		float ab2 = effectAberration.get_float;
		SyncKeyword( ref m , "ABERRATION" , Mathf.Max( ab1 , ab2 ) > 0f );
		m.SetFloat( "_Aberration" , ab1 );
		m.SetFloat( "_EffectAberration" , ab2 );
		m.SetFloat( "_Flash" , flash.get_float );
		m.SetFloat( "_Flicker" , flicker.get_float );

		float sl_intensity = scanlineIntensity.get_float;
		float sl_distortion = scanlineDistortion.get_float;
		SyncKeyword( ref m , "SCAN_LINES" , Mathf.Max( sl_intensity , sl_distortion ) > 0f );
		m.SetVector( "_ScanlineData" , new Vector4( sl_intensity , scanlineScale.get_float , sl_distortion , scanlineSpeed.get_float ) );


		m.SetFloat( "_ClippedTiles" , SyncKeyword( ref m , "CLIPPING" , allowTileClipping.check ) ? 1f : 0f );
	}
}

#if UNITY_EDITOR
static class styles
{
	static GUIStyle _bold;
	public static GUIStyle bold
	{
		get
		{
			if ( _bold == null )
			{
				_bold = new GUIStyle( gui.skin.label );
				_bold.fontStyle = FontStyle.Bold;
			}
			return _bold;
		}
	}

#if UNITY_EDITOR
	static GUIStyle _boldfoldout;
	public static GUIStyle boldfoldout
	{
		get
		{
			if ( _boldfoldout == null )
			{
				_boldfoldout = new GUIStyle( EditorStyles.foldout );
				_boldfoldout.fontStyle = FontStyle.Bold;
			}
			return _boldfoldout;
		}
	}
#endif
}
#endif

/// <summary>
/// Custom Texture List container for SSFSGenerator objects.
/// </summary>
[System.Serializable]
public class TextureList
{
	public List<Texture2D> list = new List<Texture2D>();
	public Texture2D texture { get { return list[ Random.Range( 0 , list.Count ) ]; } }

	public TextureList() { list = new List<Texture2D>(); }

	public Texture2D this[ int index ]
	{
		get { return list[ index ]; }
		set { list[ index ] = value; }
	}

	public int Count { get { return list.Count; } }
	public void Add( Texture2D tex ) { list.Add( tex ); }
	public void Remove( Texture2D tex ) { list.Remove( tex ); }
	public void RemoveAt( int index ) { list.RemoveAt( index ); }
	public void Clear() { list.Clear(); }

#if UNITY_EDITOR
	public void Draw( GUIContent label , ref bool foldout , ref Vector2 scr )
	{
		List<int> remove = new List<int>();
		guil.BeginVertical( gui.skin.box );

		foldout = eguil.Foldout( foldout , label , styles.boldfoldout );
		if ( !foldout ) { guil.EndVertical(); return; }

		guil.BeginHorizontal();
		if ( guil.Button( "Add New" ) ) list.Add( null );
		if ( guil.Button( "Add Selection" ) )
		{
			if ( Selection.objects.Length > 0 )
			{
				foreach ( object o in Selection.objects )
				{
					if ( o is Texture2D ) list.Add( o as Texture2D );
				}
			}
		}
		guil.EndHorizontal();
		GUI.color = new Color( 0.9f , 0.9f , 1f );
		scr = guil.BeginScrollView( scr , gui.skin.box , guil.MinHeight( 150f ) , guil.ExpandHeight( false ) );

		for ( int i = 0 ; i < list.Count ; i++ )
		{
			guil.BeginHorizontal();
			list[ i ] = ( Texture2D )eguil.ObjectField( list[ i ] , typeof( Texture2D ) , true );
			if ( guil.Button( "-" , guil.MaxWidth( 24f ) ) ) remove.Add( i );
			guil.EndHorizontal();
		}

		guil.EndScrollView();
		GUI.color = Color.white;
		guil.EndVertical();
		if ( remove.Count > 0 ) { foreach ( int i in remove ) list.RemoveAt( i ); }
	}
#endif
}

[System.Serializable]
public class boolchance
{
	public bool boolean;
	public float chance;
	public bool check { get { return boolean && Random.value <= chance; } }

	public boolchance( bool boolean , float chance )
	{
		this.boolean = boolean;
		this.chance = chance;
	}

#if UNITY_EDITOR
	public void Draw( GUIContent label )
	{
		guil.BeginVertical( gui.skin.box );
		boolean = eguil.ToggleLeft( label , boolean , styles.bold );
		if ( boolean )
		{
			guil.BeginHorizontal();
			guil.Space( 30f );
			guil.Label( "Chance" , guil.MaxWidth( 50f ) );
			chance = guil.HorizontalSlider( chance , 0f , 1f );
			chance = eguil.FloatField( chance , guil.MaxWidth( 60f ) );
			guil.EndHorizontal();
		}
		guil.EndVertical();
	}
#endif
}

/// <summary>
/// Custom color container for SSFSGenerator objects that allows for randomization.
/// </summary>
[System.Serializable]
public class RandomColor
{
	public bool useRandom = true;
	public float minHue = 0f, minSaturation = 0f, minValue = 0f;
	public float maxHue = 1f, maxSaturation = 1f, maxValue = 1f;
	public Color fixedColor = Color.white;

	public Color testcolor = Color.clear;

	public RandomColor( float minHue = 0f , float maxHue = 1f , float minSaturation = 0f , float maxSaturation = 1f , float minValue = 0f , float maxValue = 1f )
	{
		useRandom = true;
		this.minHue = minHue;
		this.maxHue = maxHue;
		this.minSaturation = minSaturation;
		this.maxSaturation = maxSaturation;
		this.minValue = minValue;
		this.maxValue = maxValue;
		testcolor = Color.clear;
	}

	public Color get_color
	{
		get
		{
			float h = Random.Range( minHue , maxHue );
			float s = Random.Range( minSaturation , maxSaturation );
			float v = Random.Range( minValue , maxValue );
			return Color.HSVToRGB( h , s , v );
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Foldable GUI draw method for inspectors.
	/// </summary>
	public void Draw( GUIContent label , ref bool foldout )
	{
		guil.BeginVertical( gui.skin.box );

		guil.BeginHorizontal();
		guil.Label( label , styles.bold );
		useRandom = eguil.ToggleLeft( "Random".gui() , useRandom );
		guil.EndHorizontal();

		if ( useRandom )
		{
			guil.BeginHorizontal();
			guil.Space( 30f );
			guil.BeginVertical();

			eguil.MinMaxSlider( "Hue".gui() , ref minHue , ref maxHue , 0f , 1f );
			eguil.MinMaxSlider( "Saturation".gui() , ref minSaturation , ref maxSaturation , 0f , 1f );
			eguil.MinMaxSlider( "Value".gui() , ref minValue , ref maxValue , 0f , 1f );

			guil.BeginHorizontal();
			if ( guil.Button( "Test" ) ) testcolor = get_color;
			eguil.ColorField( testcolor );
			guil.EndHorizontal();

			guil.EndVertical();
			guil.EndHorizontal();
		}
		else
		{
			fixedColor = eguil.ColorField( fixedColor );
		}
		guil.EndVertical();
	}
#endif
}

/// <summary>
/// Randomized float field for SSFSGenerator objects.
/// </summary>
[System.Serializable]
public class RandomRange
{
	public bool useRandom = true;
	public float min = 0f;
	public float max = 1f;
	public float def = 1f;
	public float test = 0f;
	public float get_float { get { return useRandom ? Random.Range( min , max ) : def; } }

	public RandomRange( float def = 1f , float min = 0f , float max = 1f )
	{
		useRandom = true;
		this.min = min;
		this.max = max;
		this.def = def;
		test = def;
	}

#if UNITY_EDITOR
	/// <summary>
	/// GUI draw method for inspectors.
	/// </summary>
	public void Draw( GUIContent label )
	{
		guil.BeginVertical( gui.skin.box );

		guil.BeginHorizontal();
		guil.Label( label , styles.bold );
		useRandom = guil.Toggle( useRandom , "Random".gui() );
		guil.EndHorizontal();
		if ( useRandom )
		{
			guil.BeginHorizontal();
			guil.Space( 30f );
			guil.BeginVertical();

			guil.BeginHorizontal();
			guil.Label( "Min" , guil.MaxWidth( 50f ) );
			min = guil.HorizontalSlider( min , 0f , 1f );
			min = eguil.FloatField( min , guil.MaxWidth( 60f ) );
			guil.EndHorizontal();

			guil.BeginHorizontal();
			guil.Label( "Max" , guil.MaxWidth( 50f ) );
			max = guil.HorizontalSlider( max , 0f , 1f );
			max = eguil.FloatField( max , guil.MaxWidth( 60f ) );
			guil.EndHorizontal();

			guil.EndVertical();
			guil.EndHorizontal();
		}
		else
		{
			guil.BeginHorizontal();
			guil.Space( 30f );
			guil.BeginVertical();

			guil.BeginHorizontal();
			guil.Label( "Val" , guil.MaxWidth( 50f ) );
			def = guil.HorizontalSlider( def , 0f , 1f );
			def = eguil.FloatField( def , guil.MaxWidth( 60f ) );
			guil.EndHorizontal();

			guil.EndVertical();
			guil.EndHorizontal();
		}
		guil.EndVertical();
	}
#endif
}

/// <summary>
/// Randomized integer field for SSFSGenerator objects.
/// </summary>
[System.Serializable]
public class RandomRangeInt
{
	public bool useRandom = true;
	public int min = 10;
	public int max = 1000;
	public int def = 24;
	public int test = 0;
	public int get_int { get { return useRandom ? Random.Range( min , max ) : def; } }

	public RandomRangeInt( int def = 26 , int min = 10 , int max = 100 )
	{
		useRandom = true;
		this.min = min;
		this.max = max;
		this.def = def;
		test = def;
	}

#if UNITY_EDITOR
	/// <summary>
	/// GUI draw method for inspectors.
	/// </summary>
	public void Draw( GUIContent label )
	{
		guil.BeginVertical( gui.skin.box );

		guil.BeginHorizontal();
		guil.Label( label , styles.bold );
		useRandom = guil.Toggle( useRandom , "Random".gui() );
		guil.EndHorizontal();
		if ( useRandom )
		{
			guil.BeginHorizontal();
			guil.Space( 30f );

			guil.Label( "Min" , guil.MaxWidth( 50f ) );
			min = eguil.IntField( min );
			guil.Label( "Max" , guil.MaxWidth( 50f ) );
			max = eguil.IntField( max );
			guil.EndHorizontal();
		}
		else
		{
			guil.BeginHorizontal();
			guil.Space( 30f );

			guil.Label( "Val" , guil.MaxWidth( 50f ) );
			def = eguil.IntField( def );
			guil.EndHorizontal();
		}
		guil.EndVertical();
	}
#endif
}

/// Begin Custom Editor For SSFSGenerator
#if UNITY_EDITOR

[CustomEditor( typeof( SSFSGenerator ) )]
public class SSFSGeneratorEditor : Editor
{
	SSFSGenerator _target;
	public SSFSGenerator gen { get { if ( _target == null ) _target = ( SSFSGenerator )target; return _target; } }

	bool listfold1 = false;
	bool listfold2 = false;
	Vector2 list1scr = Vector2.zero;
	Vector2 list2scr = Vector2.zero;

	bool tintfold0 = false;
	bool tintfold1 = false;
	bool tintfold2 = false;

	public override void OnInspectorGUI()
	{
		guil.Label( new GUIContent( "SSFS Generator" ) , EditorStyles.boldLabel );

		egui.BeginChangeCheck();

		gen.textures.Draw( "Images".gui() , ref listfold1 , ref list1scr );
		gen.scatters.Draw( "Scatters".gui() , ref listfold2 , ref list2scr );

		guil.BeginVertical( gui.skin.box );
		tintfold0 = eguil.Foldout( tintfold0 , "Colors".gui() , styles.boldfoldout );
		Color baseGUIColor = gui.color;
		if ( tintfold0 )
		{
			gui.color *= 0.9f;
			gen.baseTint.Draw( "Base Tint".gui() , ref tintfold1 );
			gen.transitionTint.Draw( "Transition Tint".gui() , ref tintfold2 );
			guil.EndVertical();
		}
		else
			guil.EndVertical();
		gui.color = baseGUIColor;

		gen.useTextureSwap.Draw( "Use Texture Swap".gui() );
		gen.useImageAsScatter.Draw( "Use Texture As Scatter".gui() );

		gen.separateTileCounts.Draw( "Separate Tile Counts".gui() );

		if ( gen.separateTileCounts.boolean )
		{
			gen.tileCountX.Draw( "X Tile Count".gui() );
			gen.tileCountY.Draw( "Y Tile Count".gui() );
		}
		else
			gen.tileCountUniform.Draw( "Uniform Tile Count".gui() );

		gen.phaseDirection.Draw( "Transition Direction".gui() );
		gen.allowRadial.Draw( "Radial Transition".gui() );

		gen.separateAxisScaling.Draw( "Separate Axis Scaling".gui() );
		if ( gen.separateAxisScaling.boolean )
		{
			gen.scalingX.Draw( "X Scaling".gui() );
			gen.scalingY.Draw( "Y Scaling".gui() );
		}
		else
			gen.scalingUniform.Draw( "Uniform Scaling".gui() );

		gen.allowTileCentricScaling.Draw( "Tile Centric Scaling".gui() );

		gen.allowIdleAnimation.Draw( "Idle Animation".gui() );
		if ( gen.allowIdleAnimation.boolean )
		{
			gen.idleIntensity.Draw( "Idle Strength".gui() );
			gen.idleSpeed.Draw( "Idle Speed".gui() );
			gen.allowIdleNoise.Draw( "Idle Noise".gui() );
			if ( gen.allowIdleNoise.boolean )
				gen.idleNoiseStrength.Draw( "Idle Noise Strength".gui() );
			gen.allowIdleReverse.Draw( "Idle Reverse".gui() );
		}

		gen.phaseSharpness.Draw( "Phase Sharpness".gui() );
		gen.scattering.Draw( "Scattering".gui() );
		gen.aberration.Draw( "Color Aberration".gui() );
		gen.effectAberration.Draw( "Effect Aberration".gui() );
		gen.overbright.Draw( "Overbright".gui() );
		gen.flash.Draw( "Flash".gui() );
		gen.flicker.Draw( "Flicker".gui() );

		gen.scanlineIntensity.Draw( "Scanline Intensity".gui() );
		gen.scanlineScale.Draw( "Scanline Scale".gui() );
		gen.scanlineDistortion.Draw( "Scanline Distortion".gui() );
		gen.scanlineSpeed.Draw( "Scanline Speed".gui() );

		if ( egui.EndChangeCheck() ) EditorUtility.SetDirty( target );
	}
}

/// <summary>
/// Extensions class for SSFSGenerator
/// </summary>
public static partial class SSFSGeneratorGUIExt
{
	public static GUIContent gui( this string s ) { return new GUIContent( s ); }
}

#endif