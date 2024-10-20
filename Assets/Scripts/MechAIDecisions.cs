using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Panda;
using ProBuilder2.Common;
using Random = UnityEngine.Random;

public class MechAIDecisions : MechAI {

    public string botName = "Test Bot";

    //Links to Mech AI Systems
    MechSystems mechSystem;
    MechAIMovement mechAIMovement;
    MechAIAiming mechAIAiming;
    MechAIWeapons mechAIWeapons;

    //FSM State Implementation
    public enum MechStates {
        Roam,
        Attack,
        Pursue,
        Flee
    }
    public MechStates mechState;

    //Roam Variables
    public GameObject[] patrolPoints;
    private int patrolIndex = 0;
    public GameObject[] aimTargets;

    //Attack Variables
    private GameObject attackTarget;
    private float attackTime = 3.5f;
    private float attackTimer;

    //Pursue Variables
    public GameObject pursueTarget;
    private Vector3 pursuePoint;

    //Flee Variables
    public GameObject fleeTarget;
    
    //GameManager
    private GameManager gameManager;

    //Being attacked
    public bool beingAttacked;

    // Use this for initialization
    void Start () {
        //Collect Mech and AI Systems
        mechSystem = GetComponent<MechSystems>();
        mechAIMovement = GetComponent<MechAIMovement>();
        mechAIAiming = GetComponent<MechAIAiming>();
        mechAIWeapons = GetComponent<MechAIWeapons>();

        //Roam State Startup Declarations
        patrolPoints = GameObject.FindGameObjectsWithTag("Patrol Point");
        patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        
        //get game manager
        gameManager = GameObject.FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update() {

        //Acquire valid attack target: perform frustum and LOS checks and determine closest target
        mechAIAiming.FrustumCheck();

        if (!attackTarget) {
            attackTarget = mechAIAiming.ClosestTarget(mechAIAiming.currentTargets);
            mechAIWeapons.laserBeamAI = false;  //Hard disable on laserBeam
        }
        /*else
            FiringSystem();*/

        /*//FSM - Behaviour Selection
        switch (mechState) {
            case (MechStates.Roam):
                Roam();
            break;
            case (MechStates.Attack):
                Attack();
            break;
            case (MechStates.Pursue):
                Pursue();
            break;
            case (MechStates.Flee):
                Flee();
            break;
        }

        //FSM Transition Logic - Replace this with Decision Tree implementation!
        if (attackTarget && !mechAIAiming.LineOfSight(attackTarget) && !StatusCheck())
            mechState = MechStates.Pursue;
        else if (attackTarget && mechAIAiming.LineOfSight(attackTarget) && !StatusCheck())
            mechState = MechStates.Attack;
        else if (StatusCheck())
            mechState = MechStates.Flee;
        else
            mechState = MechStates.Roam;*/
    }

    [Task]
    bool HasAttackTarget()
    {
        if (attackTarget)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    [Task]
    bool TargetLOS()
    {
        if(mechAIAiming.LineOfSight(attackTarget))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    [Task]
    //FSM Behaviour: Roam - Roam between random patrol points
    private void Roam() {
        //Move towards random patrol point
        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f) {
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        }
        //if in line of sight check if patrol point has resource, otherwise choose new point
        else if (mechAIAiming.LineOfSight(patrolPoints[patrolIndex]))
        {
            RaycastHit[] hits = Physics.RaycastAll(mechAIAiming.rayCastPoint.transform.position,
                -(mechAIAiming.rayCastPoint.transform.position - patrolPoints[patrolIndex].transform.position).normalized, 
                //make sure raycast only goes as far as the patrol point of interest
                Vector3.Distance(mechAIAiming.rayCastPoint.transform.position, patrolPoints[patrolIndex].transform.position));

            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    //if there is a resource pack
                    if (hits[i].transform.GetComponent<Pickup>())
                    {
                        mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
                        //Look at random patrol points
                        mechAIAiming.RandomAimTarget(patrolPoints);
                        return;
                    }
                }
            }
            //choose a different point if there is no resource pack
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        }
        else {
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        }
        //Look at random patrol points
        mechAIAiming.RandomAimTarget(patrolPoints);
    }

    [Task]
    //FSM Behaviour: Attack 
    private void Attack() {
         
        //If there is a target, set it as the aimTarget 
        if (attackTarget && mechAIAiming.LineOfSight(attackTarget)) {
            //There is a 25% chance the mech just runs away
            float rnd = Random.Range(0, 1);
            if (rnd < 0.25)
            {
                mechState = MechStates.Flee;
            }
            else
            {
                //Child object correction - wonky pivot point
                mechAIAiming.aimTarget = attackTarget.transform.GetChild(0).gameObject;

                //Move Towards attack Target
                Vector3 wonky = new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1)); //variation in movement
                if (Vector3.Distance(transform.position, attackTarget.transform.position) >= 45.0f) {
                    mechAIMovement.Movement(attackTarget.transform.position + wonky, 45);
                }
                //Otherwise "strafe" - move towards random patrol points at intervals
                else if (Vector3.Distance(transform.position, attackTarget.transform.position) < 45.0f && Time.time > attackTimer) {
                    patrolIndex = Random.Range(0, patrolPoints.Length - 1);
                    mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 2);
                    attackTimer = Time.time + attackTime + Random.Range(-0.5f, 0.5f);
                }

