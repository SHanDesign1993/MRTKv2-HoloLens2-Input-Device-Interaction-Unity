using UnityEngine;

namespace SSFS
{
	public static class SSFSCore
	{
		public static string shaderPath { get { return "Sci-Fi/SSFS/Base"; } }

		public static Shader shader
		{
			get
			{
				Shader s = Shader.Find( shaderPath );
				if ( s == null ) Debug.LogError( "SSFS SHADER NOT FOUND" );
				return s;
			}
		}

		public static Material newMaterial
		{
			get
			{
				Material m = new Material( shader);
				if ( m == null )
					Debug.LogError( "SSFS MATERIAL CREATION FAILED" );
				else
					m.name = "New SSFS Material";
				return m;
			}
		}
	}
}
