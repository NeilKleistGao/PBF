using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fluid : MonoBehaviour {
    [SerializeField] private Mesh mMesh;

    [SerializeField] private Material mMaterial;

    [SerializeField] private float mRadius = 0.1f;

    [SerializeField] private Vector3 mRectMin;

    [SerializeField] private Vector3 mRectMax;

    [SerializeField] private float mDensity = 1.0f;

    [SerializeField] private Wall[] mWalls;

    private int mParticlesNumber;
    private Vector3[] mPosition;
    private Vector3[] mPredictPosition;
    private Vector3[] mVelocity;

    private List<int>[] mNeighbors;

    private Bounds mBounds;

    private ComputeBuffer mPositionBuffer;

    private static readonly int ITERATOR_TIMES = 4;

    private void Awake() {
        int xWidth = Mathf.FloorToInt((mRectMax.x - mRectMin.x) * transform.localScale.x / mRadius / 2);
        int yWidth = Mathf.FloorToInt((mRectMax.y - mRectMin.y) * transform.localScale.y / mRadius / 2);
        int zWidth = Mathf.FloorToInt((mRectMax.z - mRectMin.z) * transform.localScale.z / mRadius / 2);

        mParticlesNumber = xWidth * yWidth * zWidth;
        mPosition = new Vector3[mParticlesNumber];
        mVelocity = new Vector3[mParticlesNumber];
        mPredictPosition = new Vector3[mParticlesNumber];
        mNeighbors = new List<int>[mParticlesNumber];

        Debug.Log(mParticlesNumber);
        mBounds = new Bounds(transform.position, transform.localScale);
        mPositionBuffer = new ComputeBuffer(mParticlesNumber, 3 * 4);

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
        
        mPositionBuffer.SetData(mPosition);
        mMaterial.SetBuffer("positionBuffer", mPositionBuffer);
        mMaterial.SetFloat("scale", mRadius * 2);
    }

    private void Update() {
        // apply forces
        Vector3 force = new Vector3(0, -9.8f, 0);
        for (int i = 0; i < mParticlesNumber; ++i) {
            mVelocity[i] += Time.fixedDeltaTime * force;
            mPredictPosition[i] = mPosition[i] + Time.fixedDeltaTime * mVelocity[i];
            SpatialHash.Instance.AddParticle(i, mPredictPosition[i]);
        }

        // find neighbors
        for (int i = 0; i < mParticlesNumber; ++i) {
            mNeighbors[i] = SpatialHash.Instance.GetNeighbors(i, mPredictPosition[i]);
        }

        for (int i = 0; i < mParticlesNumber; ++i) {
            SpatialHash.Instance.RemoveParticle(i, mPredictPosition[i]);
        }

        for (int i = 0; i < ITERATOR_TIMES; ++i) {
            // TODO: calculate lambda
            // TODO: calculate delta p
            for (int j = 0; j < mParticlesNumber; ++j) {
                foreach (Wall wall in mWalls) {
                    wall.Collide(ref mPredictPosition[j], ref mVelocity[j]);
                }
            }
        }

        for (int i = 0; i < mParticlesNumber; ++i) {
            // TODO: update velocity
            // TODO: apply vorticity
            mPosition[i] = mPredictPosition[i];
        }

        mPositionBuffer.SetData(mPosition);
        mMaterial.SetBuffer("positionBuffer", mPositionBuffer);
        Graphics.DrawMeshInstancedProcedural(mMesh, 0, mMaterial, mBounds, mParticlesNumber);
    }
}
