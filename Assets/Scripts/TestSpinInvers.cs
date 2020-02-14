using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSpinInvers : MonoBehaviour
{
    public float spinVelocity;
    public float baseInverseSpinAcceleration = 100;
    public AnimationCurve inverseSpinAccelerationCurve;
    public float inversSpinTimeScale = 1;
    float time = 0;

    private void Start()
    {
        time = Time.fixedTime;
    }

    void FixedUpdate()
    {
        float timeInterval = Time.fixedTime - time;
        timeInterval = Mathf.Clamp(timeInterval * inversSpinTimeScale, 0, 1);
        float acceleration = baseInverseSpinAcceleration * inverseSpinAccelerationCurve.Evaluate(timeInterval);
        spinVelocity -= acceleration * Time.fixedDeltaTime;
        float spin = spinVelocity * Time.fixedDeltaTime;
        Quaternion rotation = Quaternion.AngleAxis(spin, Vector3.forward);
        transform.up = rotation * transform.up;
    }
}
