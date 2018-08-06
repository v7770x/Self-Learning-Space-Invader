using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CSML;

public class DestroyByContact : MonoBehaviour {

    public GameObject playerExplosion, asteroidExplosion;
    private GameController gameController;
    void Start()
    {
        GameObject gameControllerObject = GameObject.FindWithTag("GameController");
        if (gameControllerObject != null)
        {
            gameController = gameControllerObject.GetComponent<GameController>() ;
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Boundary")
            return;
        Instantiate(asteroidExplosion, other.transform.position, other.transform.rotation);
        if (other.tag == "Player")
        {
            //Instantiate(playerExplosion, other.transform.position, other.transform.rotation);

            //get the player properties, record the death
            PlayerController ot = other.gameObject.GetComponent<PlayerController>();
            gameController.recordDeath(ot.id, ot.score);

            //check if no more players and continue with selection and mutation 
            //gameController.checkEndGame();
        }
        
        Destroy(other.gameObject);
        //Destroy(gameObject);
    }
}
