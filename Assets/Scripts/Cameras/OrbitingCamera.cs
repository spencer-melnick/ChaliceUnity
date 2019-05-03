using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitingCamera : MonoBehaviour
{
    public GameObject target;
    public Vector3 targetOffset = Vector3.zero;
    public Vector3 cameraOffset = new Vector3(0.0f, 0.0f, -10.0f);
    public Vector3 orbitAngles = new Vector3(40.0f, 0.0f, 0.0f);

    void Update()
    {
        Vector3 targetPosition = target.transform.position + this.targetOffset;
        Quaternion rotation = Quaternion.Euler(orbitAngles);
        Vector3 cameraPosition = targetPosition + rotation * this.cameraOffset;
        this.transform.SetPositionAndRotation(cameraPosition, rotation);
    }
}
