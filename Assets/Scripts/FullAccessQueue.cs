using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullAccessQueue<T>
{
    T[] items;
    int front;
    int rear;
    int count;

    public FullAccessQueue(int capacity)
    {
        items = new T[capacity];
        front = 0;
        rear = items.Length - 1;
        count = 0;
    }

    public void Enqueue(T item)
    {
        if(count < items.Length)
        {
            items[front] = item;
            front = (front + 1) % items.Length;
            count++;
        }
        else
        {
            //increase capacity

            int newArrayIndex = 0;
            T[] newArray = new T[items.Length * 2];
            for(int i = front; i < items.Length; i++)
            {
                newArray[newArrayIndex] = items[i];
                newArrayIndex++;
            }

            for (int i = 0; i < front; i++)
            {
                newArray[newArrayIndex] = items[i];
                newArrayIndex++;
            }

            items = newArray;
        }
    }

    public T Dequeue()
    {
        if(count > 0)
        {
            rear = (rear + 1) % items.Length;
            count--;
            return items[rear];
        }
        else
        {
            throw new InvalidOperationException("Queue is empty");
        }
    }

    public T Peek()
    {
        if (count > 0)
        {
            int nextRear = (rear + 1) % items.Length;
            return items[nextRear];
        }
        else
        {
            throw new InvalidOperationException("Queue is empty");
        }
    }

    public int Count => count;

    public T this[int index]
    {
        get
        {
            if(count > 0 && index > -1 && index < items.Length)
            {
                index = (index + 1 + rear) % items.Length;
                if(front - rear > 1)
                {
                    if(index > rear && index < front)
                    {
                        return items[index];
                    }
                }
                else
                {
                    if (index > rear || index < front)
                    {
                        return items[index];
                    }
                }
            }

            throw new IndexOutOfRangeException();
        }
        set
        {
            if (count > 0 && index > -1 && index < items.Length)
            {
                index = (index + 1 + rear) % items.Length;
                if (front - rear > 1)
                {
                    if (index > rear && index < front)
                    {
                        items[index] = value;
                    }
                }
                else
                {
                    if (index > rear || index < front)
                    {
                        items[index] = value;
                    }
                }
            }

            throw new IndexOutOfRangeException();
        }
    }

    public void Clear()
    {
        front = 0;
        rear = items.Length - 1;
        count = 0;
    }
}
