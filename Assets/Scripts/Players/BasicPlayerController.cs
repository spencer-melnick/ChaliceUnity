using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicPlayerController : MonoBehaviour
{

    public Character character;
    public GameObject cameraObject;
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
        float horizontalMovement = Input.GetAxisRaw("Horizontal");
        float verticalMovement = Input.GetAxisRaw("Vertical");
        Vector3 movement = new Vector3(horizontalMovement, 0.0f, verticalMovement);
        movement.Normalize();

        // TODO: Check to make sure camera is active!
        float horizontalRotation = Input.GetAxisRaw("Mouse X") * horizontalRotationSpeed * Time.deltaTime;;
        float verticalRotation = Input.GetAxisRaw("Mouse Y") * verticalRotationSpeed * Time.deltaTime;
        Vector3 cameraRotation = cameraObject.transform.eulerAngles;
        cameraRotation.y += horizontalRotation;
        cameraRotation.x -= verticalRotation;

        if (cameraRotation.x > 180.0f)
        {
            cameraRotation.x -= 360.0f;
        }

        cameraRotation.x = Mathf.Clamp(cameraRotation.x, minCameraPitch, maxCameraPitch);
        cameraObject.transform.SetPositionAndRotation(character.transform.position, Quaternion.Euler(cameraRotation));

        Quaternion cameraDirection = Camera.current.transform.rotation;
        Quaternion lookDirection = Quaternion.Euler(0.0f, cameraDirection.eulerAngles.y, 0.0f);
        movement = lookDirection * movement;
        character.Move(movement);
    }
}
