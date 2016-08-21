using UnityEngine;
using System.Collections;

public class Noise
{
    public enum NoiseType
    {
        Typical, //传统算法
        Gradient, //梯度算法
    }

    //生成2D PelinNoise(四方连续)
    static public void CreatePerlinNoise2D(ref RenderTexture noise, int width, int height,
                                           float frequency, float amplitude, Vector2 seed, NoiseType noiseType)
    {
        if (noise == null)
        {
            noise = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            noise.filterMode = FilterMode.Bilinear;
            noise.wrapMode = TextureWrapMode.Repeat;
        }
        string shadername = "";
        if (noiseType == NoiseType.Gradient)
            shadername = "PerlinNoise/Gradient2D";
        else
            shadername = "PerlinNoise/Typical2D";
        Material material = new Material(Shader.Find(shadername));
        material.SetFloat("_Octaves", 4.0f);
        material.SetFloat("_Persistence", 0.5f);
        material.SetFloat("_Lacunarity", 2.0f);
        material.SetFloat("_Frequency", frequency);
        material.SetFloat("_Amplitude", amplitude);
        material.SetVector("_Seed", seed);
        noise.DiscardContents();
        Graphics.Blit(null, noise, material, 0);
    }
    static public void CreatePerlinNoise2D(ref RenderTexture noise, int width, int height,
                                           float octaves, float persistence, float lacunarity,
                                           float frequency, float amplitude, Vector2 seed, NoiseType noiseType)
    {
        if (noise == null)
        {
            noise = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            noise.filterMode = FilterMode.Bilinear;
            noise.wrapMode = TextureWrapMode.Repeat;
        }
        string shadername = "";
        if (noiseType == NoiseType.Gradient)
            shadername = "PerlinNoise/Gradient2D";
        else
            shadername = "PerlinNoise/Typical2D";
        Material material = new Material(Shader.Find(shadername));
        material.SetFloat("_Octaves", octaves);
        material.SetFloat("_Persistence", persistence);
        material.SetFloat("_Lacunarity", lacunarity);
        material.SetFloat("_Frequency", frequency);
        material.SetFloat("_Amplitude", amplitude);
        material.SetVector("_Seed", seed);
        noise.DiscardContents();
        Graphics.Blit(null, noise, material, 0);
    }

    //生成云纹理(直接生成perlin noise纹理，然后锐化，避免了某些移动设备上，纹理数值不能作为底数的问题)
    static public void CreateCloudTexture(ref RenderTexture cloud, int width, int height,
                                          float frequency, float amplitude, Vector2 seed,
                                          float cloudSharpness, float cloudCover)
    {
        if (cloud == null)
        {
            cloud = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            cloud.filterMode = FilterMode.Bilinear;
            cloud.wrapMode = TextureWrapMode.Repeat;
        }
        Material material = new Material(Shader.Find("PerlinNoise/Typical2D"));
        material.SetFloat("_Octaves", 4.0f);
        material.SetFloat("_Persistence", 0.5f);
        material.SetFloat("_Lacunarity", 2.0f);
        material.SetFloat("_Frequency", frequency);
        material.SetFloat("_Amplitude", amplitude);
        material.SetVector("_Seed", seed);
        material.SetFloat("_CloudShapness", cloudSharpness);
        material.SetFloat("_CloudCover", cloudCover);
        cloud.DiscardContents();
        Graphics.Blit(null, cloud, material, 1);
    }
}
