using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct IntVector3
{
    public int x;
    public int y;
    public int z;

    public IntVector3(int _x, int _y, int _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }
}

    public class ComputeParticlePositions : MonoBehaviour {
    [Header("Shaders")]
    public ComputeShader computeShader;
    public Shader particleShader;

    [Space(10.0f)]

    [Header("Particle System Attributes")]
    [Range(0, 1)]
    public float animateSpeed = 0.1f;
    public float particleSpacing = 1;   // spacing between the particles
    public IntVector3 particleBoxSize = new IntVector3(16, 16, 16);   // length of the sides of the particle box 

    [Space(10.0f)]
    [Header("Particle Properties")]
    public Texture2D particleSprite;
    public Vector2 particleSize = Vector2.one;
    public Color globalTint = Color.white;
    
    // compute shader buffers for retrieving data from and sending data to the compute shader
    private ComputeBuffer constantBuffer;
    private ComputeBuffer particleMapBuffer;

    // variables
    private int kernel; // storing the compute shader's kernel ("main" function)
    private int boxVolume;  // volume of the box, based off the side length
    private float time; // storing time for animation
    private Material particleMaterial;  // particle material created from the particle shader

    void Start()
    {
        // initialise time variable
        time = 0f;

        // calculate box volume
        boxVolume = particleBoxSize.x * particleBoxSize.y * particleBoxSize.z;
        
        // initialise material
        particleMaterial = new Material(particleShader);

        // find the compute shader's "main" function and store it
        kernel = computeShader.FindKernel("CSMain");

        // create 1 dimensional buffer of float3's with a length of the box volume
        // this stores the particles' positions and colours
        particleMapBuffer = new ComputeBuffer(boxVolume, sizeof(float) * 6);
    }

    private void Update()
    {
        // increment time by animation speed
        time += Time.deltaTime * animateSpeed;

        // pass data to the shader
        SetShaderBuffers();

        // run the shader
        Dispatch();
    }

    // render the material
    void OnRenderObject()
    {
        // procedurally draw all the particles as a set of points with the particle material
        particleMaterial.SetPass(0);
        particleMaterial.SetColor("_Tint", globalTint);
        particleMaterial.SetBuffer("points", particleMapBuffer);
        particleMaterial.SetTexture("_Sprite", particleSprite);
        particleMaterial.SetVector("_Size", particleSize);
        particleMaterial.SetVector("_worldPos", transform.position);
        
        Graphics.DrawProcedural(MeshTopology.Lines, particleMapBuffer.count);
    }

    private void SetShaderBuffers()
    {
        // pass data to the shader by setting buffers
        computeShader.SetBuffer(kernel, "particleMapBuffer", particleMapBuffer);
        computeShader.SetInt("numThreadGroupsX", particleBoxSize.x / 8);
        computeShader.SetInt("numThreadGroupsY", particleBoxSize.y / 8);
        computeShader.SetInt("numThreadGroupsZ", particleBoxSize.z / 8);
        computeShader.SetFloat("time", time);
        computeShader.SetFloat("spacing", particleSpacing);
    }

    private void Dispatch()
    {
        // dispatch the compute shader
        // number of thread groups to run is the size of the particleMap divided by number of threads per dimension
        computeShader.Dispatch(kernel, particleBoxSize.x / 8, particleBoxSize.y / 8, particleBoxSize.z / 8);
    }

    // when this GameObject is disabled, release the buffers and materials
    private void OnDisable()
    {
        particleMapBuffer.Release();
        DestroyImmediate(particleMaterial);
    }
}
