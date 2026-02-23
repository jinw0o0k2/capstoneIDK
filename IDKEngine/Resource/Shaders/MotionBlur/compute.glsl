#version 460 core

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(binding = 0) uniform sampler2D SamplerColor;
layout(binding = 1) uniform sampler2D SamplerVelocity;
layout(binding = 0, rgba16f) restrict writeonly uniform image2D ImgResult;

layout(location = 0) uniform int Samples;
layout(location = 1) uniform float BlurStrength;

void main()
{
    ivec2 imgCoord = ivec2(gl_GlobalInvocationID.xy);
    ivec2 imgSize = imageSize(ImgResult);
    
    if (imgCoord.x >= imgSize.x || imgCoord.y >= imgSize.y)
    {
        return;
    }

    vec2 uv = (vec2(imgCoord) + 0.5) / vec2(imgSize);
    vec2 velocity = texture(SamplerVelocity, uv).rg * BlurStrength;
    
    vec3 resultColor = texture(SamplerColor, uv).rgb;

    // 괄호 오류를 방지하기 위해 계산식을 최대한 분해했습니다.
    if (Samples > 1)
    {
        for (int i = 1; i < Samples; i++)
        {
            float t = float(i) / float(Samples - 1);
            float offsetMultiplier = t - 0.5;
            vec2 offset = velocity * offsetMultiplier;
            resultColor += texture(SamplerColor, uv + offset).rgb;
        }
        resultColor = resultColor / float(Samples);
    }

    imageStore(ImgResult, imgCoord, vec4(resultColor, 1.0));
}







// 파일의 끝을 드라이버가 인식할 수 있게 이 아래에 엔터를 여러 번 누르세요.
// End