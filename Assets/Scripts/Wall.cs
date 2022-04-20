using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour {
    public Vector3 mNormal;

    private void Awake() {
        mNormal.Normalize();
    }
}
