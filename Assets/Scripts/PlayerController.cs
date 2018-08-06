using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CSML;


[System.Serializable]
public class Boundary
{
    public float xMin, xMax, zMin, zMax;
}

public class PlayerController : MonoBehaviour {

    // Use this for initialization
    private Rigidbody rb;
    public float speed, tilt;
    public Boundary boundary;
    public GameObject shot;
    public Transform shotSpawn;

    public float fireRate;
    private float nextFire;

    //keep track of score
    public float score, time0;
    public Matrix th1, th2, th3;
    public int id;

    //variables for radius and view range
    private float view_dist, asteroid_rad;
    private int num_angles;

	void Start () {
        rb = GetComponent<Rigidbody>();
        score = 0;
        time0 = Time.time;
        view_dist = 15;
        asteroid_rad = 0.5f;
        num_angles = 5;

	}

    void Update()
    {
        score = Time.time -time0;
        if (Input.GetButton("Fire1") && Time.time > nextFire)
        {
            Instantiate(shot, shotSpawn.position, shotSpawn.rotation);
            nextFire = Time.time + fireRate;
            GetComponent<AudioSource>().Play();
        }
        //Debug.Log(findInputs());
        float h_speed = (float) forwardProp(findInputs())[1,1].Re;
        Debug.Log(h_speed);
        movePlayer(new Vector3(h_speed, 0, 0));

    }


    void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        //movePlayer(movement * speed);

    }

    void movePlayer(Vector3 velocity)
    {
        rb.velocity = velocity*speed;
        rb.position = new Vector3(
            Mathf.Clamp(rb.position.x, boundary.xMin, boundary.xMax),
            0.0f,
            Mathf.Clamp(rb.position.z, boundary.zMin, boundary.zMax)
        );
        rb.rotation = Quaternion.Euler(0.0f, 0.0f, rb.velocity.x * -tilt);
    }

    Matrix findInputs()
    {
        double [,] input_vals = new double[num_angles,1];
        GameObject[] asteroids = GameObject.FindGameObjectsWithTag("Asteroid");
        Vector3 playerPos = transform.position;
        if (asteroids.Length != 0)
        {
            for (int i = 0; i < num_angles; i++)
            {
                float theta_deg = i * (180 - 40) * 1.0f / num_angles;
                float min_dist = -1;
                foreach (GameObject asteroid in asteroids)
                {
                    Vector3 asteroidPos = asteroid.transform.position;
                    float dist = intersectionDistance(theta_deg, playerPos.x, playerPos.z, asteroidPos.x, asteroidPos.z);
                    if (dist < min_dist || min_dist == -1)
                    {
                        min_dist = dist;
                    }
                }
                input_vals[i, 0] = (double)map_dist(min_dist);
            }
            Matrix inputs = new Matrix(input_vals);
            return inputs;
        }
        else
        {
            for (int i = 0; i < input_vals.Length; i++)
                input_vals[i, 0] = 0;
            return new Matrix(input_vals);
        }
       
    }

    float intersectionDistance(float theta_deg, float x0, float z0, float x, float z)
    {
        float theta_rad = theta_deg / 180 * Mathf.PI;
        float predicted_z = applyLinear(theta_rad, x0, x, z0);
        float dist = Mathf.Sqrt(Mathf.Pow(x - x0, 2) + Mathf.Pow(z - z0, 2));
        if(Mathf.Abs(predicted_z-z)<asteroid_rad && dist < view_dist)
        {
            return dist;
        }
        else
        {
            return 1000;
        }
    }

    float map_dist(float dist)
    {
        return 5.0f / dist;
    }

    //returns the predicted output of a point x units away from the original coordinates in direction theta
    float applyLinear(float theta_rad, float x0, float x, float z0)
    {
        float z = Mathf.Tan(theta_rad) * (x - x0) + z0;
        return z;
    }

    //apply forward propagation using the 2 matricies
    Matrix forwardProp(Matrix inputs)
    {
        //if(id==1)
        //    Debug.Log("inputs: " + inputs);
        inputs.InsertRow(Matrix.Identity(1), 1);
        Matrix hiddenLayer1 = (th1 * inputs);
        
        hiddenLayer1.InsertRow(Matrix.Identity(1), 1);
        Matrix hiddenLayer2 = th2 * hiddenLayer1;

        hiddenLayer2.InsertRow(Matrix.Identity(1), 1);
        Matrix output = th3 * hiddenLayer2;
        //if (id == 1)
        //    Debug.Log(output);
        //Debug.Log(hiddenLayer + " output:" + output);
        return output;
    }
}
