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

    private int mParticlesNumber;

    private int mGridsNumber;

    private Vector3[] mPosition;
    private Vector3[] mPredictionPosition;
    private Vector3[] mVelocity;
    private Vector3[] mWallsNormal;
    private Vector3[] mWallsPosition;

    private int[] mGrid;
    private int[] mGridCounter;
    private int[] mGridEnd;
    private int[] mNeighbors;

    private int mGridX, mGridY, mGridZ;
    private static readonly int MAX_NEIGHBORS = 50;

    private Bounds mBounds;

    private ComputeBuffer mPositionBuffer;

    private ComputeBuffer mPredictionPositionBuffer;
    private ComputeBuffer mVelocityBuffer;
    private ComputeBuffer mWallsNormalBuffer;
    private ComputeBuffer mWallsPositionBuffer;

    private ComputeBuffer mGridBuffer;
    private ComputeBuffer mGridCounterBuffer;
    private ComputeBuffer mGridEndBuffer;
    private ComputeBuffer mNeighborsBuffer;

    private int mApplyForceKernel;
    private int mUpdatePositionAndVelocityKernel;
    private int mHandleCollisionKernel;
    private int mInsertParticlesIntoGridKernel;

    private int mGetNeighborsKernel;

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

    void InitWalls() {
        mWallsNormal = new Vector3[mWalls.Length];
        mWallsPosition = new Vector3[mWalls.Length];

        for (int i = 0; i < mWalls.Length; ++i) {
            mWallsNormal[i] = mWalls[i].mNormal;
            mWallsPosition[i] = mWalls[i].transform.position;
        }
    }

    void InitGrid() {
        mGridX = Mathf.CeilToInt((mGridMax.x - mGridMin.x) / mKernelRange);
        mGridY = Mathf.CeilToInt((mGridMax.y - mGridMin.y) / mKernelRange);
        mGridZ = Mathf.CeilToInt((mGridMax.z - mGridMin.z) / mKernelRange);

        mGridsNumber = mGridX * mGridY * mGridZ;
        mGrid = new int[mParticlesNumber];
        mGridCounter = new int[mGridsNumber];
        mGridEnd = new int[mGridsNumber];

        mNeighbors = new int[mParticlesNumber * (MAX_NEIGHBORS + 1)];
        Debug.Log("Grids Number: " + mGridsNumber);
        Debug.LogFormat("{0} x {1} x {2}", mGridX, mGridY, mGridZ);
    }

    void InitComputeShader() {
        mApplyForceKernel = mSolver.FindKernel("ApplyForce");
        mUpdatePositionAndVelocityKernel = mSolver.FindKernel("UpdatePositionAndVelocity");
        mHandleCollisionKernel = mSolver.FindKernel("HandleCollision");
        mInsertParticlesIntoGridKernel = mSolver.FindKernel("InsertParticlesIntoGrid");
        mGetNeighborsKernel = mSolver.FindKernel("GetNeighbors");

        mPositionBuffer = new ComputeBuffer(mParticlesNumber, 12);
        mPredictionPositionBuffer = new ComputeBuffer(mParticlesNumber, 12);
        mVelocityBuffer = new ComputeBuffer(mParticlesNumber, 12);
        mWallsNormalBuffer = new ComputeBuffer(mWalls.Length, 12);
        mWallsPositionBuffer = new ComputeBuffer(mWalls.Length, 12);
        mGridBuffer = new ComputeBuffer(mParticlesNumber, 4);
        mGridCounterBuffer = new ComputeBuffer(mGridsNumber, 4);
        mGridEndBuffer = new ComputeBuffer(mGridsNumber, 4);
        mNeighborsBuffer = new ComputeBuffer(mParticlesNumber * (MAX_NEIGHBORS + 1), 4);

        mPositionBuffer.SetData(mPosition);
        mPredictionPositionBuffer.SetData(mPredictionPosition);
        mVelocityBuffer.SetData(mVelocity);
        mWallsNormalBuffer.SetData(mWallsNormal);
        mWallsPositionBuffer.SetData(mWallsPosition);
        mGridBuffer.SetData(mGrid);
        mGridCounterBuffer.SetData(mGridCounter);
        mGridEndBuffer.SetData(mGridEnd);
        mNeighborsBuffer.SetData(mNeighbors);

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

        mSolver.SetBuffer(mInsertParticlesIntoGridKernel, "gPredictionPosition", mPredictionPositionBuffer);
        mSolver.SetBuffer(mInsertParticlesIntoGridKernel, "gGrid", mGridBuffer);
        mSolver.SetBuffer(mInsertParticlesIntoGridKernel, "gGridCounter", mGridCounterBuffer);
        mSolver.SetBuffer(mInsertParticlesIntoGridKernel, "gGridEnd", mGridEndBuffer);

        mSolver.SetBuffer(mGetNeighborsKernel, "gPredictionPosition", mPredictionPositionBuffer);
        mSolver.SetBuffer(mGetNeighborsKernel, "gGrid", mGridBuffer);
        mSolver.SetBuffer(mGetNeighborsKernel, "gGridEnd", mGridEndBuffer);
        mSolver.SetBuffer(mGetNeighborsKernel, "gNeighbors", mNeighborsBuffer);
    }

    private void Awake() {
        InitParticles();
        InitWalls();
        InitGrid();
        InitComputeShader();
    }

    private void Update() {
        mSolver.SetInt("gParticlesNumber", mParticlesNumber);
        mSolver.SetInt("gWallSize", mWalls.Length);
        mSolver.SetFloat("gRadius", mRadius);
        mSolver.SetFloat("gKernelRange", mKernelRange);

        // apply force
        {
            int blockSize = Mathf.CeilToInt(mParticlesNumber / 1024.0f);
            mSolver.SetInt("gBlockSize", blockSize);
            mSolver.Dispatch(mApplyForceKernel, blockSize, 1, 1);
        }

        // find neighbors
        {
            // mSolver.SetInt("gGirdX", mGridX);
            // mSolver.SetInt("gGirdY", mGridY);
            // mSolver.SetInt("gGirdZ", mGridZ);
            // mSolver.SetInt("gGridSize", mGridsNumber);
            // mSolver.SetVector("gGridOrigin", mGridMin);
            // mSolver.SetInt("gMaxNeighborsCount", MAX_NEIGHBORS + 1);

            // mSolver.Dispatch(mInsertParticlesIntoGridKernel, 1, 1, 1);
            // mGridEndBuffer.GetData(mGridEnd);
            // for (int i = 0; i < mGridEnd.Length; ++i) {
            //     Debug.LogFormat("{0}: {1}", i, mGridEnd[i]);
            // }

            int blockSize = Mathf.CeilToInt(mParticlesNumber / 1024.0f);
            mSolver.SetInt("gMaxNeighborsCount", MAX_NEIGHBORS + 1);
            mSolver.SetInt("gBlockSize", blockSize);
            mSolver.Dispatch(mGetNeighborsKernel, blockSize, 1, 1);
        }

        // TODO: calculate lambda
        // collision detection
        {
            int blockSize = Mathf.CeilToInt(mParticlesNumber / 1024.0f);
            mSolver.SetInt("gBlockSize", blockSize);
            mSolver.Dispatch(mHandleCollisionKernel, blockSize, 1, 1);
        }

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
