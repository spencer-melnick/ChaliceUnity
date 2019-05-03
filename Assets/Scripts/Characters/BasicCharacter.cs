using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicCharacter : Character
{
    public float moveSpeed = 3.0f;
    public float moveAcceleration = 20.0f;
    public float rotationSpeedDegrees = 30.0f;

    public float gravitationalAcceleration = 9.8f;
    public float slopeLimitDegrees = 45.0f;

    // If a slope is closer this distance, the player will snap to it while walking down
    public float slopeDropLimit = 0.1f;

    // Any raycast speculated collisions will be prevented by this distance
    public float speculativeCollisionResolutionDistance = 0.01f;

    CapsuleCollider _capsuleCollider;
    Rigidbody _rigidBody;

    Vector3 _position;
    Vector3 _velocity;
    Vector3 _desiredMovement;
    Quaternion _desiredRotation;
    bool _isLooking = false;
    bool _isGrounded = false;

    // Moves and looks in the direction of the movement
    public override void Move(Vector3 moveDirection)
    {
        _desiredMovement = moveDirection * moveSpeed;
        _isLooking = false;
    }

    // Moves and looks in the specified direction
    public override void MoveAndLook(Vector3 moveDirection, Quaternion lookRotation)
    {
        _desiredMovement = moveDirection * moveSpeed;
        _desiredRotation = lookRotation;
        _isLooking = true;
    }

    void Start()
    {
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _rigidBody = GetComponent<Rigidbody>();

        // Make sure that the rigid body is kinematic!
        _rigidBody.isKinematic = true;
        _position = transform.position;
    }

    private void Update()
    {
        HandleRotation();
    }

    void FixedUpdate()
    {
        if (!_isGrounded)
        {
            TrySnapToSlope();
        }

        HandleAcceleration();
        MoveSpeculatively(_velocity * Time.fixedDeltaTime, 3);
        _rigidBody.MovePosition(_position);
    }

    private void OnGrounded()
    {
        _isGrounded = true;

        // Reset gravity on ground collision to allow for proper slope walking
        _velocity.y = 0.0f;
    }

    private void OnHit(RaycastHit hit)
    {
        float slope = Vector3.Angle(hit.normal, Vector3.up);
        
        if (slope < slopeLimitDegrees)
        {
            OnGrounded();
        }
    }

    bool CastFromOwnCapsule(Vector3 ray, out RaycastHit hit)
    {
        // Compute capsule sphere locations
        Vector3 capsuleTop = transform.position + _capsuleCollider.center + Vector3.up * (_capsuleCollider.height / 2 - _capsuleCollider.radius);
        Vector3 capsuleBottom = transform.position + _capsuleCollider.center + Vector3.down * (_capsuleCollider.height / 2 - _capsuleCollider.radius);
        Vector3 direction = ray.normalized;

        return Physics.CapsuleCast(capsuleTop, capsuleBottom, _capsuleCollider.radius, direction, out hit, ray.magnitude);
    }

    // Attempts to snap the player to a slope if they walk off of it
    void TrySnapToSlope()
    {
        if (CastFromOwnCapsule(Vector3.down * slopeDropLimit, out RaycastHit hit))
        {
            float slope = Vector3.Angle(hit.normal, Vector3.up);

            if (slope < slopeLimitDegrees)
            {
                _position += Vector3.down * Mathf.Max(hit.distance - speculativeCollisionResolutionDistance, 0.0f);
                OnGrounded();
            }
        }
    }

    // Handles acceleration based on desired movement and gravity
    void HandleAcceleration()
    {
        // Accelerate and move
        if (!_isGrounded)
        {
            // Apply gravity
            _velocity.y -= 9.8f * Time.deltaTime;
        }
        else
        {
            // Only apply walking acceleration when on the ground

            // Split movement into along ground plane and gravity
            Vector3 currentMovement = Vector3.ProjectOnPlane(_velocity, Vector3.up);
            // Vector3 currentGravitationalMovement = _velocity - currentMovement;
            Vector3 currentGravitationalMovement = Vector3.zero;

            // Accelerate only movement along ground plane
            currentMovement = Vector3.MoveTowards(currentMovement, _desiredMovement, moveAcceleration * Time.deltaTime);
            _velocity = currentMovement + currentGravitationalMovement;
        }

        _isGrounded = false;
    }

    void HandleRotation()
    {
        // If character is moving, start rotation
        if (!Mathf.Approximately(_velocity.x, 0.0f) || !Mathf.Approximately(_velocity.z, 0.0f))
        {
            // If the character isn't looking in a specific direction and is moving...
            if (!_isLooking && !Mathf.Approximately(_desiredMovement.magnitude, 0.0f))
            {
                // ...the character's desired look rotation is the direction they're moving
                // Ignore gravity when calculating look
                Vector3 desiredLook = _desiredMovement;
                desiredLook.y = 0.0f;
                _desiredRotation = Quaternion.LookRotation(desiredLook.normalized, Vector3.up);
            }

            // Rotate towards desired look rotation
            Quaternion rotation = Quaternion.RotateTowards(transform.rotation, _desiredRotation, rotationSpeedDegrees * Time.deltaTime);
            transform.rotation = rotation;
        }
    }

    // Moves the kinematic body by the desired offest, raycasting to prevent collisions
    // Will operate recursively, breaking up movement into steps to check for further collisions
    void MoveSpeculatively(Vector3 movement, uint steps)
    {
        // Check for base case
        if (steps != 0)
        {
            if (CastFromOwnCapsule(movement, out RaycastHit hit))
            {
                // Handle hit
                OnHit(hit);

                // Update position by maximum allowed motion
                float movementDistance = Mathf.Max(0.0f, hit.distance - speculativeCollisionResolutionDistance);
                Vector3 allowedMotion = movementDistance * movement.normalized;
                _position += allowedMotion;

                // Attempt to slide along plane for remaining motion
                Vector3 remainingMotion = movement - allowedMotion;
                Vector3 slidingMotion = Vector3.ProjectOnPlane(remainingMotion, hit.normal);
                MoveSpeculatively(slidingMotion, steps - 1);
            }
            else
            {
                _position += movement;
            }
        }
    }
}
