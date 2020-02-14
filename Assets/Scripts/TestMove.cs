using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMove : MonoBehaviour
{
    public Transform[] positions;
    public CharacterMotion motion;


    void Start()
    {
        for(int i =0; i< positions.Length; i++)
        {
            motion.AddTargetPos(positions[i].position);
        }
    }
}
