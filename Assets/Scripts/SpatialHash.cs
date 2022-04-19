using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialHash : MonoBehaviour {
    private Dictionary<Vector3Int, List<int>> mHash;

    private static SpatialHash gInstance = null;

    public SpatialHash Instance {
        get {
            return gInstance;
        }
    }

    private void Awake() {
        mHash = new Dictionary<Vector3Int, List<int>>();
        if (gInstance == null) {
            gInstance = this;
        }
    }

    public void AddParticle(int pIndex, Vector3Int pPosition) {
        Vector3Int hash = GetHasing(pPosition);
        if (!mHash.ContainsKey(hash)) {
            mHash.Add(hash, new List<int>());
        }

        mHash[hash].Add(pIndex);
    }

    public void RemoveParticle(int pIndex, Vector3Int pPosition) {
        Vector3Int hash = GetHasing(pPosition);
        if (mHash.ContainsKey(hash)) {
            mHash[hash].Remove(pIndex);
        }
    }

    private Vector3Int GetHasing(Vector3 pPosition) {
        return new Vector3Int(Mathf.FloorToInt(pPosition.x + 0.5f),
            Mathf.FloorToInt(pPosition.y + 0.5f),
            Mathf.FloorToInt(pPosition.z + 0.5f));
    }

    public List<int> GetNeighbors(int pIndex, Vector3Int pPosition) {
        List<int> neighbors = new List<int>();
        Vector3Int index = GetHasing(pPosition);

        for (int i = -1; i < 2; ++i) {
            for (int j = -1; j < 2; ++j) {
                for (int k = -1; k < 2; ++k) {
                    Vector3Int next = new Vector3Int(index.x + i, index.y + j, index.z + k);
                    if (mHash.ContainsKey(next)) {
                        foreach (int id in mHash[next]) {
                            if (id != pIndex) {
                                neighbors.Add(id);
                            }
                        }
                    }
                }
            }
        }

        return neighbors;
    }
}
