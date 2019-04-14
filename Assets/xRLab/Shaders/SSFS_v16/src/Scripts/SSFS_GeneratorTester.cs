/*
Monobehaviour used to test the SSFSGenerator object behaviour
*/

using UnityEngine;

namespace SSFS
{
	[RequireComponent(typeof(MeshRenderer))]
	public class SSFS_GeneratorTester : MonoBehaviour
	{
		MeshRenderer _mr;
		MeshRenderer mr { get { if ( _mr == null ) _mr = GetComponent<MeshRenderer>(); return _mr; } }
		Material _m;
		Material m  { get { if ( _m == null ) _m = generator.GenerateMaterial(); return _m; } }

		public SSFSGenerator generator = null;
		public KeyCode key = KeyCode.R;

		private void Start()
		{
			mr.material = m;
		}

		private void Update()
		{
			if ( Input.GetKeyDown( key ) )
			{
				if ( generator == null )
					Debug.Log( "Null SSFS Generator" );
				else
					generator.GenerateMaterial( ref _m );
			}
		}
	}
}