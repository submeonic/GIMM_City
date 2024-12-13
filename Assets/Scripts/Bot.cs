using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Bot : MonoBehaviour {

    public GameObject target;
    NavMeshAgent agent;

    void Start() {

        agent = GetComponent<NavMeshAgent>();
    }

    void Seek(Vector3 location) {

        agent.SetDestination(location);
    }

    void Flee(Vector3 location) {

        Vector3 fleeVector = location - transform.position;
        agent.SetDestination(transform.position - fleeVector);
    }

    void Pursue() {

        Vector3 targetDir = target.transform.position - transform.position;
        float relativeHeading = Vector3.Angle(transform.forward, transform.TransformVector(target.transform.forward));
        float toTarget = Vector3.Angle(transform.forward, transform.TransformVector(targetDir));
        float curSpeed = target.GetComponent<Drive>().currentSpeed;
        float lookAhead = targetDir.magnitude / (agent.speed + curSpeed);

        if ((toTarget > 40.0f && relativeHeading < 20.0f) || curSpeed < 0.01f) {

            // Debug.Log("SEEKING");

            Seek(target.transform.position + target.transform.forward * lookAhead);
            return;

        }

        // Debug.Log("LOOKING AHEAD");



    }

    void Update() {
        float distance = Vector3.Distance(transform.position, target.transform.position);
        //Seek(target.transform.position);
        // Flee(target.transform.position);
        if (distance < 50f)
        {
            Pursue();
        }
    }
}
