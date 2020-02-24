using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMotion : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] int pathLength = 20;
    [SerializeField] bool showPath = false;
    FullAccessQueue<Vector2> movePath;
    FullAccessQueue<Vector2> nextPath;
    FullAccessQueue<Transform> signPoints;
    new Rigidbody2D rigidbody;
    float pointsLength;
    public bool isActive = true;

    //move
    [Header("Move")]
    [SerializeField] float baseMoveSpeed = 2;
    [Tooltip("put speed and time between 0 to 1")]
    [SerializeField] AnimationCurve moveSpeedCurve;
    [SerializeField] float moveCurveTimeScale = 1;
    Vector2 moveVelocity;
    float moveVelocitySize;
    float passedLength;
    float passedMoveAccelerationTime;

    //spin
    [Header("Spin")]
    [SerializeField] float baseSpinSpeed = 20;
    [Tooltip("put speed and time between 0 to 1")]
    [SerializeField] AnimationCurve spinSpeedCurve;
    float spinVelocity;
    public float baseInverseSpinAcceleration = 100;
    public AnimationCurve inverseSpinAccelerationCurve;
    public float inverseSpinTimeScale = 1;
    float passedSpinTime;
    Vector2 lastSpinTarget;
    float spin;
    bool prevInverseSpin = false;

    //interpolation
    const int BEZIER_ESTIMATE_POINTS_COUNT = 100;    //enough precision for this game
    float[] cumulativeLengths = new float[BEZIER_ESTIMATE_POINTS_COUNT + 1];
    const float SPEED_CURVE_PRECISION = 0.000001f;
    static List<Vector2> tempBezierPoints = new List<Vector2>();

    //properties
    public float SpinVelocity => spinVelocity;
    public Vector2 MoveVelocity => moveVelocity;
    public float MinSpinVelocity => baseSpinSpeed * spinSpeedCurve.Evaluate(0);
    public float MinMoveVelocity => baseMoveSpeed * moveSpeedCurve.Evaluate(0);
    public float MaxSpinVelocity => baseSpinSpeed * spinSpeedCurve.Evaluate(1);
    public float MaxMoveVelocity => baseMoveSpeed * moveSpeedCurve.Evaluate(1);

    private void Awake()
    {
        movePath = new FullAccessQueue<Vector2>(pathLength);
        nextPath = new FullAccessQueue<Vector2>(pathLength);
        if (showPath)
        {
            signPoints = new FullAccessQueue<Transform>(pathLength);
        }
        lastSpinTarget = transform.position + transform.up;
        rigidbody = GetComponent<Rigidbody2D>();
    }

    public void AddTargetPos(Vector2 position)
    {
        if(nextPath.Count < pathLength)
        {
            if(nextPath.Count == 0)
            {
                //add first point
                if(movePath.Count == 0)
                {
                    nextPath.Enqueue(transform.position);
                    AddSignPoint(transform.position);
                }
                else
                {
                    Vector2 lastMovePoint = movePath[movePath.Count - 1];
                    nextPath.Enqueue(lastMovePoint);
                }
            }

            nextPath.Enqueue(position);
        }
        else
        {
            Vector2 firstPoint = nextPath.Dequeue();
            nextPath[0] = firstPoint;
            nextPath.Enqueue(position);
        }

        if (signPoints != null)
        {
            if (signPoints.Count < pathLength)
            {
                AddSignPoint(position);
            }
            else
            {
                Transform firstPoint = RemoveSignPoint();
                firstPoint.position = position;
                AddSignPoint(firstPoint);
            }
        }

        CalculateBezierLength();
        //set start velocities
        //Vector2 startPointTangent = (GetBezierPoint(1f / BEZIER_ESTIMATE_POINTS_COUNT) - GetBezierPoint(0)).normalized;
        //moveVelocity = startPointTangent * baseMoveSpeed * moveSpeedCurve.Evaluate(0);
        //spinVelocity = baseSpinSpeed * spinSpeedCurve.Evaluate(0);
    }

    void AddSignPoint(Vector2 position)
    {
        if (signPoints != null)
        {
            Transform point = PositionPool.Instance.GetPosition();
            point.position = position;
            point.gameObject.SetActive(true);
            signPoints.Enqueue(point);
        }
    }

    void AddSignPoint(Transform point)
    {
        if (signPoints != null && point != null)
        {
            point.gameObject.SetActive(true);
            signPoints.Enqueue(point);
        }
    }

    Transform RemoveSignPoint()
    {
        if (signPoints != null && signPoints.Count != 0)
        {
            Transform point = signPoints.Dequeue();
            point.gameObject.SetActive(false);
            return point;
        }

        return null;
    }

    void CalculateBezierLength()
    {
        if(movePath.Count != 0)
        {
            //fill cumulativeLengths by the bezier curve point to point lengthes
            float lengthSum = 0;
            float interpolatePart = 1f / BEZIER_ESTIMATE_POINTS_COUNT;
            float interpolation = interpolatePart;
            Vector2 prevPoint = GetBezierPoint(0);
            cumulativeLengths[0] = 0;
            for (int i = 1; i < cumulativeLengths.Length; i++)
            {
                Vector2 point = GetBezierPoint(interpolation);
                cumulativeLengths[i] = Vector2.Distance(prevPoint, point) + lengthSum;
                lengthSum = cumulativeLengths[i];
                prevPoint = point;
                interpolation += interpolatePart;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isActive)
        {
            BezierMove();
            RotateToNextPoint();
            UpdateMovePath();
            moveVelocitySize = moveVelocity.magnitude;
        }
    }

    void BezierMove()
    {
        if (movePath.Count > 0)
        {
            float prevMoveTime = passedMoveAccelerationTime;
            passedMoveAccelerationTime = (passedMoveAccelerationTime + Time.fixedDeltaTime) / moveCurveTimeScale; 
            passedMoveAccelerationTime = Mathf.Clamp(passedMoveAccelerationTime, 0, 1);
            float speedSize = baseMoveSpeed /** moveSpeedCurve.Evaluate(passedMoveAccelerationTime)*/;
            float moveLength = speedSize * Time.fixedDeltaTime;
            passedLength += moveLength;
            float interpolate = GetBezierInterpolate(passedLength);
            Vector2 nextPoint = GetBezierPoint(interpolate);
            Vector2 tangent = (nextPoint - rigidbody.position).normalized;
            if (moveVelocity.magnitude == 0)
            {
                moveVelocity = tangent * speedSize;
            }
            float deltaAngle = Vector2.Angle(moveVelocity, tangent);
            //make the velocity it's projection on nextTangent vector
            //speedSize = Mathf.Cos(deltaAngle * Mathf.PI / 180f) * moveVelocity.magnitude;
            float minMoveSpeed = baseMoveSpeed * moveSpeedCurve.Evaluate(0);
            if (speedSize < minMoveSpeed)
            {
                moveVelocity = minMoveSpeed * tangent;
                passedMoveAccelerationTime = 0;
            }
            else
            {
                moveVelocity = speedSize * tangent;
                //passedMoveAccelerationTime = GetCurveTime(moveSpeedCurve, speedSize / baseMoveSpeed) * moveCurveTimeScale;
                //if(FloatEquals(prevMoveTime, passedMoveAccelerationTime, Time.fixedDeltaTime * 2))  //passed time was not increased because of find time precision
                //{
                //    passedMoveAccelerationTime = (prevMoveTime + Time.fixedDeltaTime) / moveCurveTimeScale;
                //    passedMoveAccelerationTime = Mathf.Clamp(passedMoveAccelerationTime, 0, 1) * moveCurveTimeScale;
                //}
            }
            rigidbody.position = nextPoint;
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

                float timeInterval = Mathf.Clamp(passedSpinTime * inverseSpinTimeScale, 0, 1);
                float inverseAcceleration = baseInverseSpinAcceleration * inverseSpinAccelerationCurve.Evaluate(timeInterval);
                spinVelocity += -Mathf.Sign(spinVelocity) * Mathf.Abs(inverseAcceleration * Time.fixedDeltaTime);
                spin = spinVelocity * Time.fixedDeltaTime;
                passedSpinTime += Time.fixedDeltaTime;
            }
            else
            {
                passedSpinTime = GetCurveTime(spinSpeedCurve, Mathf.Abs(spinVelocity) / baseSpinSpeed);
                passedSpinTime = Mathf.Clamp(passedSpinTime + Time.fixedDeltaTime, 0, 1);
                spinVelocity = Mathf.Sign(spinVelocity) * baseSpinSpeed * spinSpeedCurve.Evaluate(passedSpinTime);
                spin = spinVelocity * Time.fixedDeltaTime;
            }

            if((toEndAngle - spin) * toEndAngle < 0)    //transform.up crossed the target by rotation
            {
                transform.up = toTarget;
                float performedSpeed = toEndAngle / Time.fixedDeltaTime;
                passedSpinTime = GetCurveTime(spinSpeedCurve, Mathf.Abs(performedSpeed) / baseSpinSpeed);
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
                        passedSpinTime = GetCurveTime(spinSpeedCurve, Mathf.Abs(spinVelocity) / baseSpinSpeed);
                    }
                }
            }
        }
        else if (movePath.Count == 0) //rotation reached the end target in movePath
        {
            spinVelocity = 0;
            passedSpinTime = 0;
            lastSpinTarget = transform.position + transform.up;
        }
    }

    private void UpdateMovePath()
    {
        if(movePath.Count > 0)
        {
            float interpolate = GetBezierInterpolate(passedLength);
            float distanceSum = 0;
            int passedPoints = 0;
            for (int pointIndex = 1; pointIndex < movePath.Count; pointIndex++)
            {
                float distance = Vector2.Distance(movePath[pointIndex], movePath[pointIndex - 1]);
                distanceSum += distance;
                if (interpolate < distanceSum)
                {
                    passedPoints = pointIndex - 1;
                    break;
                }
                else if (interpolate == distanceSum)
                {
                    passedPoints = pointIndex;
                    break;
                }
            }
            passedPoints++; //index to count

            //remove the passed signPoint
            if(signPoints != null)
            {
                int nextPathSigns = Mathf.Clamp(nextPath.Count - 1, 0, nextPath.Count);
                int pathSigns = signPoints.Count - nextPathSigns;
                if(pathSigns > 0)
                {
                    int passedSigns = passedPoints - (movePath.Count - pathSigns);
                    for (int i = 0; i < passedSigns; i++)
                    {
                        RemoveSignPoint();
                    }
                }
            }

            //set lastSpinTarget to the point after passed point
            if(passedPoints != movePath.Count)
            {
                lastSpinTarget = movePath[passedPoints];
            }
            else
            {
                lastSpinTarget = movePath[passedPoints - 1];
            }

            //passed whole path
            if (interpolate >= 1)
            {
                FullAccessQueue<Vector2> tempPath = movePath;
                movePath = nextPath;
                nextPath = tempPath;
                nextPath.Clear();
                passedMoveAccelerationTime = 0;
                passedLength = 0;
            }
        }
        else
        {
            //swap pathes
            FullAccessQueue<Vector2> tempPath = movePath;
            movePath = nextPath;
            nextPath = tempPath;
        }
    }

    Vector2 GetBezierPoint(float interpolate)
    {
        if(movePath.Count != 0)
        {
            tempBezierPoints.Clear();
            for (int i = 0; i < movePath.Count; i++)
            {
                tempBezierPoints.Add(movePath[i]);
            }

            return GetBezierPointRecursive(tempBezierPoints, movePath.Count, interpolate);
        }

        throw new System.InvalidOperationException("The move path is empty. Not bezier point exists.");
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
        else if(pointCount == 1)
        {
            return points[0];
        }

        throw new System.InvalidOperationException("No control point specified to return bezier point.");
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
            return midIndex / (float)BEZIER_ESTIMATE_POINTS_COUNT;
        }
        else
        {
            return (midIndex + (length - lengthBefore)/(cumulativeLengths[midIndex + 1] - lengthBefore)) / BEZIER_ESTIMATE_POINTS_COUNT;
        }
    }

    float GetCurveTime(AnimationCurve curve, float normalizedSpeed)
    {
        normalizedSpeed = Mathf.Clamp(normalizedSpeed, curve.Evaluate(0), curve.Evaluate(1));
        float minTime = 0;
        float maxTime = 1;
        float midTime = minTime + (maxTime - minTime) / 2;
        float mid = curve.Evaluate(midTime);

        //binary search
        while (maxTime - minTime > SPEED_CURVE_PRECISION || Mathf.Abs(mid - normalizedSpeed) > SPEED_CURVE_PRECISION)
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

    bool FloatEquals(float a, float b, float treshold = float.Epsilon)
    {
        return a - treshold <= b && b <= a + treshold;
    }
}
