﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TokenHandler : MonoBehaviour {
	public NavMeshAgent navMeshAgent;
	public List<Transform> pathSteps;
	public int playerNumber;
	public bool isSelected;
	public GameObject activationIndicator;
	public bool winner = false;
	public float stoppingDistance = 0.5f;
    public int[] safeSpaces;

	public int destPoint = 0; // the current destination of the token
	private int nextStep = 0; // the next step toward destPoint
	private bool hasStarted = false;
	private GameObject gameController;

	// Use this for initialization
	void Start () {
        this.navMeshAgent = gameObject.GetComponent<NavMeshAgent>();
		GameObject pathObject = GameObject.Find("Pathway_Player" + this.playerNumber);
		if (pathObject)
		{
			PathwayHandler pathwayHandler = pathObject.GetComponent<PathwayHandler>();
			this.pathSteps = pathwayHandler.stops;
			CheckCurrentTarget();
		}
		gameController = GameObject.FindGameObjectsWithTag("GameController")[0];
	}
	
	// Update is called once per frame
	void Update () {
		// Choose the next destination point when the agent gets
		// close to the current one.
		if (!hasStarted)
		{
			if (isSelected)
			{
                this.navMeshAgent.isStopped = false;
				destPoint = 1; // player is selected, place them at the start
				hasStarted = true;
			}
			else
			{
				destPoint = 0; // player is not selected, place them in the waiting area
			}
		}

		// If this token is selected, we can move!
		if (this.isSelected)
		{
			if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < stoppingDistance)
				CheckCurrentTarget();

			activationIndicator.SetActive(true);
			if (Input.GetMouseButtonDown(0))
			{
				RaycastHit hit;

				if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100))
				{
					ActivateClickableObject(hit.point);
				}
			}
		} else
		{
			activationIndicator.SetActive(false);
		}

		// If this token has reached the end, move it to the winner's area
		if (winner)
		{
			// If we've reached the winner's area, stop navigation and celebrate!
			if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < stoppingDistance)
			{
				navMeshAgent.isStopped = true;
			}
			else
			{
				navMeshAgent.destination = pathSteps[pathSteps.Count - 1].position;
			}
		}
	}

	void ActivateClickableObject(Vector3 point)
	{
		int target = GetClosestObjectID(this.pathSteps.ToArray(), point);
		Debug.Log("User clicked close to " + target);
		//navMeshAgent.SetDestination(target.transform.position);
		int difference = target - destPoint;
		if (difference == gameController.GetComponent<GameController>().diceValue)
		{
            // Find all token objects
            GameObject[] tokenObjects = GameObject.FindGameObjectsWithTag("Token");
            // Check if another piece occupies that space
            GameObject otherObject = GetClosestGameObject(tokenObjects, point, 0.6f);

            if (otherObject != null)
            {
                Debug.Log("Clicked on a token!");
                // If the other object is the player's own token, don't allow the move
                if (otherObject.GetComponent<TokenHandler>().playerNumber == this.playerNumber)
                {
                    Debug.Log("The other token is your own. You can't move there!");
                } else
                {
                    Debug.Log("The other token is the opposite player's!");
                    // Check if the other player's piece is on a safe space
                    if (otherObject.GetComponent<TokenHandler>().IsOnSafeSpace())
                    {
                        Debug.Log("Opponent on safe space");
                    } else
                    {
                        Debug.Log("Opponent is not safe! They are knocked back to the start.");
                        otherObject.GetComponent<TokenHandler>().KnockBack();
                    }
                }
            }
            else
            {
                destPoint = target;
            }
		} else
		{
			Debug.Log("You can't move there!");
		}
	}

    bool IsOnSafeSpace()
    {
        bool status = false;
        foreach(int safeSpace in this.safeSpaces)
        {
            if (this.destPoint == safeSpace) status = true;
        }

        return status;
    }

    // Reset this token to the start
    public void KnockBack()
    {
        this.destPoint = 0;
        this.nextStep = 0;
        this.transform.position = this.pathSteps[nextStep].position;
        this.navMeshAgent.isStopped = true;
    }

    GameObject GetClosestGameObject(GameObject[] otherTransforms, Vector3 point, float maxDistance)
    {
        GameObject closestTarget = null;
        // Set the initial closest distance really high. We don't want to return null.
        float closestDistance = 1000;

        for (int i = 0; i < otherTransforms.Length; i++)
        {
            float distanceFromTarget = Vector3.Distance(point, otherTransforms[i].transform.position);
            if (distanceFromTarget < closestDistance)
            {
                // We have a new closest target.
                closestTarget = otherTransforms[i];
                closestDistance = distanceFromTarget;
            }
        }
        if (closestDistance < maxDistance)
        {
            return closestTarget;
        }
        else
        {
            return null;
        }
    }

    int GetClosestObjectID(Transform[] otherTransforms, Vector3 point)
	{
		int closestTarget = -1;
		// Set the initial closest distance really high. We don't want to return null.
		float closestDistance = 1000;

		for (int i = 0; i < otherTransforms.Length; i++)
		{
			float distanceFromTarget = Vector3.Distance(point, otherTransforms[i].position);
			if (distanceFromTarget < closestDistance)
			{
				// We have a new closest target.
				closestTarget = i; // otherTransforms[i].gameObject;
				closestDistance = distanceFromTarget;
			}
		}

		return closestTarget;
	}


	public void CheckCurrentTarget()
	{
		// Returns if no points have been set up
		if (pathSteps.Count == 0)
			return;

		// Set the agent to go to the currently selected destination.
		navMeshAgent.destination = pathSteps[nextStep].position;

		// Choose the next point in the array as the destination,
		// cycling to the start if necessary.
		// destPoint = (destPoint + 1) % pathSteps.Count;

		// If the target is before the current step, 
		// or it's the end, set it to the winning circle
		if (!winner && (destPoint < nextStep || nextStep >= pathSteps.Count - 2))
		{
			//navMeshAgent.isStopped = true;
			//transform.position = pathSteps[pathSteps.Count - 1].position;
			//transform.rotation = pathSteps[pathSteps.Count - 1].rotation;
			destPoint = pathSteps.Count - 1;
			this.isSelected = false;
			this.winner = true;
			this.GetComponentInParent<PlayerHandler>().IncrementPoints();
		}

		// If the target is ahead, keep progressing to the next step.
		if (destPoint > nextStep)
		{
			nextStep = (nextStep + 1) % pathSteps.Count;
		}
	}
}
