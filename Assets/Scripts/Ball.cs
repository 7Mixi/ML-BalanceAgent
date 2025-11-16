using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Ball : MonoBehaviour
{
    [SerializeField] GameController controller;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Target") && !controller.inTarget)
        {
            controller.inTarget = true;
            //Debug.Log("Inside target");

        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Target") && !controller.inTarget)
        {
            controller.inTarget = false;
            //Debug.Log("Out of target");
        }
    }
}
