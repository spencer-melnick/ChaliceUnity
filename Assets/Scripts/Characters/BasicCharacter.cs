using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicCharacter : Character
{
    public float moveSpeed = 3.0f;
    public float moveAcceleration = 20.0f;
    public float rotationSpeedDegrees = 30.0f;

    CharacterController _characterController;
    Vector3 _currentVelocity;
    Vector3 _desiredVelocity;
    Quaternion _desiredRotation;
    bool _isLooking = false;

    public override void Move(Vector3 moveDirection)
    {
        _desiredVelocity = moveDirection * moveSpeed;
        _isLooking = false;
    }

    public override void MoveAndLook(Vector3 moveDirection, Quaternion lookRotation)
    {
        _desiredVelocity = moveDirection * moveSpeed;
        _desiredRotation = lookRotation;
        _isLooking = true;
    }

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleRotation();
    }

    void FixedUpdate()
    {
        HandleMove();
    }

    void HandleMove()
    {
        // Accelerate and move
        if (!_characterController.isGrounded)
        {
            // Apply gravity
            _currentVelocity.y -= 9.8f * Time.deltaTime;
        }
        else
        {
            // Only apply walking acceleration when on the ground
            _currentVelocity = Vector3.MoveTowards(_currentVelocity, _desiredVelocity, moveAcceleration * Time.deltaTime);
            _currentVelocity.y = 0.0f;
        }
        _characterController.Move(_currentVelocity * Time.deltaTime);
    }

    void HandleRotation()
    {
        // If character is moving, start rotation
        if (!Mathf.Approximately(_currentVelocity.x, 0.0f) || !Mathf.Approximately(_currentVelocity.z, 0.0f))
        {
            // If the character isn't looking in a specific direction and is moving...
            if (!_isLooking && !Mathf.Approximately(_desiredVelocity.magnitude, 0.0f))
            {
                // ...the character's desired look rotation is the direction they're moving
                // Ignore gravity when calculating look
                Vector3 desiredLook = _desiredVelocity;
                desiredLook.y = 0.0f;
                _desiredRotation = Quaternion.LookRotation(desiredLook.normalized, Vector3.up);
            }

            // Rotate towards desired look rotation
            Quaternion rotation = Quaternion.RotateTowards(transform.rotation, _desiredRotation, rotationSpeedDegrees * Time.deltaTime);
            transform.transform.rotation = (rotation);
        }
    }
}
