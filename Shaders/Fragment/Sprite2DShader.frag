#version 330 core

in vec2 texCoord;

in vec4 color;

out vec4 FragColor;

uniform sampler2D texture0;

uniform sampler2D depthTex;

uniform sampler2D colorTex;

uniform vec2 viewportSize;

void main()
{
    // Sample the texel from
    // the sprite's texture

    // (and multiply the value with
    // the additional color)
    vec4 texel = texture(texture0, texCoord) * color;

    // Discard the process,
    // if alpha is 0
    if(texel.a <= 0.1f)
        discard;    

    // Get the coordinates to sample
    // from the texture of the previous
    // frame buffer
    vec2 ndc_frag = 1 * gl_FragCoord.xy / viewportSize;

    // Sample the needed color
    // from the previous framebuffer
    vec4 colorTexel = texture(colorTex, ndc_frag);

    // Sample the needed depth
    // from the previous framebuffer
    float depthTexel = texture(depthTex, ndc_frag).r;

    // If depth of sampled
    // fb is closer to the
    // camera than the texel
    if(depthTexel < gl_FragCoord.z)
    {

        // Calculate color with
        // previous fb as dominating
        // parameter
        FragColor = colorTexel * colorTexel.a + texel * (1 - colorTexel.a);
    }
    // If not
    else
    {

        // Calculate color with
        // sprite as dominating
        // parameter
        FragColor = texel * texel.a + colorTexel * (1 - texel.a);
    }
}
