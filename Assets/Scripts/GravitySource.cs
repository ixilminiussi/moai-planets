using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GravitySource : MonoBehaviour
{
    public float gravity;
    public float rotationSpeed;

    public void Attract(GravityBody body)
    {
        Vector3 gravityUp = (body.transform.position - transform.position).normalized;

        body.rb.AddForce(gravityUp * gravity);

        Quaternion targetRotation = Quaternion.FromToRotation(body.transform.up, gravityUp) * body.transform.rotation;
        // body.transform.rotation = Quaternion.Slerp(body.transform.rotation, targetRotation, 500 * Time.deltaTime);
        body.transform.rotation = targetRotation;
    }
}
