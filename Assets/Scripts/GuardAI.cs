using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
public class GuardAI : MonoBehaviour
{
    public List<Transform> wayPoints;
    public NavMeshAgent ai;
    public Transform currentTarget;
    public int waypointsIndex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ai = GetComponent<NavMeshAgent>();

        if(wayPoints.Count > 0)
        {
            if(wayPoints[index: 0] != null)
            {
                currentTarget = wayPoints[index: 0];
            }
        }
        ai.destination = currentTarget.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (wayPoints.Count > 0 && currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);

            if (distance < 3f)
            {
                if ((waypointsIndex + 1) < wayPoints.Count)
                {
                    waypointsIndex++;
                    currentTarget = wayPoints[waypointsIndex];
                    ai.destination = currentTarget.position;
                }

               else if ((waypointsIndex + 1) == wayPoints.Count)
                {
                    waypointsIndex = 0;
                    currentTarget = wayPoints[waypointsIndex];
                    ai.destination = currentTarget.position;
                }
               
            }
        }
    }
}
