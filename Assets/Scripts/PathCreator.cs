using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PathCreator : MonoBehaviour, IPointerClickHandler
{
    public CharacterMotion motion;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(motion != null)
        {
            motion.AddTargetPos(eventData.position);
        }
    }
}
