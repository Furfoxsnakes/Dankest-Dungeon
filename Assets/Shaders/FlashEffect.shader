Shader "Custom/SpriteTripleFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashSpeed ("Flash Speed", Range(1, 20)) = 8.0
        _TriggerTime ("Trigger Time", Float) = -1000
        [MaterialToggle] _FlashActive ("Flash Active", Float) = 0
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
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment CustomSpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            
            float4 _FlashColor;
            float _FlashSpeed;
            float _TriggerTime;
            float _FlashActive;
            
            fixed4 CustomSpriteFrag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                
                if (_FlashActive > 0.5)
                {
                    // Calculate elapsed time since flash was triggered
                    float elapsedTime = (_Time.y - _TriggerTime) * _FlashSpeed;
                    
                    // Only flash for the duration needed for 3 flashes
                    if (elapsedTime < 3.0)
                    {
                        // Get which flash we're on (0, 1, or 2)
                        int flashIndex = floor(elapsedTime);
                        float flashPhase = frac(elapsedTime);
                        
                        // Create a pulsing effect using sine wave
                        float flashIntensity = sin(flashPhase * 3.14159);
                        
                        // Apply flash color blend
                        c.rgb = lerp(c.rgb, _FlashColor.rgb, flashIntensity * _FlashColor.a);
                    }
                }
                
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}