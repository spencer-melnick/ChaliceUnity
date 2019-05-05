using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePlanet : MonoBehaviour
{
    public SpecialGravityCharacter character;

    void Update()
    {
        character.upVector = (character.transform.position - transform.position).normalized;
    }
}
