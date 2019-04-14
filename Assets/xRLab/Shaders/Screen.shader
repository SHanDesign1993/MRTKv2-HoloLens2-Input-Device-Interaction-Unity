Shader "Custom/MaskScreen" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Mask("Mask Texture", 2D) = "white" {}
	}
		SubShader{

			Tags {"Queue" = "Transparent"}

			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			Pass{
				SetTexture[_Mask] {combine texture}
				SetTexture[_MainTex] {combine texture, previous}
			}
	}
}
