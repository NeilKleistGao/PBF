using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour {
    [SerializeField] private Vector3 mNormal;

    private void Awake() {
        mNormal.Normalize();
    }

    public void Collide(ref Vector3 pPosition, ref Vector3 pVelocity) {
        float sdf = Vector3.Dot(mNormal, pPosition - transform.position);
        if (sdf >= 0) {
            return;
        }

        pVelocity -= (sdf * mNormal) / Time.fixedDeltaTime;
        pPosition -= sdf * mNormal;
    }
}
