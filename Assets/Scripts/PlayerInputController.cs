using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    DragDetector pathDetector;
    float startTime;
    Vector2 lastPosition = new Vector2(float.MaxValue, float.MaxValue);
    [SerializeField] float positionGap = 0.5f;
    [SerializeField] CharacterMotion motion;

    // Start is called before the first frame update
    void Start()
    {
        pathDetector = FindObjectOfType<DragDetector>();
        pathDetector.OnDrag += OnDrag;
        startTime = Time.time;
    }

    void OnDrag(Vector2 worldPosition)
    {
        if(Vector2.Distance(worldPosition, lastPosition)> positionGap)
        {
            motion.AddTargetPos(worldPosition);
            lastPosition = worldPosition;
        }
    }
}
