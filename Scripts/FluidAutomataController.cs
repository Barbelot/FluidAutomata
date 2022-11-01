using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidAutomata
{
    public class FluidAutomataController : MonoBehaviour
    {
        [Header("Rendering")]
        public ComputeShader fluidAutomataCompute;

        public Vector3Int fluidSize = new Vector3Int(2048, 2048, 1);

        public Material fluidMaterial;
        public int fluidUpdatePerFrames = 1;

        [Header("Parameters")]
        public Vector2 fluidGlobalVelocity = Vector2.zero;


        [Header("Affector")]
        public GameObject affector;
        public LayerMask layerMask;
        [Range(0, 1)] [Tooltip("The radius is in UV space")] public float affectorRadius = 0.05f;

        [Header("Debug")]
        public bool initialize;

        private RaycastHit raycastHit;

        private RenderTexture fluidTexture;

        private struct FluidParticle
        {
            Vector2 velocity;
            float pressure;
            float energy;
        };

        private int FluidParticleFloatCount = 4;

        private ComputeBuffer divergenceBuffer, gradientBuffer, blurBuffer, advectionBuffer;

        private int divergenceInitKernel, pressureDivergenceKernel, pressureGradientKernel, blurKernel, advectionKernel, writeFluidTextureKernel;

        //Must match the values in the shader
        private int NUM_THREADS_1D = 16;
        private int NUM_THREADS_2D = 16;
        private int NUM_THREADS_3D = 8;

        #region MonoBehaviour Functions

        // Start is called before the first frame update
        void Start() {
            InitializeFluid();
        }

        // Update is called once per frame
        void Update() {
            if (initialize) {
                InitializeFluid(); initialize = false;
            }

            for (int i = 0; i < fluidUpdatePerFrames; i++)
                UpdateFluid();
        }

        private void OnDisable() {

            ReleaseBuffers();

        }

        private void OnValidate() {

            fluidSize = new Vector3Int(Mathf.NextPowerOfTwo(fluidSize.x), Mathf.NextPowerOfTwo(fluidSize.y), Mathf.NextPowerOfTwo(fluidSize.z));

        }

        #endregion

        #region Fluid Control

        void InitializeFluid() {

            InitializeKernels();
            InitializeBuffers();
            InitializeFluidTexture();

            fluidAutomataCompute.SetInt("_FluidBufferSizeX", fluidSize.x);
            fluidAutomataCompute.SetInt("_FluidBufferSizeY", fluidSize.y);
            fluidAutomataCompute.SetInt("_FluidBufferSizeZ", fluidSize.z);

            fluidAutomataCompute.SetVector("_FluidGlobalVelocity", fluidGlobalVelocity);

            fluidAutomataCompute.SetBuffer(divergenceInitKernel, "_DivergenceBuffer", divergenceBuffer);
            fluidAutomataCompute.Dispatch(divergenceInitKernel, fluidSize.x / NUM_THREADS_2D, fluidSize.y / NUM_THREADS_2D, 1);

        }

        void InitializeBuffers() {

            ReleaseBuffers();

            divergenceBuffer = new ComputeBuffer(fluidSize.x * fluidSize.y, sizeof(float) * FluidParticleFloatCount);
            gradientBuffer = new ComputeBuffer(fluidSize.x * fluidSize.y, sizeof(float) * FluidParticleFloatCount);
            blurBuffer = new ComputeBuffer(fluidSize.x * fluidSize.y, sizeof(float) * FluidParticleFloatCount);
            advectionBuffer = new ComputeBuffer(fluidSize.x * fluidSize.y, sizeof(float) * FluidParticleFloatCount);
        }

        void InitializeKernels() {

            divergenceInitKernel = fluidAutomataCompute.FindKernel("DivergenceInit");
            pressureDivergenceKernel = fluidAutomataCompute.FindKernel("PressureDivergence");
            pressureGradientKernel = fluidAutomataCompute.FindKernel("PressureGradient");
            blurKernel = fluidAutomataCompute.FindKernel("Blur");
            advectionKernel = fluidAutomataCompute.FindKernel("Advection");
            writeFluidTextureKernel = fluidAutomataCompute.FindKernel("WriteFluidTexture");

        }

        void InitializeFluidTexture() {

            fluidTexture = new RenderTexture(fluidSize.x, fluidSize.y, 1, RenderTextureFormat.ARGBFloat);
            fluidTexture.enableRandomWrite = true;
            fluidTexture.Create();

            fluidMaterial.SetTexture("_FluidTexture", fluidTexture);
        }

        void UpdateFluid() {

            //Update affectors
            if (Physics.Raycast(affector.transform.position, Vector3.down, out raycastHit, 100.0f, layerMask)) {
                fluidAutomataCompute.SetVector("_AffectorPosition", raycastHit.textureCoord);
            } else {
                fluidAutomataCompute.SetVector("_AffectorPosition", -Vector2.one);
            }

            fluidAutomataCompute.SetFloat("_AffectorRadius", affectorRadius);

            //Update global parameters
            fluidAutomataCompute.SetFloat("_Time", Time.time);
            fluidAutomataCompute.SetVector("_FluidGlobalVelocity", fluidGlobalVelocity);

            fluidAutomataCompute.SetInt("_FluidBufferSizeX", fluidSize.x);
            fluidAutomataCompute.SetInt("_FluidBufferSizeY", fluidSize.y);
            fluidAutomataCompute.SetInt("_FluidBufferSizeZ", fluidSize.z);

            //Update fluid
            ComputePressureDivergence();
            ComputePressureGradient();
            ComputeBlur();
            ComputeAdvection();
            ComputeWriteFluidTexture();

        }

        void ComputePressureDivergence() {

            fluidAutomataCompute.SetBuffer(pressureDivergenceKernel, "_DivergenceBuffer", divergenceBuffer);
            fluidAutomataCompute.SetBuffer(pressureDivergenceKernel, "_AdvectionBuffer", advectionBuffer);

            fluidAutomataCompute.Dispatch(pressureDivergenceKernel, fluidSize.x / NUM_THREADS_2D, fluidSize.y / NUM_THREADS_2D, 1);
        }

        void ComputePressureGradient() {

            fluidAutomataCompute.SetBuffer(pressureGradientKernel, "_DivergenceBuffer", divergenceBuffer);
            fluidAutomataCompute.SetBuffer(pressureGradientKernel, "_GradientBuffer", gradientBuffer);

            fluidAutomataCompute.Dispatch(pressureGradientKernel, fluidSize.x / NUM_THREADS_2D, fluidSize.y / NUM_THREADS_2D, 1);
        }

        void ComputeBlur() {

            fluidAutomataCompute.SetBuffer(blurKernel, "_BlurBuffer", blurBuffer);
            fluidAutomataCompute.SetBuffer(blurKernel, "_GradientBuffer", gradientBuffer);

            fluidAutomataCompute.Dispatch(blurKernel, fluidSize.x / NUM_THREADS_2D, fluidSize.y / NUM_THREADS_2D, 1);
        }

        void ComputeAdvection() {

            fluidAutomataCompute.SetBuffer(advectionKernel, "_BlurBuffer", blurBuffer);
            fluidAutomataCompute.SetBuffer(advectionKernel, "_GradientBuffer", gradientBuffer);
            fluidAutomataCompute.SetBuffer(advectionKernel, "_AdvectionBuffer", advectionBuffer);

            fluidAutomataCompute.Dispatch(advectionKernel, fluidSize.x / NUM_THREADS_2D, fluidSize.y / NUM_THREADS_2D, 1);
        }

        void ComputeWriteFluidTexture() {

            fluidAutomataCompute.SetBuffer(writeFluidTextureKernel, "_DivergenceBuffer", divergenceBuffer);
            fluidAutomataCompute.SetTexture(writeFluidTextureKernel, "_FluidTexture", fluidTexture);

            fluidAutomataCompute.Dispatch(writeFluidTextureKernel, fluidSize.x / NUM_THREADS_2D, fluidSize.y / NUM_THREADS_2D, 1);
        }

        void ReleaseBuffers() {

            if (divergenceBuffer != null) divergenceBuffer.Release();
            if (gradientBuffer != null) gradientBuffer.Release();
            if (blurBuffer != null) blurBuffer.Release();
            if (advectionBuffer != null) advectionBuffer.Release();
        }

        #endregion
    }
}
