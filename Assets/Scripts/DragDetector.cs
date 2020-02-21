using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DragDetector : MonoBehaviour, IDragHandler
{
    public event UnityAction<Vector2> OnDrag;

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, 0));
        OnDrag?.Invoke(worldPosition);
    }
}
