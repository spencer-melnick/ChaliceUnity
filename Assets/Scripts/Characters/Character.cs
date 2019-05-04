using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The base class for character controllers
public class Character : MonoBehaviour
{
    public virtual void Move(Vector3 motion)
    {

    }

    public virtual void MoveAndLook(Vector3 movementDirection, Quaternion lookRotation)
    {

    }

    public virtual void Jump()
    {

    }
}
