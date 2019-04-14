using UnityEngine;
using System.Collections;

namespace SSFS
{
	public static partial class MaterialExtensions
	{
		//call this on an GameObject using StartCoroutine( material.SSFSPhaseTo() )
		public static IEnumerator SSFSPhaseTo( this Material m , float endPhase , float time = 1f )
		{
			float o = m.GetFloat( "_Phase" );
			while ( Mathf.Abs( o - endPhase ) > 0.0001 )
			{
				float s = Time.deltaTime / time;
				Mathf.MoveTowards( o , endPhase , s );
				yield return new WaitForEndOfFrame();
			}
		}

		public static void SyncKeyword( this Material m , string keyword , bool state )
		{
			if ( m == null ) return;
			if ( state ) m.EnableKeyword( keyword ); else m.DisableKeyword( keyword );
		}

		public static void SetVector( this Material m , string name , Vector2 v1 , Vector2 v2 )
		{
			m.SetVector( name , new Vector4( v1.x , v1.y , v2.x , v2.y ) );
		}

		public static void GetVector( this Material m , string name , out Vector2 v1 , out Vector2 v2 )
		{
			Vector4 v0 = m.GetVector( name );
			v1 = new Vector2( v0.x , v0.y );
			v2 = new Vector2( v0.z , v0.w );
		}

		public static Vector4 Append( this Vector2 v1 , Vector2 v2 )
		{
			return new Vector4( v1.x , v1.y , v2.x , v2.y );
		}

		public static void Split( this Vector4 v0 , out Vector2 v1 , out Vector2 v2 )
		{
			v1 = new Vector2( v0.x , v0.y );
			v2 = new Vector2( v0.z , v0.w );
		}
	}
}
