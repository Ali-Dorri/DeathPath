using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Position Pool", menuName = "Position Pool")]
public class PositionPool : MonoBehaviour
{
    [SerializeField] GameObject positionPrefab;
    Stack<Transform> pool;
    [SerializeField] int startSize = 100;
    static PositionPool instance;

    public static PositionPool Instance
    {
        get
        {
            if(instance == null)
            {
                instance = FindObjectOfType<PositionPool>();
                instance.Initialize();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            Initialize();
        }
    }

    private void Initialize()
    {
        pool = new Stack<Transform>(startSize);
        for (int i = 0; i < startSize; i++)
        {
            pool.Push(CreatePosition());
        }
    }

    Transform CreatePosition()
    {
        GameObject position = Instantiate(positionPrefab);
        position.hideFlags = HideFlags.HideInHierarchy;
        position.SetActive(false);
        return position.transform;
    }


    public Transform GetPosition()
    {
        Transform position;
        if (pool.Count > 0)
        {
            position = pool.Pop();
        }
        else
        {
            position = CreatePosition();
        }

        return position;
    }

    public void DestroyPosition(Transform position)
    {
        position.gameObject.SetActive(false);
        pool.Push(position);
    }

    void OnDestroy()
    {
        if(instance == this)
        {
            instance = null;
        }
    }
}
