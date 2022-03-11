using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EpeeControl : MonoBehaviour
{
    public float velocity = 10f;

    void Update()
    {
        Vector3 movement = Vector3.zero;
        if (Input.GetKey(KeyCode.RightArrow))
        {
            movement += Vector3.right;
        } 
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            movement += Vector3.left;
        } 
        if (Input.GetKey(KeyCode.UpArrow))
        {
            movement += Vector3.up;
        } 
        if (Input.GetKey(KeyCode.DownArrow))
        {
            movement += Vector3.down;
        }

        if (movement.magnitude > 0)
        {
            movement = movement.normalized * velocity * Time.deltaTime;
            transform.position += movement;
        }
    }
}
