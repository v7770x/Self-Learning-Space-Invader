using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour {

    // Use this for initialization
    public float speed;
    private Rigidbody rb;
	void Start () {
        rb = GetComponent<Rigidbody>();
        rb.velocity = rb.transform.forward * speed;
        //Debug.Log(rb);
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
