using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicPlayerController : MonoBehaviour
{

    public Character controlledCharacter;
    public OrbitingCamera controlledCamera;

    public float horizontalRotationSpeed = 150.0f;
    public float verticalRotationSpeed = 200.0f;
    public float maxCameraPitch = 80.0f;
    public float minCameraPitch = -80.0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update()
    {
        RotateCamera();
        MoveCharacter();
    }

    private void LateUpdate()
    {
        // TODO: Resolve collisions
    }

    void RotateCamera()
    {
        // Check if the controlled camera is active
        if (controlledCamera != null && controlledCamera.GetComponentInParent<Camera>() == Camera.main)
        {
            // Scale mouse movement by delta time and appropriate speed
            float horizontalRotation = Input.GetAxisRaw("Mouse X") * horizontalRotationSpeed * Time.deltaTime; ;
            float verticalRotation = Input.GetAxisRaw("Mouse Y") * verticalRotationSpeed * Time.deltaTime;

            // Operate along Euler angles
            Vector3 cameraAngles = controlledCamera.orbitAngles;
            cameraAngles.y += horizontalRotation;
            cameraAngles.x -= verticalRotation;

            // Check for angle wrap-around to allow for proper clamping
            if (cameraAngles.x > 180.0f)
            {
                cameraAngles.x -= 360.0f;
            }

            // Clamp angles to limits
            cameraAngles.y %= 360.0f;
            cameraAngles.x = Mathf.Clamp(cameraAngles.x, minCameraPitch, maxCameraPitch);
            controlledCamera.orbitAngles = cameraAngles;
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
            controlledCharacter.Move(movement);
        }
    }
}
