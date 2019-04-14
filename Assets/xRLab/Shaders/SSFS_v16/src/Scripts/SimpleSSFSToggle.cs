using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using guil = UnityEngine.GUILayout;
using eguil = UnityEditor.EditorGUILayout;
#endif

namespace SSFS
{
	public class SimpleSSFSToggle : MonoBehaviour
	{
		public enum TargetMode { Material , Renderer }
		public enum ToggleMode { KeyPress , KeyHold , Boolean , Timer }
		public TargetMode targetMode = TargetMode.Material;
		public ToggleMode toggleMode = ToggleMode.KeyPress;
		public Material material;
		public Renderer targetRenderer;
		public bool phaseOn = true;
		public KeyCode key = KeyCode.E;
		public float timerDelay = 5f;
		public float transitionLength = 0.2f;

		float targetPhase = 1f;
		float timer = 0f;
		bool transitioning = false;

		Material _mat;
		Material mat
		{
			get
			{
				if ( _mat == null )
				{
					switch ( targetMode )
					{
						case TargetMode.Material:
							_mat = material;
							break;
						case TargetMode.Renderer:
							_mat = targetRenderer.sharedMaterial;
							break;
					}
				}
				return _mat;
			}
		}

		private void Update()
		{
			float p = mat.GetFloat( "_Phase" );

			switch (toggleMode)
			{
				case ToggleMode.KeyPress:
					if ( Input.GetKeyDown( key ) && !transitioning )
					{
						transitioning = true;
						targetPhase = p > 0.5f ? 0f : 1f;
					}
					break;
				case ToggleMode.KeyHold:
					targetPhase = Input.GetKey( key ) ? 1f : 0f;
					transitioning = p != targetPhase;
					break;
				case ToggleMode.Boolean:
					targetPhase = phaseOn ? 1f : 0f;
					transitioning = p != targetPhase;
					break;
				case ToggleMode.Timer:
					timer += Time.deltaTime;
					if ( timer > timerDelay )
					{
						transitioning = true;
						targetPhase = p > 0.5f ? 0f : 1f;
						timer = 0f;
					}
					break;
			}

			if ( transitioning )
			{
				if ( Mathf.Abs( p - targetPhase ) > 0.0001f )
					mat.SetFloat( "_Phase" , Mathf.MoveTowards( p , targetPhase , Time.deltaTime / transitionLength ) );
				else
					transitioning = false;
			}
		}
	}
}

#if UNITY_EDITOR

namespace SSFS
{
	[CustomEditor(typeof(SimpleSSFSToggle))]
	public class SimpleSSFSToggleEditor : Editor
	{
		SimpleSSFSToggle t { get { return ( SimpleSSFSToggle )target; } }

		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();
			GUIContent gc = new GUIContent( "null" , "null" );

			guil.Space( 8f );
			gc = new GUIContent( "Target" , "Where to get the material to change." );
			t.targetMode = ( SimpleSSFSToggle.TargetMode )eguil.EnumPopup( gc , t.targetMode );

			switch (t.targetMode)
			{
				case SimpleSSFSToggle.TargetMode.Material:
					gc = new GUIContent( "Material" , "Target Material" );
					t.material = ( Material )eguil.ObjectField( gc , t.material , typeof( Material ) , true );
					break;

				case SimpleSSFSToggle.TargetMode.Renderer:
					gc = new GUIContent( "Renderer" , "Target Renderer" );
					t.targetRenderer = ( Renderer )eguil.ObjectField( gc , t.targetRenderer , typeof( Renderer ) , true );
					break;
			}
			gc = new GUIContent( "Transition Time" , "Length In Seconds" );
			t.transitionLength = eguil.FloatField( gc , t.transitionLength );
			guil.Space( 12f );
			gc = new GUIContent( "Toggle Mode" , "Method used for material toggling." );
			t.toggleMode = ( SimpleSSFSToggle.ToggleMode )eguil.EnumPopup( gc , t.toggleMode );
			switch (t.toggleMode )
			{
				case SimpleSSFSToggle.ToggleMode.KeyPress:
				case SimpleSSFSToggle.ToggleMode.KeyHold:
					gc = new GUIContent( "Key" , "Key used to toggle." );
					t.key = ( KeyCode )eguil.EnumPopup( gc , t.key );
					break;
				case SimpleSSFSToggle.ToggleMode.Boolean:
					gc = new GUIContent( "Phase On" , "Whether the material hase a _Phase value of 1.0 or not." );
					t.phaseOn = eguil.Toggle( gc , t.phaseOn );
					break;
				case SimpleSSFSToggle.ToggleMode.Timer:
					gc = new GUIContent( "Delay" , "Length of time before the material toggles." );
					t.timerDelay = eguil.FloatField( gc , t.timerDelay );
					break;
			}
			if ( EditorGUI.EndChangeCheck() ) EditorUtility.SetDirty( target );
			guil.Space( 8f );
		}
	}
}

#endif