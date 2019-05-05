using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable IDE1006 // Naming Styles

public class Character : MonoBehaviour
{
    public struct KinematicCollision
    {
        public Vector3 point;
        public Vector3 normal;
        public float slope;
    }

    public float groundMoveSpeed = 5.0f;
    public float groundMoveAcceleration = 15.0f;
    public float rotationSpeed = 10.0f;
    public float gravitationalAcceleration = 9.8f;

    public float slopeAngleLimit = 45.0f;
    public float slopeLimitDampening = 0.8f;
    public float groundSnapDistance = 0.1f;
    public float collisionResolutionDistance = 0.05f;
    public uint maxSpeculativeSteps = 15;
    public uint maxResolutionSteps = 10;

    public Vector3 position { get; private set; }
    public Vector3 velocity { get; private set; }
    public bool isGrounded { get; private set; }
    public bool wasGrounded { get; private set; }
    public KinematicCollision ground { get; private set; }


    // Components
    private Rigidbody _rigidbody;
    private CapsuleCollider _capsuleCollider;

    private Vector3 _desiredLook;
    private Vector3 _desiredPlanarVelocity;
    private Vector3 _planarVelocity;

    private Collider[] _overlappingColliders;


    // Public methods

    public void MovePlanar(Vector3 movement, Vector3 lookDirection)
    {
        _desiredPlanarVelocity = movement * groundMoveSpeed;
        _desiredLook = lookDirection;
    }

    public void MovePlanar(Vector3 movement)
    {
        MovePlanar(movement, movement.normalized);
    }


    // Unity overrides
    private void Awake()
    {
        position = transform.position;
        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();

        _overlappingColliders = new Collider[15];
    }

    private void FixedUpdate()
    {
        ResetGrounded();
        ProbeGround();
        UpdateVelocity();
        TryMove();
        ResolvePenetrations();
        _rigidbody.MovePosition(position);
        UpdateRotation();
    }


    // Physics substeps
    void ResetGrounded()
    {
        KinematicCollision groundCollision = new KinematicCollision
        {
            normal = Vector3.up,
            slope = 0.0f
        };

        ground = groundCollision;
        wasGrounded = isGrounded;
        isGrounded = false;
    }

    void ProbeGround()
    {
        KinematicCollision groundCollision;
        CalculateOwnCapsuleParameters(out Vector3 capsuleTop, out Vector3 capsuleBottom, out float radius);

        // Move capsule bottom up just a bit to make sure we raycast against anything that's already touching
        capsuleBottom += Vector3.up * collisionResolutionDistance;

        // Check for ground by casting our capsule down
        if (Physics.CapsuleCast(capsuleTop, capsuleBottom, radius, -Vector3.up, out RaycastHit capsuleHit, groundSnapDistance))
        {
            groundCollision.point = capsuleHit.point;
            groundCollision.normal = capsuleHit.normal;
            groundCollision.slope = Vector3.Angle(groundCollision.normal, Vector3.up);

            // Try to find surface normal if possible, using raycast
            if (Physics.Raycast(capsuleBottom, -capsuleHit.normal, out RaycastHit rayHit, (groundSnapDistance + radius)))
            {
                groundCollision.normal = rayHit.normal;
                groundCollision.slope = Vector3.Angle(groundCollision.normal, Vector3.up);
            }

            if (groundCollision.slope <= slopeAngleLimit)
            {
                // Update ground properties
                isGrounded = true;
                ground = groundCollision;

                // Snap to ground
                position += Vector3.up * -(capsuleHit.distance - collisionResolutionDistance);

                if (!wasGrounded)
                {
                    HandleOnGrounded();
                }
            }
            else if (groundCollision.slope <= 90.0f && wasGrounded)
            {
                HandleOnTakeoff(groundCollision);
            }
        }
        else if (wasGrounded)
        {
            HandleOnTakeoff();
        }
    }

