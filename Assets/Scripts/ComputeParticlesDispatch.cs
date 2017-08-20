using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeParticlesDispatch : MonoBehaviour {
    [Header("Shaders")]
    public Shader particleShader;
    public ComputeShader computeShader;

    [Space(10.0f)]

    [Header("Particle System Attributes")]
    [Range(0, 1)]
    public float animateSpeed;
    public float spacing;   // spacing between the particles
    public int particleMapSideLength = 16;  // length of one side of the particle box

    [Space(10.0f)]
    [Header("Particle Attributes")]
    public Texture2D sprite;
    public Vector2 size = Vector2.one;
    public Color tint = Color.white;

    public enum BillboardType
    {
        world,
        cylindrical,
        spherical
    };

    public BillboardType billboardType;

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
        boxVolume = particleMapSideLength * particleMapSideLength * particleMapSideLength;
        
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
        particleMaterial.SetColor("_Tint", tint);
        particleMaterial.SetBuffer("points", particleMapBuffer);
        particleMaterial.SetTexture("_Sprite", sprite);
        particleMaterial.SetVector("_Size", size);
        particleMaterial.SetInt("_StaticCylinderSpherical", (int)billboardType);
        particleMaterial.SetVector("_worldPos", transform.position);

        Graphics.DrawProcedural(MeshTopology.Points, particleMapBuffer.count);
    }

    private void SetShaderBuffers()
    {
        // pass data to the shader by setting buffers
        computeShader.SetBuffer(kernel, "particleMapBuffer", particleMapBuffer);
        computeShader.SetInt("numThreadGroups", particleMapSideLength / 8);
        computeShader.SetFloat("time", time);
        computeShader.SetFloat("spacing", spacing);
    }

    private void Dispatch()
    {
        // dispatch the compute shader
        // number of thread groups to run is the size of the particleMap divided by number of threads per dimension
        computeShader.Dispatch(kernel, particleMapSideLength / 8, particleMapSideLength / 8, particleMapSideLength / 8);
    }

    // when this GameObject is disabled, release the buffers and materials
    private void OnDisable()
    {
        particleMapBuffer.Release();
        DestroyImmediate(particleMaterial);
    }
}
