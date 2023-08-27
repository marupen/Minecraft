using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{

    public float horizontalSpeed = 10.0f;
    public float verticalSpeed = 10.0f;
    private float translation;
    private float straffe;
    private Rigidbody rgdbd;

    // Use this for initialization
    void Start()
    {
        // turn off the cursor
        Cursor.lockState = CursorLockMode.Locked;

        rgdbd = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // Input.GetAxis() is used to get the user's input
        // You can furthor set it on Unity. (Edit, Project Settings, Input)
        translation = Input.GetAxis("Vertical") * horizontalSpeed * Time.deltaTime;
        straffe = Input.GetAxis("Horizontal") * horizontalSpeed * Time.deltaTime;
        transform.Translate(straffe, 0, translation);

        if (Input.GetKeyDown("escape"))
        {
            // turn on the cursor
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            rgdbd.velocity = new Vector3(rgdbd.velocity.x, verticalSpeed, rgdbd.velocity.z);
        }
    }
}