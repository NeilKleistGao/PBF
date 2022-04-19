using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fluid : MonoBehaviour {
    [SerializeField] private Mesh mMesh;

    [SerializeField] private Material mMaterial;

    [SerializeField] private ComputeShader mSolver;

    [SerializeField] private float mRadius = 0.1f;

    [SerializeField] private Vector3 mRectMin;

    [SerializeField] private Vector3 mRectMax;

    private int mParticlesNumber;
    private Vector3[] mPosition;
    private Vector3[] mPredictionPosition;
    private Vector3[] mVelocity;

    private Bounds mBounds;

    private ComputeBuffer mPositionBuffer;

    private ComputeBuffer mPredictionPositionBuffer;
    private ComputeBuffer mVelocityBuffer;

    private int mApplyForceKernel;
    private int mUpdatePositionAndVelocityKernel;

    private void InitParticles() {
        int xWidth = Mathf.FloorToInt((mRectMax.x - mRectMin.x) * transform.localScale.x / mRadius / 2);
        int yWidth = Mathf.FloorToInt((mRectMax.y - mRectMin.y) * transform.localScale.y / mRadius / 2);
        int zWidth = Mathf.FloorToInt((mRectMax.z - mRectMin.z) * transform.localScale.z / mRadius / 2);

        mParticlesNumber = xWidth * yWidth * zWidth;
        mPosition = new Vector3[mParticlesNumber];
        mVelocity = new Vector3[mParticlesNumber];
        mPredictionPosition = new Vector3[mParticlesNumber];

        Debug.Log("Particles Number: " + mParticlesNumber);
        mBounds = new Bounds(transform.position, transform.localScale);

        for (int i = 0; i < xWidth; ++i) {
            float x = mRectMin.x + (i + 0.5f) * mRadius * 2;
            for (int j = 0; j < yWidth; ++j) {
                float y = mRectMin.y + (j + 0.5f) * mRadius * 2;
                for (int k = 0; k < zWidth; ++k) {
                    float z = mRectMin.z + (k + 0.5f) * mRadius * 2;
                    int id = i * yWidth * zWidth + j * zWidth + k;
                    mPosition[id] = new Vector3(x, y, z) + transform.position;
                    mVelocity[id] = Vector3.zero;
                }
            }
        }
        
        mMaterial.SetFloat("scale", mRadius * 2);
    }

    void InitComputeShader() {
        mApplyForceKernel = mSolver.FindKernel("ApplyForce");
        mUpdatePositionAndVelocityKernel = mSolver.FindKernel("UpdatePositionAndVelocity");

        mPositionBuffer = new ComputeBuffer(mParticlesNumber, 12);
        mPredictionPositionBuffer = new ComputeBuffer(mParticlesNumber, 12);
        mVelocityBuffer = new ComputeBuffer(mParticlesNumber, 12);

        mPositionBuffer.SetData(mPosition);
        mPredictionPositionBuffer.SetData(mPredictionPosition);
        mVelocityBuffer.SetData(mVelocity);

        mSolver.SetBuffer(mApplyForceKernel, "gPosition", mPositionBuffer);
        mSolver.SetBuffer(mApplyForceKernel, "gPredictionPosition", mPredictionPositionBuffer);
        mSolver.SetBuffer(mApplyForceKernel, "gVelocity", mVelocityBuffer);

        mSolver.SetBuffer(mUpdatePositionAndVelocityKernel, "gPosition", mPositionBuffer);
        mSolver.SetBuffer(mUpdatePositionAndVelocityKernel, "gPredictionPosition", mPredictionPositionBuffer);
        mSolver.SetBuffer(mUpdatePositionAndVelocityKernel, "gVelocity", mVelocityBuffer);
    }

    private void Awake() {
        InitParticles();
        InitComputeShader();
    }

    private void Update() {
        mSolver.SetInt("gParticlesNumber", mParticlesNumber);

        // apply force
        {
            int blockSize = Mathf.CeilToInt(mParticlesNumber / 1024.0f);
            mSolver.SetInt("gBlockSize", blockSize);
            mSolver.Dispatch(mApplyForceKernel, blockSize, 1, 1);
        }

        // TODO: find neighbors
        // TODO: calculate lambda
        // TODO: collision detection
        // TODO: update position
        // TODO: apply viscocity
        // update position and velocity
        {
            int blockSize = Mathf.CeilToInt(mParticlesNumber / 1024.0f);
            mSolver.SetInt("gBlockSize", blockSize);
            mSolver.Dispatch(mUpdatePositionAndVelocityKernel, blockSize, 1, 1);
        }

        // render
        mMaterial.SetBuffer("positionBuffer", mPositionBuffer);
        Graphics.DrawMeshInstancedProcedural(mMesh, 0, mMaterial, mBounds, mParticlesNumber);
    }
}
