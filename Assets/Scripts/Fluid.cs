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

    [SerializeField] private Wall[] mWalls;

    [SerializeField] private float mKernelRange;

    [SerializeField] private float mDensity;

    private int mParticlesNumber;

    private Vector3[] mPosition;
    private Vector3[] mPredictionPosition;
    private Vector3[] mVelocity;
    private Vector3[] mWallsNormal;
    private Vector3[] mWallsPosition;

    private static readonly int MAX_NEIGHBORS = 50;

    private Bounds mBounds;

    private ComputeBuffer mPositionBuffer;
    private ComputeBuffer mPredictionPositionBuffer;
    private ComputeBuffer mVelocityBuffer;
    private ComputeBuffer mWallsNormalBuffer;
    private ComputeBuffer mWallsPositionBuffer;
    private ComputeBuffer mNeighborsBuffer;
    private ComputeBuffer mNeighborCountBuffer;
    private ComputeBuffer mLambdaBuffer;

    private int mUpdateKernel;

    private void InitParticles() {
        int xWidth = 20;
        int yWidth = 20;
        int zWidth = 10;

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

    void InitWalls() {
        mWallsNormal = new Vector3[mWalls.Length];
        mWallsPosition = new Vector3[mWalls.Length];

        for (int i = 0; i < mWalls.Length; ++i) {
            mWallsNormal[i] = mWalls[i].mNormal;
            mWallsPosition[i] = mWalls[i].transform.position;
        }
    }

    void InitComputeShader() {
        mUpdateKernel = mSolver.FindKernel("Update");

        mPositionBuffer = new ComputeBuffer(mParticlesNumber, 12);
        mPredictionPositionBuffer = new ComputeBuffer(mParticlesNumber, 12);
        mVelocityBuffer = new ComputeBuffer(mParticlesNumber, 12);
        mWallsNormalBuffer = new ComputeBuffer(mWalls.Length, 12);
        mWallsPositionBuffer = new ComputeBuffer(mWalls.Length, 12);
        mNeighborsBuffer = new ComputeBuffer(mParticlesNumber, 4 * MAX_NEIGHBORS);
        mNeighborCountBuffer = new ComputeBuffer(mParticlesNumber, 4);
        mLambdaBuffer = new ComputeBuffer(mParticlesNumber, 4);

        mPositionBuffer.SetData(mPosition);
        mPredictionPositionBuffer.SetData(mPredictionPosition);
        mVelocityBuffer.SetData(mVelocity);
        mWallsNormalBuffer.SetData(mWallsNormal);
        mWallsPositionBuffer.SetData(mWallsPosition);

        mSolver.SetBuffer(mUpdateKernel, "gPosition", mPositionBuffer);
        mSolver.SetBuffer(mUpdateKernel, "gPredictionPosition", mPredictionPositionBuffer);
        mSolver.SetBuffer(mUpdateKernel, "gVelocity", mVelocityBuffer);
        mSolver.SetBuffer(mUpdateKernel, "gWallsNormal", mWallsNormalBuffer);
        mSolver.SetBuffer(mUpdateKernel, "gWallsPosition", mWallsPositionBuffer);
        mSolver.SetBuffer(mUpdateKernel, "gNeighbors", mNeighborsBuffer);
        mSolver.SetBuffer(mUpdateKernel, "gNeighborCount", mNeighborCountBuffer);
        mSolver.SetBuffer(mUpdateKernel, "gLambda", mLambdaBuffer);
    }

    private void Awake() {
        InitParticles();
        InitWalls();
        InitComputeShader();
    }

    private void Update() {
        mSolver.SetInt("gParticlesNumber", mParticlesNumber);
        mSolver.SetInt("gWallSize", mWalls.Length);
        mSolver.SetFloat("gRadius", mRadius);
        mSolver.SetFloat("gKernelRange", mKernelRange);
        mSolver.SetFloat("gDensity", mDensity);
        mSolver.SetInt("gMaxNeighborsCount", MAX_NEIGHBORS);

        int blockSize = Mathf.CeilToInt(mParticlesNumber / 1024.0f);
        mSolver.SetInt("gBlockSize", blockSize);
        mSolver.Dispatch(mUpdateKernel, blockSize, 1, 1);

        // render
        mMaterial.SetBuffer("positionBuffer", mPositionBuffer);
        Graphics.DrawMeshInstancedProcedural(mMesh, 0, mMaterial, mBounds, mParticlesNumber);
    }
}
