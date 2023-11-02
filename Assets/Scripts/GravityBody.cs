using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityBody : MonoBehaviour
{
    public GravitySource gravitySource;

    [HideInInspector]
    public Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.freezeRotation = true;
        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        gravitySource.Attract(this);
    }
}
