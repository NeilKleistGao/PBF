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

    [SerializeField] private Vector3 mGridMin;
    [SerializeField] private Vector3 mGridMax;

    [SerializeField] private float mKernelRange;

    [SerializeField] private float mDensity;

    private int mParticlesNumber;

    private Vector3[] mPosition;
    private Vector3[] mPredictionPosition;
    private Vector3[] mVelocity;
    private Vector3[] mWallsNormal;
    private Vector3[] mWallsPosition;

    private int[] mNeighbors;
    private float[] mLambda;

    private static readonly int MAX_NEIGHBORS = 50;

    private Bounds mBounds;

    private ComputeBuffer mPositionBuffer;
    private ComputeBuffer mPredictionPositionBuffer;
    private ComputeBuffer mVelocityBuffer;
    private ComputeBuffer mWallsNormalBuffer;
    private ComputeBuffer mWallsPositionBuffer;
    private ComputeBuffer mNeighborsBuffer;
    private ComputeBuffer mLambdaBuffer;

    private int mApplyForceKernel;
    private int mUpdatePositionAndVelocityKernel;
    private int mHandleCollisionKernel;
    private int mGetNeighborsKernel;
    private int mCalculateLambdaKernel;
    private int mUpdatePositionKernel;

    private void InitParticles() {
        int xWidth = Mathf.FloorToInt((mRectMax.x - mRectMin.x) * transform.localScale.x / mRadius / 2);
        int yWidth = Mathf.FloorToInt((mRectMax.y - mRectMin.y) * transform.localScale.y / mRadius / 2);
        int zWidth = Mathf.FloorToInt((mRectMax.z - mRectMin.z) * transform.localScale.z / mRadius / 2);

        mParticlesNumber = xWidth * yWidth * zWidth;
        mPosition = new Vector3[mParticlesNumber];
        mVelocity = new Vector3[mParticlesNumber];
        mPredictionPosition = new Vector3[mParticlesNumber];
        mNeighbors = new int[mParticlesNumber * (MAX_NEIGHBORS + 1)];
        mLambda = new float[mParticlesNumber];

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
        mApplyForceKernel = mSolver.FindKernel("ApplyForce");
        mUpdatePositionAndVelocityKernel = mSolver.FindKernel("UpdatePositionAndVelocity");
        mHandleCollisionKernel = mSolver.FindKernel("HandleCollision");
        mGetNeighborsKernel = mSolver.FindKernel("GetNeighbors");
        mCalculateLambdaKernel = mSolver.FindKernel("CalculateLambda");
        mUpdatePositionKernel = mSolver.FindKernel("UpdatePosition");

        mPositionBuffer = new ComputeBuffer(mParticlesNumber, 12);
        mPredictionPositionBuffer = new ComputeBuffer(mParticlesNumber, 12);
        mVelocityBuffer = new ComputeBuffer(mParticlesNumber, 12);
        mWallsNormalBuffer = new ComputeBuffer(mWalls.Length, 12);
        mWallsPositionBuffer = new ComputeBuffer(mWalls.Length, 12);
        mNeighborsBuffer = new ComputeBuffer(mParticlesNumber * (MAX_NEIGHBORS + 1), 4);
        mLambdaBuffer = new ComputeBuffer(mParticlesNumber, 4);

        mPositionBuffer.SetData(mPosition);
        mPredictionPositionBuffer.SetData(mPredictionPosition);
        mVelocityBuffer.SetData(mVelocity);
        mWallsNormalBuffer.SetData(mWallsNormal);
        mWallsPositionBuffer.SetData(mWallsPosition);
        mNeighborsBuffer.SetData(mNeighbors);
        mLambdaBuffer.SetData(mLambda);

        mSolver.SetBuffer(mApplyForceKernel, "gPosition", mPositionBuffer);
        mSolver.SetBuffer(mApplyForceKernel, "gPredictionPosition", mPredictionPositionBuffer);
        mSolver.SetBuffer(mApplyForceKernel, "gVelocity", mVelocityBuffer);

        mSolver.SetBuffer(mUpdatePositionAndVelocityKernel, "gPosition", mPositionBuffer);
        mSolver.SetBuffer(mUpdatePositionAndVelocityKernel, "gPredictionPosition", mPredictionPositionBuffer);
        mSolver.SetBuffer(mUpdatePositionAndVelocityKernel, "gVelocity", mVelocityBuffer);

        mSolver.SetBuffer(mHandleCollisionKernel, "gPredictionPosition", mPredictionPositionBuffer);
        mSolver.SetBuffer(mHandleCollisionKernel, "gVelocity", mVelocityBuffer);
        mSolver.SetBuffer(mHandleCollisionKernel, "gWallsNormal", mWallsNormalBuffer);
        mSolver.SetBuffer(mHandleCollisionKernel, "gWallsPosition", mWallsPositionBuffer);

        mSolver.SetBuffer(mGetNeighborsKernel, "gPredictionPosition", mPredictionPositionBuffer);
        mSolver.SetBuffer(mGetNeighborsKernel, "gNeighbors", mNeighborsBuffer);

        mSolver.SetBuffer(mCalculateLambdaKernel, "gPredictionPosition", mPredictionPositionBuffer);
        mSolver.SetBuffer(mCalculateLambdaKernel, "gNeighbors", mNeighborsBuffer);
        mSolver.SetBuffer(mCalculateLambdaKernel, "gLambda", mLambdaBuffer);

        mSolver.SetBuffer(mUpdatePositionKernel, "gPredictionPosition", mPredictionPositionBuffer);
        mSolver.SetBuffer(mUpdatePositionKernel, "gNeighbors", mNeighborsBuffer);
        mSolver.SetBuffer(mUpdatePositionKernel, "gLambda", mLambdaBuffer);
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

        // apply force
        {
            int blockSize = Mathf.CeilToInt(mParticlesNumber / 1024.0f);
            mSolver.SetInt("gBlockSize", blockSize);
            mSolver.Dispatch(mApplyForceKernel, blockSize, 1, 1);
        }

        // find neighbors
        {
            int blockSize = Mathf.CeilToInt(mParticlesNumber / 1024.0f);
            mSolver.SetInt("gMaxNeighborsCount", MAX_NEIGHBORS + 1);
            mSolver.SetInt("gBlockSize", blockSize);
            mSolver.Dispatch(mGetNeighborsKernel, blockSize, 1, 1);
        }

        for (int i = 0; i < 1; ++i) {
            // calculate lambda
            {
                int blockSize = Mathf.CeilToInt(mParticlesNumber / 1024.0f);
                mSolver.SetInt("gBlockSize", blockSize);
                mSolver.Dispatch(mCalculateLambdaKernel, blockSize, 1, 1);
                // mLambdaBuffer.GetData(mLambda);
                // foreach (float lmd in mLambda) {
                //     Debug.Log(lmd);
                // }
            }

            // collision detection
            {
                int blockSize = Mathf.CeilToInt(mParticlesNumber / 1024.0f);
                mSolver.SetInt("gBlockSize", blockSize);
                mSolver.Dispatch(mHandleCollisionKernel, blockSize, 1, 1);
            }

            // update position
            {
                int blockSize = Mathf.CeilToInt(mParticlesNumber / 1024.0f);
                mSolver.SetInt("gBlockSize", blockSize);
                mSolver.Dispatch(mUpdatePositionKernel, blockSize, 1, 1);
                // mPredictionPositionBuffer.GetData(mPredictionPosition);
                // foreach (var fuck in mPredictionPosition) {
                //     Debug.Log(fuck);
                // }
            }
        }

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
