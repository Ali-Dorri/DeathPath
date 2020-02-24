using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float deathTime = 1f;
    [SerializeField] Collider2D bladeCollider;
    [SerializeField] SpriteRenderer bodyRenderer;
    [SerializeField] SpriteRenderer bladeRenderer;

    public void OnSmash(Collider2D collider, CharacterMotion motion)
    {
        Die();
        GameManager.Instance.CharacterDied(this, GetSmashForce(motion.MoveVelocity.magnitude, motion.SpinVelocity));
        SplashBlood(collider, motion);
    }

    private void Die()
    {
        animator.SetBool("isDead", true);
        StartCoroutine(DestroyAfterSeconds());
        if(this != GameManager.Instance.Player)
        {
            GetComponent<EnemyAgent>().Stop();
        }
        else
        {
            GetComponent<CharacterMotion>().isActive = false;
        }

        if(bladeCollider != null)
        {
            bladeCollider.enabled = false;
        }
        bodyRenderer.sortingOrder = -1;
        bladeRenderer.sortingOrder = -1;
    }

    IEnumerator DestroyAfterSeconds()
    {
        yield return new WaitForSeconds(deathTime);
        GameManager.Instance.enemySpawner.CharacterDied(this);
    }

    void SplashBlood(Collider2D collider, CharacterMotion motion)
    {
        Vector2 hitDirection = transform.position - collider.transform.position;
        hitDirection.Normalize();
        float force = GetSmashForce(motion.MoveVelocity.magnitude, motion.SpinVelocity);
        float minForce = GetSmashForce(motion.MinMoveVelocity, motion.MinSpinVelocity);
        float maxForce = GetSmashForce(motion.MaxMoveVelocity, motion.MaxSpinVelocity);
        float forceRatio = (force - minForce) / (maxForce - minForce);
        forceRatio = Mathf.Clamp(forceRatio, 0, 1);
        GameManager.Instance.bloodSplasher.SplashNewBlood(transform.position, hitDirection, forceRatio);
    }

    float GetSmashForce(float moveVelocity, float spinVelocity)
    {
        return moveVelocity + spinVelocity;
    }

    public void Restart()
    {
        animator.Rebind();
        if (this != GameManager.Instance.Player)
        {
            GetComponent<EnemyAgent>().Restart();
        }

        if (bladeCollider != null)
        {
            bladeCollider.enabled = true;
        }
        bodyRenderer.sortingOrder = 0;
        bladeRenderer.sortingOrder = 0;
    }
}
