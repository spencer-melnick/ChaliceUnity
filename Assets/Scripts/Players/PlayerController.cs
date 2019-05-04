using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public Character controlledCharacter;
    public OrbitingCamera controlledCamera;

    public float horizontalRotationSpeed = 150.0f;
    public float verticalRotationSpeed = 200.0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update()
    {
        RotateCamera();
        MoveCharacter();
    }

    void RotateCamera()
    {
        // Check if the controlled camera is active
        if (controlledCamera != null && controlledCamera.GetComponentInParent<Camera>() == Camera.main)
        {
            // Scale mouse movement by delta time and appropriate speed
            float horizontalRotation = Input.GetAxisRaw("Mouse X") * horizontalRotationSpeed * Time.deltaTime; ;
            float verticalRotation = Input.GetAxisRaw("Mouse Y") * verticalRotationSpeed * Time.deltaTime;

            // Rotate camera orbit
            controlledCamera.orbitAngles.x -= verticalRotation;
            controlledCamera.orbitAngles.y += horizontalRotation;
        }
    }

    void MoveCharacter()
    {
        if (controlledCharacter != null)
        {
            // Normalize axis inputs along xz plane
            float horizontalMovement = Input.GetAxisRaw("Horizontal");
            float verticalMovement = Input.GetAxisRaw("Vertical");
            Vector3 movement = new Vector3(horizontalMovement, 0.0f, verticalMovement);
            movement.Normalize();

            // Move relatve to camera
            Quaternion cameraDirection = Camera.main.transform.rotation;
            Quaternion lookDirection = Quaternion.Euler(0.0f, cameraDirection.eulerAngles.y, 0.0f);
            movement = lookDirection * movement;
            controlledCharacter.MovePlanar(movement);
        }
    }
}
