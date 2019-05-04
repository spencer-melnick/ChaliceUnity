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

    public bool penetrationResolution = true;
    public uint maxPenetrationsPerStep = 10;

    CapsuleCollider _capsuleCollider;
    Rigidbody _rigidBody;

    Vector3 _position;
    Vector3 _velocity;
    Vector3 _desiredMovement;
    Quaternion _desiredRotation;

    bool _isLooking = false;
    bool _isGrounded = false;
    Vector3 _groundNormal = Vector3.up;

    Collider[] _penetratingColliders;

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

        // Allocate collision buffer
        _penetratingColliders = new Collider[maxPenetrationsPerStep];
    }

    void FixedUpdate()
    {
        HandleRotation();
        HandleAcceleration();
        // TrySnapToSlope();

        Debug.DrawLine(_position + Vector3.up, _position + Vector3.up + _velocity, Color.blue, Time.fixedDeltaTime);

        _isGrounded = false;
        _groundNormal = Vector3.up;
        MoveSpeculatively(_velocity * Time.fixedDeltaTime, 3);

        if (penetrationResolution)
        {
            ResolvePenetrations();
        }

        Debug.DrawLine(_position, _position + _velocity, Color.red, Time.fixedDeltaTime);
        _rigidBody.MovePosition(_position);
    }

    private void OnGrounded(Vector3 groundNormal)
    {
        _groundNormal = groundNormal;
        _isGrounded = true;

        // Reset gravity on ground collision to allow for proper slope walking
        // _velocity.y = 0.0f;

        Debug.DrawLine(_position, _position + groundNormal, Color.yellow, Time.fixedDeltaTime);
    }

    private void OnHit(RaycastHit hit)
    {
        float slope = Vector3.Angle(hit.normal, Vector3.up);
        
        if (slope < slopeLimitDegrees)
        {
            OnGrounded(hit.normal);
        }
    }

    void CalculateOwnCapsuleParameters(out Vector3 capsuleTop, out Vector3 capsuleBottom, out float radius)
    {
        capsuleTop = transform.position + _capsuleCollider.center + Vector3.up * (_capsuleCollider.height / 2 - _capsuleCollider.radius);
        capsuleBottom = transform.position + _capsuleCollider.center + Vector3.down * (_capsuleCollider.height / 2 - _capsuleCollider.radius);
        radius = _capsuleCollider.radius;
    }

    bool CastFromOwnCapsule(Vector3 ray, out RaycastHit hit)
    {
        CalculateOwnCapsuleParameters(out Vector3 capsuleTop, out Vector3 capsuleBottom, out float radius);
        Vector3 direction = ray.normalized;
        return Physics.CapsuleCast(capsuleTop, capsuleBottom, radius, direction, out hit, ray.magnitude);
    }

    // Attempts to snap the player to a slope if they walk off of it
    void TrySnapToSlope()
    {
        if (!_isGrounded && CastFromOwnCapsule(Vector3.down * slopeDropLimit, out RaycastHit hit))
        {
            float slope = Vector3.Angle(hit.normal, Vector3.up);

            if (slope < slopeLimitDegrees)
            {
                float movementDistance = Mathf.Max(hit.distance - speculativeCollisionResolutionDistance, 0.0f);
                _position += Vector3.down * Mathf.Max(hit.distance - speculativeCollisionResolutionDistance, 0.0f);
                _velocity.y = -movementDistance / Time.fixedDeltaTime;

                OnGrounded(hit.normal);
            }
        }
    }

    // Handles acceleration based on desired movement and gravity
    void HandleAcceleration()
    {
        // Apply gravity
        _velocity.y -= gravitationalAcceleration * Time.deltaTime;

        if (_isGrounded)
        {
            // Only apply walking acceleration when on the ground

            // Split movement into along ground plane and gravity
            Quaternion groundRotation = Quaternion.FromToRotation(Vector3.up, _groundNormal);
            Vector3 currentMovement = Vector3.ProjectOnPlane(_velocity, _groundNormal);
            Vector3 currentGravitationalMovement = _velocity - currentMovement;

            // Accelerate only movement along ground plane
            currentMovement = Vector3.MoveTowards(currentMovement, groundRotation * _desiredMovement, moveAcceleration * Time.deltaTime);
            //currentMovement = _desiredMovement;
            _velocity = currentMovement + currentGravitationalMovement;
        }
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
            Quaternion rotation = Quaternion.RotateTowards(transform.rotation, _desiredRotation, rotationSpeedDegrees * Time.fixedDeltaTime);
            _rigidBody.MoveRotation(rotation);
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

                _velocity = slidingMotion / Time.fixedDeltaTime;
                MoveSpeculatively(slidingMotion, steps - 1);
            }
            else
            {
                _position += movement;
            }
        }
    }

    void ResolvePenetrations()
    {
        // Check for possible penetrations
        CalculateOwnCapsuleParameters(out Vector3 capsuleTop, out Vector3 capsuleBottom, out float radius);
        int penetrations = Physics.OverlapCapsuleNonAlloc(capsuleTop, capsuleBottom, radius, _penetratingColliders);

        if (penetrations > 0)
        {
            for (int i = 0; i < penetrations; i++)
            {
                Collider penetratingCollider = _penetratingColliders[i];

                // Don't collide with self, and double check using penetration calculaton
                if (penetratingCollider != _capsuleCollider &&
                    Physics.ComputePenetration(_capsuleCollider, _position, _rigidBody.rotation,
                        penetratingCollider, penetratingCollider.transform.position, penetratingCollider.transform.rotation,
                        out Vector3 direction, out float distance))
                {
                    // Todo: add trigger on penetration resolution
                    _position += direction * (distance + speculativeCollisionResolutionDistance);
                    _velocity = Vector3.ProjectOnPlane(_velocity, direction);
                }
            }
        }
    }
}
