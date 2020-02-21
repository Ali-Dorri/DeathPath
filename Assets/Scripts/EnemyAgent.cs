using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAgent : MonoBehaviour
{
    public float speed = 3;
    new Rigidbody2D rigidbody;
    public float spinSpeed = 40;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        Character player = GameManager.Instance.Player;
        Vector2 direction = ((Vector2)player.transform.position - rigidbody.position).normalized;
        rigidbody.position += direction * speed * Time.fixedDeltaTime;

        //rotate
        float angle = Vector2.SignedAngle(transform.up, direction);
        float spin = spinSpeed * Time.fixedDeltaTime * Mathf.Sign(angle);
        if(Mathf.Abs(spin) > Mathf.Abs(angle))
        {
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.up = rotation * transform.up;
        }
        else
        {
            Quaternion rotation = Quaternion.AngleAxis(spin, Vector3.forward);
            transform.up = rotation * transform.up;
        }
    }

    public void Stop()
    {
        speed = 0;
        spinSpeed = 0;
    }

    internal void Restart()
    {
        speed = 3;
        spinSpeed = 40;
    }
}
