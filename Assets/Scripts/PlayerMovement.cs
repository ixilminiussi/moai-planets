using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float runSpeed = 20;
    public float walkSpeed = 7;
    public float acceleration = 10;
    public float deceleration = 2;
    public float rotationSpeed = 4;


    float moveSpeed = 0f;

    [Header("Ground Check")]
    public float snapDistance;
    public float playerHeight;
    public LayerMask Ground;
    bool grounded;

    public Transform orientation;
    float horizontalInput;
    float verticalInput;
    bool runInput;


    public Transform playerObj;


    enum Movement
    {
        Walking,
        Running,
        Standing
    }

    Movement movementState = Movement.Standing;

    Vector3 moveDirection;

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f, Ground);

        MyInput();
        SpeedControl();
    }

    private void FixedUpdate()
    {
        MovePlayer();
        SnapDown();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        runInput = Input.GetButton("Run");

        if (horizontalInput == 0f && verticalInput == 0f)
        {
            movementState = Movement.Standing;
        }
        else
        {
            if (runInput)
            {
                movementState = Movement.Running;
            }
            else
            {
                movementState = Movement.Walking;
            }
        }
    }

    private void MovePlayer()
    {
        Vector3 goalDirection;

        if (movementState == Movement.Standing)
        {
            goalDirection = Vector3.zero;
        }
        else
        {
            goalDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        }

        moveDirection = Vector3.Slerp(moveDirection, goalDirection, rotationSpeed * Time.deltaTime);

        rb.MovePosition(rb.position + moveDirection * Time.deltaTime * moveSpeed);
    }

    private void SnapDown() {
        RaycastHit hit;

        if (Physics.Raycast(transform.position - (transform.up * playerHeight / 2), -transform.up, out hit, snapDistance, Ground)) {
            rb.position = rb.position - transform.up * hit.distance;
        }
    }

    private void SpeedControl()
    {
        switch (movementState)
        {
            case Movement.Standing:
                moveSpeed = Mathf.Lerp(moveSpeed, 0, deceleration * Time.deltaTime);
                break;
            case Movement.Walking:
                if (moveSpeed > walkSpeed)
                {
                    moveSpeed = Mathf.Lerp(moveSpeed, walkSpeed, deceleration * Time.deltaTime);
                }
                else
                {
                    moveSpeed = Mathf.Lerp(moveSpeed, walkSpeed, acceleration * Time.deltaTime);
                }
                break;
            case Movement.Running:
                moveSpeed = Mathf.Lerp(moveSpeed, runSpeed, acceleration * Time.deltaTime);
                break;
        }
    }
}
