using System;
using UnityEngine;

[Serializable]
public struct Dimensions
{
    public int width;
    public int height;
    public int depth;
}

[Serializable]
public struct BrushProperties
{
    public float brushRadius;
    public float brushValue;
}

public class Driver : MonoBehaviour
{
    [SerializeField] ComputeShader _computeShader;
    [SerializeField] MeshRenderer _boundingBoxMesh;
    [SerializeField] Dimensions _dimensions;
    [SerializeField] FilterMode _filterMode;
    [SerializeField] float _stepRate;
    [SerializeField] int _maxStepsPerUpdate = 1;
    [SerializeField] float _timeSyncOffset;
    [SerializeField] bool _isBrushEnabled;
    [SerializeField] BrushProperties _brushProperties;
    [SerializeField] float _interpolation;

    private RenderTexture _bufferA, _bufferB;
    private int _step;

    void Start()
    {
        Reconfigure();
    }

    void Reconfigure()
    {
        ConfigureBuffer(ref _bufferA);
        ConfigureBuffer(ref _bufferB);

        _boundingBoxMesh.sharedMaterial.mainTexture = _bufferA;

        _computeShader.SetFloat("Width", _dimensions.width);
        _computeShader.SetFloat("Height", _dimensions.height);
        _computeShader.SetFloat("Depth", _dimensions.depth);
    }

    void ConfigureBuffer(ref RenderTexture buffer)
    {
        if (buffer != null)
        {
            buffer.Release();
        }

        buffer = new RenderTexture(_dimensions.width, _dimensions.height, 0);
        buffer.width = _dimensions.width;
        buffer.height = _dimensions.width;
        buffer.filterMode = _filterMode;
        buffer.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        buffer.volumeDepth = _dimensions.depth;
        buffer.wrapMode = TextureWrapMode.Clamp;
        buffer.enableRandomWrite = true;
        buffer.Create();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            print("Reconfigure");
            Reconfigure();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            print("Randomize");
            Randomize();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            print("Gradient");
            Gradient();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            print("Clear");
            Clear();
        }

        if (_isBrushEnabled && Input.GetMouseButton(0))
        {
            print("Modify");
            Modify();
        }

        _timeSyncOffset += Time.deltaTime;
        int stepsThisUpdate = 0;
        while (_timeSyncOffset * _stepRate >= 1 && stepsThisUpdate < _maxStepsPerUpdate)
        {
            _timeSyncOffset -= 1 / _stepRate;
            Step();
            stepsThisUpdate++;
        }
    }

    private void Clear()
    {
        int clearKernel = _computeShader.FindKernel("Clear");
        _computeShader.SetTexture(clearKernel, "Input", GetCurrentInputBuffer());
        _computeShader.GetKernelThreadGroupSizes(clearKernel, out uint xGroupSize, out uint yGroupSize, out uint zGroupSize);
        _computeShader.Dispatch(clearKernel, Mathf.CeilToInt(_dimensions.width / (float)xGroupSize), Mathf.CeilToInt(_dimensions.height / (float)yGroupSize), Mathf.CeilToInt(_dimensions.depth / (float)zGroupSize));
    }

    private void Randomize()
    {
        int randomizeKernel = _computeShader.FindKernel("Randomize");
        _computeShader.SetFloat("Time", Time.time);
        _computeShader.SetTexture(randomizeKernel, "Input", GetCurrentInputBuffer());
        _computeShader.GetKernelThreadGroupSizes(randomizeKernel, out uint xGroupSize, out uint yGroupSize, out uint zGroupSize);
        _computeShader.Dispatch(randomizeKernel, Mathf.CeilToInt(_dimensions.width / (float)xGroupSize), Mathf.CeilToInt(_dimensions.height / (float)yGroupSize), Mathf.CeilToInt(_dimensions.depth / (float)zGroupSize));
    }

    private void Gradient()
    {
        int gradientKernel = _computeShader.FindKernel("Gradient");
        _computeShader.SetTexture(gradientKernel, "Input", GetCurrentInputBuffer());
        _computeShader.GetKernelThreadGroupSizes(gradientKernel, out uint xGroupSize, out uint yGroupSize, out uint zGroupSize);
        _computeShader.Dispatch(gradientKernel, Mathf.CeilToInt(_dimensions.width / (float)xGroupSize), Mathf.CeilToInt(_dimensions.height / (float)yGroupSize), Mathf.CeilToInt(_dimensions.depth / (float)zGroupSize));
    }

    private void Modify()
    {
        int modifyKernel = _computeShader.FindKernel("Modify");
        _computeShader.SetTexture(modifyKernel, "Input", GetCurrentInputBuffer());
        _computeShader.SetFloat("BrushRadius", _brushProperties.brushRadius);
        _computeShader.SetFloat("BrushValue", _brushProperties.brushValue);
        _computeShader.SetVector("BrushLocation", Camera.main.ScreenToWorldPoint(Input.mousePosition)); // TODO: improve non-VR 3D brush positioning
        _computeShader.GetKernelThreadGroupSizes(modifyKernel, out uint xGroupSize, out uint yGroupSize, out uint zGroupSize);
        _computeShader.Dispatch(modifyKernel, Mathf.CeilToInt(_dimensions.width / (float)xGroupSize), Mathf.CeilToInt(_dimensions.height / (float)yGroupSize), Mathf.CeilToInt(_dimensions.depth / (float)zGroupSize));
    }

    private void Step()
    {
        int stepKernel = _computeShader.FindKernel("Step");
        _computeShader.SetFloat("Interpolation", _interpolation);
        _computeShader.SetTexture(stepKernel, "Input", GetCurrentInputBuffer());
        _computeShader.SetTexture(stepKernel, "Output", GetCurrentOutputBuffer());
        _computeShader.GetKernelThreadGroupSizes(stepKernel, out uint xGroupSize, out uint yGroupSize, out uint zGroupSize);
        _computeShader.Dispatch(stepKernel, Mathf.CeilToInt(_dimensions.width / (float)xGroupSize), Mathf.CeilToInt(_dimensions.height / (float)yGroupSize), Mathf.CeilToInt(_dimensions.depth / (float)zGroupSize));
        _boundingBoxMesh.material.mainTexture = GetCurrentOutputBuffer();
        _step++;
    }

    private RenderTexture GetCurrentInputBuffer()
    {
        return _step % 2 == 0 ? _bufferA : _bufferB;
    }

    private RenderTexture GetCurrentOutputBuffer()
    {
        return _step % 2 == 1 ? _bufferA : _bufferB;
    }
}