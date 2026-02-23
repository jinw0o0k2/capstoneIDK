using System;
using OpenTK.Mathematics;
using BBOpenGL;
using IDKEngine.Utils;

namespace IDKEngine.Render;

class MotionBlur : IDisposable
{
    // C#에서 셰이더로 넘겨줄 설정값
    public int Samples = 10;
    public float BlurStrength = 0.5f;

    // 최종 결과물이 담길 텍스처
    public BBG.Texture Result => resultTexture;
    private BBG.Texture resultTexture;
    
    private readonly BBG.AbstractShaderProgram shaderProgram;

    public MotionBlur(Vector2i size)
    {
        // 셰이더 로드 (경로를 엔진 폴더 구조에 맞게 "MotionBlur/compute.glsl" 등으로 맞추세요)
        shaderProgram = new BBG.AbstractShaderProgram(BBG.AbstractShader.FromFile(BBG.ShaderStage.Compute, "MotionBlur/compute.glsl"));
        SetSize(size);
    }

    public void Compute(BBG.Texture colorSrc, BBG.Texture velocitySrc)
    {
        BBG.Cmd.UseShaderProgram(shaderProgram);

        // 유니폼 변수 업로드 (GLSL의 location과 매칭됨)
        shaderProgram.Upload(0, Samples);      // location 0 에 샘플 수
        shaderProgram.Upload(1, BlurStrength); // location 1 에 강도

        // 텍스처 바인딩 (Color: 0번, Velocity: 1번)
        BBG.Cmd.BindTextureUnit(colorSrc, 0);
        BBG.Cmd.BindTextureUnit(velocitySrc, 1);
        
        // 컴퓨트 셰이더가 결과를 기록할 이미지 바인딩
        BBG.Cmd.BindImageUnit(resultTexture, 0); 

        // BBG 프레임워크의 프로파일링/디스패치 기능 사용
        BBG.Computing.Compute("Apply Motion Blur", () =>
        {
            // 8x8 워크그룹 단위로 화면 전체 디스패치
            BBG.Computing.Dispatch(MyMath.DivUp(resultTexture.Width, 8), MyMath.DivUp(resultTexture.Height, 8), 1);
            BBG.Cmd.MemoryBarrier(BBG.Cmd.MemoryBarrierMask.TextureFetchBarrierBit);
        });
    }

    public void SetSize(Vector2i size)
    {
        if (resultTexture != null) resultTexture.Dispose();
        
        resultTexture = new BBG.Texture(BBG.Texture.Type.Texture2D);
        resultTexture.SetFilter(BBG.Sampler.MinFilter.Linear, BBG.Sampler.MagFilter.Linear);
        resultTexture.SetWrapMode(BBG.Sampler.WrapMode.ClampToEdge, BBG.Sampler.WrapMode.ClampToEdge);
        // 고해상도 처리를 위해 RGBA16 Float 포맷으로 할당
        resultTexture.Allocate(size.X, size.Y, 1, BBG.Texture.InternalFormat.R16G16B16A16Float);
    }

    public void Dispose()
    {
        resultTexture.Dispose();
        shaderProgram.Dispose();
    }
}