using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class OrbitingCamera : MonoBehaviour
{
    public GameObject target;
    public Vector3 targetOffset = Vector3.zero;
    public Vector3 cameraOffset = new Vector3(0.0f, 0.0f, -10.0f);
    public Vector3 orbitAngles = new Vector3(40.0f, 0.0f, 0.0f);

    public float pitchMax = 80.0f;
    public float pitchMin = -80.0f;

    void Update()
    {
        if (target != null)
        {
            // Modulo yaw angle to prevent large values
            orbitAngles.y %= 360.0f;

            // Wrap pitch angle for clamp
            if (orbitAngles.x > 180.0f)
            {
                orbitAngles.x -= 360.0f;
            }
            orbitAngles.x = Mathf.Clamp(orbitAngles.x, pitchMin, pitchMax);

            Quaternion rotation = Quaternion.identity;

            Character character = target.GetComponent<Character>();
            if (character != null)
            {
                rotation = Quaternion.FromToRotation(Vector3.up, character.upVector);
            }
            Vector3 targetPosition = target.transform.position + rotation * this.targetOffset;

            rotation *= Quaternion.Euler(orbitAngles);
            Vector3 cameraPosition = targetPosition + rotation * this.cameraOffset;
            this.transform.SetPositionAndRotation(cameraPosition, rotation);
        }
    }
}
