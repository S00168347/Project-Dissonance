﻿using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
// This script will only work if our enemy has an Agent component.
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    #region Enemy Properties
    // Properties that can be altered in the Unity inspector. Some of these might be moved to other scripts for the sake of cleanliness.
    public int Health;
    public float shootCooldown, walkSpeed, turningSpeed, stoppingDistance, haltTime;
    public Light Flashlight;

    // This camera is used to determine where the user has clicked on-screen. It'll be removed when disturbance investigation testing is over.
    public Camera disturbanceCam;
    // This variable will keep track of the object that you clicked.
    public RaycastHit hit;
    // Bool used to keep track of whether the enemy's halted. May be replaced with enum.
    private bool halted;
    // This variable stores where the agent was originally headed.
    private Vector3 originalDestination;

    //Footsteps footSteps; We can add FMOD SFX later.
    private bool alerted = false;

    // Properties that are automatically set when the object is created.
    NavMeshAgent agent;
    Patrol patrolRoute;
    Vector3 post;
    EnemyMovement movement;
    TimeManager tm = new TimeManager();
    #endregion

    public enum Phase
    {
        PATROL,
        VIGIL,
        HALT,
        INVESTIGATE,
        ALERT,
        DECEASED
    }

    [SerializeField]
    public Phase enemyPhase, previousPhase;

    //private Phase previousPhase;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;

        //footSteps = GetComponent<Footsteps>();

        movement = GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        #region Set the enemy's patrol route, if we haven't given them one, assign them a post.
        // Set the enemy's patrol route if we have given them one in the editor.

        // The following commented if statement is an alternate choice to checking if the component is there. The uncommented one is what we'll use.
        // if (GetComponent<Patrol>().enabled != false)
        if (GetComponent<Patrol>() != null)
        {
            patrolRoute = GetComponent<Patrol>();
            // NOTE: We can only set enemy phase directly (Like the line below.) in this Start method. Everywhere else, we should use the SetPhase(newPhase) method.
            enemyPhase = Phase.PATROL;
            UpdateBehaviour();
        }

        // If we haven't given them a patrol route, assign their starting location as their post. (An area they've been assigned to keep watch at.)
        else
        {
            post = GetComponent<Transform>().position;
            // REMINDER: Everywhere else, we use the SetPhase(newPhase) method.
            enemyPhase = Phase.VIGIL;
            UpdateBehaviour();
        }
        #endregion
    }

    void Update()
    {
        if (enemyPhase != previousPhase)
        {
            UpdateBehaviour();
        }

        // The enemy will always check for disturbances regardless of its current phase.
        CheckForDisturbances();

        // Probably best not to have this in the update, but we can leave refactoring to later.
        if (enemyPhase == Phase.HALT)
        {
            // After a brief amount of time has passed, the enemy will move into the INVESTIGATE phase.
            if (tm.TimeCount(haltTime))
            {
                SetPhase(Phase.INVESTIGATE);
            }
        }       
    }

    void UpdateBehaviour()
    {
        previousPhase = enemyPhase; // Ensure that enemyPhase and previousPhase are once again in sync.

        switch (enemyPhase)
        {
            case Phase.PATROL: // TO FIX: Enemy speed issue here. Movement speed above 4 causes enemy to stop. Tweak REST_DISTANCE on the Patrol script.                             
                Flashlight.color = Color.white;
                patrolRoute.StartPatrol();
                break;

            case Phase.VIGIL:
                break;

            case Phase.HALT:               
                Flashlight.color = Color.yellow;

                #region If the enemy was on patrol...
                if (agent.hasPath)
                {
                    // Keep track of where the enemy was originally headed.
                    originalDestination = agent.destination;
                    // ...stop the enemy's patrol.
                    patrolRoute.StopPatrol();
                    // Clear the agent's path.
                    agent.ResetPath();
                }

                // The enemy's movement (independent of any patrol routes) will begin now.
                movement.enabled = true;
                #endregion
       
                // Turn towards the direction of the disturbance.
                movement.SetRotationTarget(hit.point);
                break;

            case Phase.INVESTIGATE:
                // Set the disturbance location as the enemy's destination.                 
                movement.SetWalkTarget(hit.point);
                break;

            case Phase.ALERT:
                // Change light colour.
                Flashlight.color = Color.red;                            
                break;

            case Phase.DECEASED:
                Flashlight.enabled = false;
                break;
        }
    }

    void SetPhase(Phase newPhase)
    {
        if (newPhase != enemyPhase)
        {           
            previousPhase = enemyPhase;
            Debug.Log("Previous phase: " + previousPhase);
            enemyPhase = newPhase;
            Debug.Log("New phase: " + newPhase);           
            //UpdateBehaviour();
        }
    }

    private void CheckForDisturbances()
    {
        if (Input.GetMouseButtonDown(0) && disturbanceCam != null)
        {
            // Send out a ray to the position where you clicked.
            Ray ray = disturbanceCam.ScreenPointToRay(Input.mousePosition);

            // This if will determine whether or not you clicked an object.
            if (Physics.Raycast(ray, out hit))
            {
                SetPhase(Phase.HALT);
            }
        }
    }
}