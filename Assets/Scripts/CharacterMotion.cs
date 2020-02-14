using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMotion : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] int pathLength = 20;
    [SerializeField] bool showPath = false;
    FullAccessQueue<Transform> movePath;
    new Rigidbody2D rigidbody;
    float pointsLength;

    //move
    [Header("Move")]
    [SerializeField] float baseMoveSpeed = 2;
    [Tooltip("put speed and time between 0 to 1")]
    [SerializeField] AnimationCurve moveSpeedCurve;
    Vector2 moveVelocity;
    float passedLength;
    float passedMoveAccelerationTime;
    Transform startPoint;

    //spin
    [Header("Spin")]
    [SerializeField] float baseSpinSpeed = 20;
    [Tooltip("put speed and time between 0 to 1")]
    [SerializeField] AnimationCurve spinSpeedCurve;
    public float spinVelocity;
    public float baseInverseSpinAcceleration = 100;
    public AnimationCurve inverseSpinAccelerationCurve;
    public float inversSpinTimeScale = 1;
    float passedSpinTime;
    Vector2 lastSpinTarget;
    float spin;
    bool prevInverseSpin = false;

    //interpolation
    const int BEZIER_ESTIMATE_POINTS_COUNT = 100;    //enough precision for this game
    float[] cumulativeLengths = new float[BEZIER_ESTIMATE_POINTS_COUNT + 1];
    const float SPEED_CURVE_PRECISION = 0.0001f;
    static List<Vector2> tempBezierPoints = new List<Vector2>();

    private void Awake()
    {
        movePath = new FullAccessQueue<Transform>(pathLength);
        lastSpinTarget = transform.position + transform.up;
        rigidbody = GetComponent<Rigidbody2D>();
    }

    public void AddTargetPos(Vector2 position)
    {
        if(movePath.Count < pathLength)
        {
            if(movePath.Count == 0)
            {
                startPoint = PositionPool.Instance.GetPosition();
                startPoint.position = transform.position;
                movePath.Enqueue(startPoint);
            }

            Transform signPosition = PositionPool.Instance.GetPosition();
            signPosition.position = position;
            movePath.Enqueue(signPosition);
            if (showPath)
            {
                signPosition.gameObject.SetActive(true);
            }
        }
        else
        {
            Transform signPosition = movePath.Dequeue();
            signPosition.position = position;
            movePath.Enqueue(signPosition);
            if (showPath)
            {
                signPosition.gameObject.SetActive(true);
            }
            else
            {
                signPosition.gameObject.SetActive(false);
            }
        }

        CalculateBezierLength();
        //set start velocities
        Vector2 startPointTangent = (GetBezierPoint(1f / BEZIER_ESTIMATE_POINTS_COUNT) - GetBezierPoint(0)).normalized;
        moveVelocity = startPointTangent * baseMoveSpeed * moveSpeedCurve.Evaluate(0);
        spinVelocity = baseSpinSpeed * spinSpeedCurve.Evaluate(0);
    }

    void CalculateBezierLength()
    {
        //fill cumulativeLengths by the bezier curve point to point lengthes
        float lengthSum = 0;
        float interpolatePart = 1f / BEZIER_ESTIMATE_POINTS_COUNT;
        float interpolation = interpolatePart;
        Vector2 prevPoint = GetBezierPoint(0);
        cumulativeLengths[0] = 0;
        for(int i = 1; i < cumulativeLengths.Length; i++)
        {
            Vector2 point = GetBezierPoint(interpolation);
            cumulativeLengths[i] = Vector2.Distance(prevPoint, point) + lengthSum;
            lengthSum = cumulativeLengths[i];
            prevPoint = point;
            interpolation += interpolatePart;
        }
    }

    private void FixedUpdate()
    {
        //BezierMove();
        RotateToNextPoint();
        UpdateMovePath();
    }

    void BezierMove()
    {
        if(movePath.Count > 0)
        {
            passedMoveAccelerationTime = Mathf.Clamp(passedMoveAccelerationTime + Time.fixedDeltaTime, 0, 1);
            float speedSize = baseMoveSpeed * moveSpeedCurve.Evaluate(passedMoveAccelerationTime);
            float moveLength = speedSize * Time.fixedDeltaTime;
            passedLength += moveLength;
            float interpolate = GetBezierInterpolate(passedLength);
            Vector2 nextPoint = GetBezierPoint(interpolate);
            interpolate = GetBezierInterpolate(passedLength + Time.fixedDeltaTime); //next next interpolate
            Vector2 nextNextPoint = GetBezierPoint(interpolate);
            Vector2 nextTangent = (nextNextPoint - nextPoint).normalized;
            moveVelocity = speedSize  * moveVelocity.normalized;
            rigidbody.position = nextPoint;

            float deltaAngle = Vector2.Angle(moveVelocity, nextTangent);
            //make the velocity it's projection on nextTangent vector
            moveVelocity = Mathf.Cos(deltaAngle * Mathf.PI / 180f) * moveVelocity.magnitude * nextTangent;

            //find next passedTime
            float minMoveSpeed = baseMoveSpeed * moveSpeedCurve.Evaluate(0);
            if (moveVelocity.magnitude < minMoveSpeed)
            {
                moveVelocity = minMoveSpeed * moveVelocity.normalized;
                passedMoveAccelerationTime = 0;
            }
            else
            {
                passedMoveAccelerationTime = GetSpeedCurveTime(moveSpeedCurve, moveVelocity.magnitude, baseMoveSpeed);
            }
        }
        else
        {
            moveVelocity = Vector2.zero;
            passedLength = 0;
            passedMoveAccelerationTime = 0;
        }
    }

    void RotateToNextPoint()
    {
        if(movePath.Count > 0)
        {
            if(movePath.Peek() == startPoint)
            {
                lastSpinTarget = movePath[1].position;
            }
            else
            {
                lastSpinTarget = movePath.Peek().position;
            }
        }

        Vector2 toTarget = (lastSpinTarget - (Vector2)transform.position).normalized;
        if (toTarget.normalized != ((Vector2)transform.up).normalized)
        {
            float toEndAngle = Vector2.SignedAngle(transform.up, toTarget);
            //float spin;
            bool inverseSpin = toEndAngle * spinVelocity < 0; //true if spinVelocity is inverse with rotation to target

            if (inverseSpin)
            {
                if(prevInverseSpin != inverseSpin)
                {
                    passedSpinTime = 0;
                    prevInverseSpin = inverseSpin;
                }

                float timeInterval = Mathf.Clamp(passedSpinTime * inversSpinTimeScale, 0, 1);
                float inverseAcceleration = baseInverseSpinAcceleration * inverseSpinAccelerationCurve.Evaluate(timeInterval);
                spinVelocity += -Mathf.Sign(spinVelocity) * Mathf.Abs(inverseAcceleration * Time.fixedDeltaTime);
                spin = spinVelocity * Time.fixedDeltaTime;
                passedSpinTime += Time.fixedDeltaTime;
            }
            else
            {
                passedSpinTime = GetSpeedCurveTime(spinSpeedCurve, Mathf.Abs(spinVelocity), baseSpinSpeed);
                passedSpinTime = Mathf.Clamp(passedSpinTime + Time.fixedDeltaTime, 0, 1);
                spinVelocity = Mathf.Sign(spinVelocity) * baseSpinSpeed * spinSpeedCurve.Evaluate(passedSpinTime);
                spin = spinVelocity * Time.fixedDeltaTime;
            }

            if((toEndAngle - spin) * toEndAngle < 0)    //transform.up crossed the target by rotation
            {
                transform.up = toTarget;
                float performedSpeed = toEndAngle / Time.fixedDeltaTime;
                passedSpinTime = GetSpeedCurveTime(spinSpeedCurve, Mathf.Abs(performedSpeed), baseSpinSpeed);
            }
            else
            {
                Quaternion rotation = Quaternion.AngleAxis(spin, Vector3.forward);
                Vector3 rotatedUp = rotation * transform.up;
                transform.up = rotatedUp;

                if (inverseSpin)
                {
                    //check inverse speed with rotation to target after spin acceleration
                    if (toEndAngle * spinVelocity > 0) // sign of spinVelocity has changed(becuase spinVelocity had inverse sign with toEndAngle)
                    {
                        passedSpinTime = GetSpeedCurveTime(spinSpeedCurve, Mathf.Abs(spinVelocity), baseSpinSpeed); ;
                    }
                }
            }
        }
        else if (movePath.Count == 0) //rotation reached the end target in movePath
        {
            spinVelocity = 0;
            passedSpinTime = 0;
        }
    }

    private void UpdateMovePath()
    {
        // TODO remove the passed move position and stop(moveVelocity = Vector2.zero) if movePath is empty

    }

    Vector2 GetBezierPoint(float interpolate)
    {
        tempBezierPoints.Clear();
        for (int i = 0; i < movePath.Count; i++)
        {
            tempBezierPoints.Add(movePath[i].position);
        }

        return GetBezierPointRecursive(tempBezierPoints, movePath.Count, interpolate);
    }

    Vector2 GetBezierPointRecursive(List<Vector2> points, int pointCount, float interpolate)
    {
        if(pointCount > 1)
        {
            for (int i = 1; i < pointCount; i++)
            {
                Vector2 interpolatedPoint = points[i - 1] + interpolate * (points[i] - points[i - 1]);
                points[i - 1] = interpolatedPoint;
            }

            return GetBezierPointRecursive(points, pointCount - 1, interpolate);
        }
        else
        {
            return points[0];
        }
    }

    float GetBezierInterpolate(float length)
    {
        int minIndex = 0;
        int maxIndex = cumulativeLengths.Length - 1;
        int midIndex = minIndex + (maxIndex - minIndex) / 2;

        while (minIndex < maxIndex)
        {
            midIndex = minIndex + (maxIndex - minIndex) / 2;
            if(cumulativeLengths[midIndex] < length)
            {
                minIndex = midIndex + 1;
            }
            else
            {
                maxIndex = midIndex;
            }
        }

        if (cumulativeLengths[midIndex] > length)
        {
            midIndex--;
        }
        length = Mathf.Clamp(length, cumulativeLengths[0], cumulativeLengths[cumulativeLengths.Length - 1]);

        float lengthBefore = cumulativeLengths[midIndex];
        if(lengthBefore == length)
        {
            return midIndex / BEZIER_ESTIMATE_POINTS_COUNT;
        }
        else
        {
            return (midIndex + (length - lengthBefore)/(cumulativeLengths[midIndex + 1] - lengthBefore)) / BEZIER_ESTIMATE_POINTS_COUNT;
        }
    }

    float GetSpeedCurveTime(AnimationCurve curve, float speed, float baseSpeed)
    {
        float normalizedSpeed = speed / baseSpeed;
        normalizedSpeed = Mathf.Clamp(normalizedSpeed, curve.Evaluate(0), curve.Evaluate(1));
        float minTime = 0;
        float maxTime = 1;
        float midTime = minTime + (maxTime - minTime) / 2;
        float mid = curve.Evaluate(midTime);

        //binary search
        while (Mathf.Abs(mid - normalizedSpeed) > SPEED_CURVE_PRECISION)
        {
            midTime = minTime + (maxTime - minTime) / 2;
            mid = curve.Evaluate(midTime);
            if(mid < normalizedSpeed)
            {
                minTime = midTime;
            }
            else
            {
                maxTime = midTime;
            }
        }

        return midTime;
    }
}
