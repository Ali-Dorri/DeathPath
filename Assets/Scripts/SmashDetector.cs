using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmashDetector : MonoBehaviour
{
    [SerializeField] Character character;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        CharacterMotion motion = null;
        if (collider.transform.parent != null)
        {
            motion = collider.transform.parent.GetComponent<CharacterMotion>();
        }

        bool smashed = gameObject.tag == "Player" && collider.gameObject.tag == "Enemy Weapon";
        smashed = smashed || gameObject.tag == "Enemy" && collider.gameObject.tag == "Player Weapon";

        if (smashed && motion != null)
        {
            character.OnSmash(collider, motion);
        }
    }
}
