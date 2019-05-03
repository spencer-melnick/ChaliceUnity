using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicCharacter : Character
{
    public float moveSpeed = 3.0f;
    public float moveAcceleration = 20.0f;
    public float rotationSpeedDegrees = 30.0f;

    Rigidbody _rigidbody;
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
        _rigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        HandleMove();
    }

    void HandleMove()
    {
        // Accelerate and move
        _currentVelocity = Vector3.MoveTowards(_currentVelocity, _desiredVelocity, moveAcceleration * Time.fixedDeltaTime);
        _rigidbody.MovePosition(transform.position + _currentVelocity * Time.fixedDeltaTime);

        // If character is moving, start rotation
        if (!Mathf.Approximately(_currentVelocity.magnitude, 0.0f))
        {
            // If the character isn't looking in a specific direction...
            if (!_isLooking)
            {
                // ...the character's desired look rotation is the direction they're moving
                _desiredRotation = Quaternion.LookRotation(_currentVelocity.normalized, Vector3.up);
            }

            // Rotate towards desired look rotation
            Quaternion rotation = Quaternion.RotateTowards(transform.rotation, _desiredRotation, rotationSpeedDegrees * Time.fixedDeltaTime);
            _rigidbody.MoveRotation(rotation);
        }
    }
}
