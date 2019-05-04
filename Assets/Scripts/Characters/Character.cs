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
    public float gravitationalAcceleration = 9.8f;

    public Vector3 upVector = Vector3.up;
    public float slopeAngleLimit = 45.0f;
    public float groundSnapDistance = 0.1f;
    public float collisionResolutionDistance = 0.05f;
    public uint maxSpeculativeSteps = 15;
    public uint maxResolutionSteps = 10;

    public Vector3 position { get; private set; }
    public Vector3 velocity { get; private set; }
    public bool isGrounded { get; private set; }
    public KinematicCollision ground { get; private set; }


    // Components
    private Rigidbody _rigidbody;
    private CapsuleCollider _capsuleCollider;

    private Vector3 _planarVelocity;

    private Collider[] _overlappingColliders;


    // Public methods
    public void MovePlanar(Vector3 movement)
    {
        _planarVelocity = movement;
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

        DebugDraw();

        MoveSpeculatively();
        ResolvePenetrations();
        _rigidbody.MovePosition(position);
    }


    // Physics substeps
    void ResetGrounded()
    {
        KinematicCollision groundCollision = new KinematicCollision
        {
            normal = upVector,
            slope = 0.0f
        };

        ground = groundCollision;
        isGrounded = false;
    }

    void ProbeGround()
    {
        KinematicCollision groundCollision;
        CalculateOwnCapsuleParameters(out Vector3 capsuleTop, out Vector3 capsuleBottom, out float radius);

        // Move capsule bottom up just a bit to make sure we raycast against anything that's already touching
        capsuleBottom += upVector * collisionResolutionDistance;

        // Check for ground by casting our capsule down
        if (Physics.CapsuleCast(capsuleTop, capsuleBottom, radius, -upVector, out RaycastHit capsuleHit, groundSnapDistance))
        {
            groundCollision.point = capsuleHit.point;
            groundCollision.normal = capsuleHit.normal;
            groundCollision.slope = Vector3.Angle(groundCollision.normal, upVector);

            // Try to find surface normal if possible, using raycast
            if (Physics.Raycast(capsuleBottom, -capsuleHit.normal, out RaycastHit rayHit, (groundSnapDistance + radius)))
            {
                Debug.DrawLine(capsuleBottom, capsuleBottom + capsuleHit.normal * -(rayHit.distance), Color.red, Time.fixedDeltaTime);

                groundCollision.normal = rayHit.normal;
                groundCollision.slope = Vector3.Angle(groundCollision.normal, upVector);
            }

            if (groundCollision.slope <= slopeAngleLimit)
            {
                // Update ground properties
                isGrounded = true;
                ground = groundCollision;

                // Snap to ground
                position += upVector * -(capsuleHit.distance - collisionResolutionDistance);
            }
        }
    }

    void UpdateVelocity()
    {
        if (isGrounded)
        {
            Quaternion groundRotation = Quaternion.FromToRotation(Vector3.up, ground.normal);
            velocity = groundRotation * _planarVelocity * groundMoveSpeed;
        }
        else
        {
            velocity += upVector * -gravitationalAcceleration * Time.fixedDeltaTime;
        }
    }

    void MoveSpeculatively()
    {
        for (int i = 0; i < maxSpeculativeSteps; i++)
        {
            Vector3 movement = velocity * Time.fixedDeltaTime;
            Debug.DrawLine(position, position + movement, Color.yellow, Time.fixedDeltaTime);

            if (CastFromOwnCapsule(movement, out RaycastHit hit))
            {
                position += hit.distance * velocity.normalized;
                velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
            }
            else
            {
                position += movement;
                break;
            }
        }
    }

    // This is used for penetration resolution
    void MoveSpeculatively(Vector3 movement)
    {
        for (int i = 0; i < maxSpeculativeSteps; i++)
        {
            if (CastFromOwnCapsule(movement, out RaycastHit hit))
            {
                position += hit.distance * movement.normalized;
                velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
                movement = Vector3.ProjectOnPlane(movement, hit.normal);
            }
            else
            {
                velocity = Vector3.ProjectOnPlane(velocity, movement.normalized);
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
                    if (Physics.ComputePenetration(_capsuleCollider, _capsuleCollider.transform.position, _capsuleCollider.transform.rotation,
                        otherCollider, otherCollider.transform.position, otherCollider.transform.rotation,
                        out Vector3 direction, out float distance))
                    {
                        // Attempt to move out of the collision
                        Vector3 movement = direction * distance;
                        Debug.DrawLine(position, position + movement, Color.magenta, Time.fixedDeltaTime);
                        MoveSpeculatively(movement);
                    }
                }
            }
        }
    }


    // Helper functions
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

    void DebugDraw()
    {
        if (isGrounded)
        {
            Debug.DrawLine(position, position + ground.normal, Color.green, Time.fixedDeltaTime);
        }

        Debug.DrawLine(position, position + velocity, Color.blue, Time.fixedDeltaTime);
    }
}
