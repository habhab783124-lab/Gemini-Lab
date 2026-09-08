Shader "GeminiLab/WorldMap/CloudColorKey"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _KeyColor ("Key Color", Color) = (1,1,1,1)
        _KeyThreshold ("Key Threshold", Range(0,1)) = 0.08
        _KeyFeather ("Key Feather", Range(0.001,0.25)) = 0.06
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment CloudFrag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnitySprites.cginc"

            fixed4 _KeyColor;
            half _KeyThreshold;
            half _KeyFeather;

            fixed4 CloudFrag(v2f IN) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(IN.texcoord) * IN.color;
                half distanceToKey = distance(color.rgb, _KeyColor.rgb);
                half alpha = smoothstep(_KeyThreshold, _KeyThreshold + _KeyFeather, distanceToKey);
                color.a *= alpha;
                return color;
            }
            ENDCG
        }
    }
}