                //Track position of current target to pursue if lost
                pursuePoint = attackTarget.transform.position;
            }
        }
    }

    [Task]
    //FSM Behaviour: Pursue
    void Pursue() {
        //Move towards last known position of attackTarget
        if (Vector3.Distance(transform.position, pursuePoint) > 3.0f) {
            mechAIMovement.Movement(pursuePoint, 5); //keep distance
            mechAIAiming.RandomAimTarget(patrolPoints);
        }
        //Otherwise if reached and have not re-engaged, reset attackTarget and Roam
        else {
            attackTarget = null;
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
            mechState = MechStates.Roam;
        }
    }

    [Task]
    //FSM Behaviour: Flee
    void Flee() {

        //If there is an attack target, set it as the aimTarget 
        if (attackTarget && mechAIAiming.LineOfSight(attackTarget)) {
            //Child object correction - wonky pivot point
            mechAIAiming.aimTarget = attackTarget.transform.GetChild(0).gameObject;
        } else {
            //Look at random patrol points
            mechAIAiming.RandomAimTarget(patrolPoints);
        }

        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f)
        {
            //choose closest patrol point that is out of sight of the attackTarget
            if (attackTarget)
            {
                Array.Sort(patrolPoints, (a, b) =>
                {
                    return (int)(Vector3.Distance(b.transform.position, transform.position) - Vector3.Distance(a.transform.position, transform.position));
                });

                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (LineOfSight(patrolPoints[i].transform.position, attackTarget))
                    {
                        patrolIndex = i;
                        break;
                    }
                }
            }
            else
            {
                patrolIndex = Random.Range(0, patrolPoints.Length - 1);
            }
        }
        else
        {
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        }
        
        /*//Move towards random patrol points <<< This could be drastically improved!
        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f) {
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        } else {
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        }*/
    }

    //Method to determine if object is within LOS of Mech
    private bool LineOfSight(Vector3 position, GameObject thisTarget) {

        //Need to correct for wonky pivot point - Mech model pivot at base instead of centre
        Vector3 correction = thisTarget.transform.GetChild(0).gameObject.transform.position;

        RaycastHit hit;
        if (Physics.Raycast(position, -(position - correction).normalized, out hit, 100.0f)) {

            Debug.DrawLine(position, hit.point, Color.red);
            //if the raycasthit travelled further than the proposed position, there is a clear line of sight
            if (hit.distance > Vector3.Distance(position, thisTarget.transform.position))
            {
                return true;
            }
            else
                return false;
        }
        else
            return false;
    }

    //Method allowing AI Mech to acquire target after taking damage from enemy
    public override void TakingFire(int origin) {

        //If not own damage and no current attack target, find attack target
        if (origin != mechSystem.ID && !attackTarget) {
            foreach (GameObject target in mechAIAiming.targets) {
                if (target) {
                    if (origin == target.GetComponent<MechSystems>().ID) {
                        attackTarget = target;
                        mechAIAiming.aimTarget = target;
                    }
                }
            }
        }
    }

    [Task]
    //Method for checking heuristic status of Mech to determine if Fleeing is necessary
    private bool StatusCheck()
    {

        float status = 0;
        int attacked = 1;
        
        //number of deaths and score variables
        int score = gameManager.playerScores[mechSystem.ID];
        int deaths = gameManager.playerDeaths[mechSystem.ID];
        
        if (attackTarget)
        {
            status += Vector3.Distance(transform.position, attackTarget.transform.position); //more likely to attack when further away
            if(attackTarget.GetComponent<MechSystems>().health < 1000) // more likely to attack when the attackTarget is low on health
            {
                status *= 1.5f;
            }
            attacked = 2; //the threshold to start attacking is higher if there is an attack target -> fear factor
        }
        
        status += (mechSystem.health * 0.5f) + mechSystem.energy + (mechSystem.shells * 3) +
                 (mechSystem.missiles * 5) - deaths + (score * 2); //less likely to attack if it has already died
        

        if (status > 1500 * attacked)
            return false;
        else
            return true;
    }

    [Task]
    private void Laser()
    {
        //Lasers - Enough energy and within a generous firing angle
        if (mechSystem.energy > 10 && mechAIAiming.FireAngle(20))
            mechAIWeapons.Lasers();
    }

    [Task]
    private void Cannon()
    {
        //Cannons - Moderate distance, enough shells and tight firing angle
        if (Vector3.Distance(transform.position, attackTarget.transform.position) > 25
            && mechSystem.shells > 4 && mechAIAiming.FireAngle(15))
            mechAIWeapons.Cannons();
    }

    [Task] 
    private void LaserBeam()
    {
        //Laser Beam - Strict range, plenty of energy and very tight firing angle
        if (Vector3.Distance(transform.position, attackTarget.transform.position) > 20
            && Vector3.Distance(transform.position, attackTarget.transform.position) < 50
            && mechSystem.energy >= 300 && mechAIAiming.FireAngle(10))
            mechAIWeapons.laserBeamAI = true;
        else
            mechAIWeapons.laserBeamAI = false;
    }

    [Task]
    private void MissileArray()
    {
        //Missile Array - Long Range, enough ammo, very tight firing angle
        if (Vector3.Distance(transform.position, attackTarget.transform.position) > 50
            && mechSystem.missiles >= 18 && mechAIAiming.FireAngle(5))
            mechAIWeapons.MissileArray();
    }

    [Task]
    //Method that handles head movement
    private void Swivel()
    {
        mechAIAiming.RandomAimTarget(patrolPoints);
    }

}
