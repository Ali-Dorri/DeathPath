using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodSplash : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] new SpriteRenderer renderer;

    public void Splash(float bloodLifeTime)
    {
        animator.Play("Splash");
        StartCoroutine(DisappearBlood(bloodLifeTime));
    }

    public void Restart()
    {
        Color color = renderer.color;
        color.a = 1;
        renderer.color = color;
        animator.Rebind();
    }

    IEnumerator DisappearBlood(float time)
    {
        yield return new WaitForSeconds(0.5f);  //wait for splash animation
        float startTime = Time.time;
        while (Time.time - startTime < time)
        {
            float fade = (Time.time - startTime) / time;
            fade = 1 - fade;
            Color color = renderer.color;
            color.a = fade;
            renderer.color = color;
            yield return null;
        }

        GameManager.Instance.bloodSplasher.DestroyBlood(this);
    }
}
