using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelativeMovement : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform player;
    public Transform playerObj;
    public Rigidbody rb;

    public float rotationSpeed = 4;

    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update() {
        Vector3 thisRelPosition = player.InverseTransformPoint(transform.position);

        Vector3 viewDir = player.TransformDirection(new Vector3(-thisRelPosition.x, 0, -thisRelPosition.z).normalized);

        // Rotate reference point
        orientation.rotation = Quaternion.LookRotation(viewDir, player.up);

        // Rotate player object
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 inputDir = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (inputDir != Vector3.zero) {
            Quaternion toRotation = Quaternion.LookRotation(inputDir, player.up);
            playerObj.rotation = Quaternion.Slerp(playerObj.rotation, toRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
