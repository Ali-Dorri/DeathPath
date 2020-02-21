using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodSplasher : MonoBehaviour
{
    [SerializeField] BloodSplash bloodPrefab;
    [SerializeField] float bloodLifeTime = 2;
    [SerializeField] float minSplashScale = 0.75f;
    [SerializeField] float maxSplashScale = 1.5f;
    Stack<BloodSplash> bloodPool = new Stack<BloodSplash>(10);

    public void SplashNewBlood(Vector3 position, Vector2 hitDirection, float forceRatio)
    {
        float splashScale = minSplashScale + forceRatio * (maxSplashScale - minSplashScale);
        BloodSplash blood = GetBlood();
        blood.transform.position = position;
        blood.transform.localScale = new Vector3(blood.transform.localScale.x, splashScale, blood.transform.localScale.z);
        blood.transform.rotation = Quaternion.LookRotation(Vector3.forward, hitDirection);
        blood.Splash(bloodLifeTime);
    }

    BloodSplash GetBlood()
    {
        BloodSplash blood;
        if (bloodPool.Count > 0)
        {
            blood = bloodPool.Pop();
            blood.gameObject.SetActive(true);
            blood.Restart();
        }
        else
        {
            blood = Instantiate(bloodPrefab);
        }
        return blood;
    }

    public void DestroyBlood(BloodSplash blood)
    {
        bloodPool.Push(blood);
        blood.gameObject.SetActive(true);
    }
}
