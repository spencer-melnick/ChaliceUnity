using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicCharacter : Character
{
    public float moveSpeed = 3.0f;
    public float moveAcceleration = 20.0f;
    public float rotationSpeedDegrees = 30.0f;
    public float jumpHeight = 2.0f;

    public float gravitationalAcceleration = 9.8f;
    public float slopeLimitDegrees = 45.0f;

    // If a slope is closer this distance, the player will snap to it while walking down
    public float slopeDropLimit = 0.1f;

    // Any raycast speculated collisions or found penetrations will be prevented by this distance
    public float collisionResolutionDistance = 0.01f;

    public uint maxPenetrationsPerStep = 10;
    public uint maxSpeculativeSteps = 15;
    public uint maxPenetrationResolutionSteps = 2;

    CapsuleCollider _capsuleCollider;
    Rigidbody _rigidBody;

    Vector3 _position;
    Vector3 _velocity;
    Vector3 _desiredMovement;
    Quaternion _desiredRotation;

    bool _isLooking = false;
    bool _isGrounded = false;
    bool _shouldJump = false;

    Collider[] _penetratingColliders;
    Vector3 _lastFreePosition;

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

    public override void Jump()
    {
        if (_isGrounded)
        {
            _shouldJump = true;
        }
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

        _isGrounded = false;
        MoveSpeculatively(_velocity * Time.fixedDeltaTime, maxSpeculativeSteps);
        ResolvePenetrations();
        TrySnapToSlope();

        _rigidBody.MovePosition(_position);
    }

    private void OnGrounded()
    {
        _isGrounded = true;
    }

    private void OnHit(RaycastHit hit)
    {
        float slope = Vector3.Angle(hit.normal, Vector3.up);
        
        if (slope < slopeLimitDegrees)
        {
            OnGrounded();
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
        // Only try to snap if the character isn't grounded
        // Cast from capsule based on the drop limit
        if (!_isGrounded && CastFromOwnCapsule(Vector3.down * slopeDropLimit, out RaycastHit hit))
        {
            float slope = Vector3.Angle(hit.normal, Vector3.up);

            // Only planes less sloped than the limit count as ground
            if (slope < slopeLimitDegrees)
            {
                // Snap to the ground
                float movementDistance = Mathf.Max(hit.distance - collisionResolutionDistance, 0.0f);
                _position += Vector3.down * movementDistance;
                _velocity += Vector3.down * (movementDistance / Time.fixedDeltaTime);

                // Set the ground plane to this
                OnGrounded();
            }
        }
    }

    // Handles acceleration based on desired movement and gravity
    void HandleAcceleration()
    {
        // Apply gravity first
        _velocity += Vector3.down * gravitationalAcceleration * Time.fixedDeltaTime;

        if (_isGrounded)
        {
            // Only apply walking acceleration when on the ground
            // Accelerate only movement along ground plane
            Vector3 previousMovement = _velocity;
            Vector3 currentMovement = Vector3.MoveTowards(previousMovement, _desiredMovement, moveAcceleration * Time.fixedDeltaTime);

            _velocity += currentMovement - previousMovement;
        }

        if (_shouldJump)
        {
            _velocity += Vector3.up * Mathf.Sqrt(gravitationalAcceleration * jumpHeight * 2.0f);
            _shouldJump = false;
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
                float movementDistance = Mathf.Max(0.0f, hit.distance - collisionResolutionDistance);
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
        for (int i = 0; i < maxPenetrationResolutionSteps; i++)
        {
            // Check for possible penetrations
            CalculateOwnCapsuleParameters(out Vector3 capsuleTop, out Vector3 capsuleBottom, out float radius);
            int penetrations = Physics.OverlapCapsuleNonAlloc(capsuleTop, capsuleBottom, radius, _penetratingColliders);

            if (penetrations > 1)
            {
                for (int j = 0; j < penetrations; j++)
                {
                    Collider penetratingCollider = _penetratingColliders[j];

                    // Don't collide with self, and double check using penetration calculaton
                    if (penetratingCollider != _capsuleCollider &&
                        Physics.ComputePenetration(_capsuleCollider, _position, _rigidBody.rotation,
                            penetratingCollider, penetratingCollider.transform.position, penetratingCollider.transform.rotation,
                            out Vector3 direction, out float distance))
                    {
                        // Todo: add trigger on penetration resolution

                        // Resolve penetration using speculative motion
                        _velocity = Vector3.ProjectOnPlane(_velocity, direction);
                        Vector3 movement = _velocity * Time.fixedDeltaTime + direction * (distance + collisionResolutionDistance);
                        MoveSpeculatively(movement, 1);
                    }
                }
            }
            else
            {
                break;
            }
        }
    }
}