    void UpdateVelocity()
    {
        if (isGrounded)
        {
            // Try to accelerate our planar velocity towards our desired planar velocity
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, _desiredPlanarVelocity, groundMoveAcceleration * Time.fixedDeltaTime);

            // Rotate planar velocity around ground plane
            Quaternion groundRotation = Quaternion.FromToRotation(Vector3.up, ground.normal);
            velocity = groundRotation * _planarVelocity;
        }
        else
        {
            velocity += Vector3.up * -gravitationalAcceleration * Time.fixedDeltaTime;
        }
    }

    void TryMove()
    {
        MoveSpeculatively(velocity * Time.fixedDeltaTime, true);
    }

    void MoveSpeculatively(Vector3 movement, bool affectVelocity)
    {
        for (int i = 0; i < maxSpeculativeSteps; i++)
        {
            if (CastFromOwnCapsule(movement, out RaycastHit hit))
            {
                // Calculate out how far we can move until we collided and find remaining movement
                Vector3 distanceTraveled = hit.distance * movement.normalized;
                position += distanceTraveled;
                movement -= distanceTraveled;

                // Slide along surface
                if (affectVelocity)
                {
                    velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
                }
                movement = Vector3.ProjectOnPlane(movement, hit.normal);
            }
            else
            {
                position += movement;
                break;
            }
        }
    }

    void ResolvePenetrations()
    {
        for (int i = 0; i < maxResolutionSteps; i++)
        {
            CalculateOwnCapsuleParameters(out Vector3 capsuleTop, out Vector3 capsuleBottom, out float radius);
            int overlaps = Physics.OverlapCapsuleNonAlloc(capsuleTop, capsuleBottom, radius, _overlappingColliders);

            // Try and remove our own capsule collider from the overlapping colliders
            for (int j = 0; j < overlaps; j++)
            {
                Collider overlappingCollider = _overlappingColliders[j];
                if (overlappingCollider == _capsuleCollider)
                {
                    // Swap capsule collider with last used element in the array and decrement length
                    Collider tempCollider = _overlappingColliders[overlaps - 1];
                    _overlappingColliders[overlaps - 1] = _capsuleCollider;
                    _overlappingColliders[j] = tempCollider;
                    overlaps--;
                    break;
                }
            }

            // If we still have overlaps, we must resolve them
            if (overlaps > 0)
            {
                for (int j = 0; j < overlaps; j++)
                {
                    Collider otherCollider = _overlappingColliders[j];

                    // Computer penetration resolution vectors if they exist
                    if (Physics.ComputePenetration(_capsuleCollider, position, _capsuleCollider.transform.rotation,
                        otherCollider, otherCollider.transform.position, otherCollider.transform.rotation,
                        out Vector3 direction, out float distance))
                    {
                        // Attempt to move out of the collision
                        Vector3 movement = direction * distance;

                        // If character is moving into ground plane, cancel that motion
                        if (isGrounded && Vector3.Dot(ground.normal, movement) < 0)
                        {
                            movement = Vector3.ProjectOnPlane(movement, ground.normal);
                        }
                        velocity = Vector3.ProjectOnPlane(velocity, direction);
                        MoveSpeculatively(movement, true);
                    }
                }
            }
        }
    }

    void UpdateRotation()
    {
        Quaternion localRotation = _rigidbody.rotation * Quaternion.Inverse(Quaternion.FromToRotation(Vector3.up, Vector3.up));
        float yaw = localRotation.eulerAngles.y;
        localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);

        Quaternion rotation;

        if (isGrounded && !Mathf.Approximately(_desiredPlanarVelocity.magnitude, 0.0f))
        {
            rotation = Quaternion.FromToRotation(Vector3.up, Vector3.up) * Quaternion.LookRotation(_desiredLook, Vector3.up);
        }
        else
        {
            rotation = Quaternion.FromToRotation(Vector3.up, Vector3.up) * localRotation;
        }

        
        rotation = Quaternion.RotateTowards(_rigidbody.rotation, rotation, rotationSpeed * Time.fixedDeltaTime);
        _rigidbody.MoveRotation(rotation);
    }

    // Custom triggers (sort of)

    void HandleOnTakeoff(KinematicCollision? collisionNullable = null)
    {
        // If character took off and is touching a steep slope cancel upward velocity
        if (collisionNullable != null && Vector3.Dot(Vector3.up, velocity) > 0.0f)
        {
            KinematicCollision collision = collisionNullable.GetValueOrDefault();

            // Calculate vector moving upward along slope
            // Note: quaternions not necessary here
            float yaw = Mathf.Atan2(collision.normal.x, collision.normal.z) * 180.0f / Mathf.PI;
            float pitch = Mathf.Acos(collision.normal.y) * 180.0f / Mathf.PI;
            Vector3 upwardNormal = Quaternion.Euler(pitch, yaw, 0.0f) * Vector3.forward;

            // Remove velocity along slope vector
            velocity -= Vector3.Project(velocity, upwardNormal) * slopeLimitDampening;
        }
    }

    void HandleOnGrounded()
    {
        _planarVelocity = Vector3.ProjectOnPlane(velocity, ground.normal);
    }


    // Helper functions
    void CalculateOwnCapsuleParameters(out Vector3 capsuleTop, out Vector3 capsuleBottom, out float radius)
    {
        capsuleTop = transform.position +_capsuleCollider.center + Vector3.up * (_capsuleCollider.height / 2 - _capsuleCollider.radius);
        capsuleBottom = transform.position + _capsuleCollider.center + Vector3.up * -(_capsuleCollider.height / 2 - _capsuleCollider.radius);
        radius = _capsuleCollider.radius;
    }

    bool CastFromOwnCapsule(Vector3 ray, out RaycastHit hit)
    {
        CalculateOwnCapsuleParameters(out Vector3 capsuleTop, out Vector3 capsuleBottom, out float radius);
        Vector3 direction = ray.normalized;
        return Physics.CapsuleCast(capsuleTop, capsuleBottom, radius, direction, out hit, ray.magnitude);
    }
}
