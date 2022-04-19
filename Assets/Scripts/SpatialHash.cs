using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialHash : MonoBehaviour {
    private Dictionary<Vector3Int, List<FluidParticle>> mHash;

    private static SpatialHash gInstance = null;

    public SpatialHash Instance {
        get {
            return gInstance;
        }
    }

    private void Awake() {
        mHash = new Dictionary<Vector3Int, List<FluidParticle>>();
        if (gInstance == null) {
            gInstance = this;
        }
    }

    public void AddParticle(FluidParticle pParticle) {
        Vector3Int hash = GetHasing(pParticle.transform.position);
        if (!mHash.ContainsKey(hash)) {
            mHash.Add(hash, new List<FluidParticle>());
        }

        mHash[hash].Add(pParticle);
    }

    public void RemoveParticle(FluidParticle pParticle) {
        Vector3Int hash = GetHasing(pParticle.transform.position);
        if (mHash.ContainsKey(hash)) {
            mHash[hash].Remove(pParticle);
        }
    }

    private Vector3Int GetHasing(Vector3 pPosition) {
        return new Vector3Int(Mathf.FloorToInt(pPosition.x + 0.5f),
            Mathf.FloorToInt(pPosition.y + 0.5f),
            Mathf.FloorToInt(pPosition.z + 0.5f));
    }

    public List<FluidParticle> GetNeighbors(FluidParticle pParticle) {
        List<FluidParticle> neighbors = new List<FluidParticle>();
        Vector3Int index = GetHasing(pParticle.transform.position);
        
        for (int i = -1; i < 2; ++i) {
            for (int j = -1; j < 2; ++j) {
                for (int k = -1; k < 2; ++k) {
                    Vector3Int next = new Vector3Int(index.x + i, index.y + j, index.z + k);
                    if (mHash.ContainsKey(next)) {
                        foreach (FluidParticle fp in mHash[next]) {
                            neighbors.Add(fp);
                        }
                    }
                }
            }
        }

        return neighbors;
    }
}
