using UnityEngine;
using MachineLearning.Classes;
using System.Collections.Generic;
using System;

public class Dot : MonoBehaviour {
	List<int> topology;
	public int score = 0;

	NeuralNetwork nn;

	Rigidbody2D player;
	public float speed = 10;

	public int epoch = 0;

	double distanceToTop;
	double distanceToRight;
	double distanceToBottom;
	double distanceToLeft;

	public float maxXDistance = 8;
	public float minXDistance = -8;
	public float maxYDistance = 5;
	public float minYDistance = -5;


	int populationSize = 10;
	public int networkIndex = 0;
	List<int> scores;
	public List<NeuralNetwork> networks;
	public double weight;
	private System.Random random = new System.Random();

	DateTime trialStartTime;

	public GameObject prefab;

	// Use this for initialization
	void Start () {

		for (int i = 0;i < 20;i++)
		{
			Vector3 position = new Vector3(UnityEngine.Random.Range(minXDistance, maxXDistance), UnityEngine.Random.Range(minYDistance, maxYDistance), -1);
			Instantiate(prefab, position, Quaternion.identity);
		}

		topology = new List<int>();
		//Distance U, R, D, L to the walls
        //X and Y values of closest candy
		topology.Add(6);
		//Hidden Layers
		topology.Add(5);
		//X and Y outputs
		topology.Add(4);

		scores = new List<int>();
        networks = new List<NeuralNetwork>();

		for (int i = 0;i < populationSize;i++)
		{
			NeuralNetwork n = new NeuralNetwork(topology, random);
			n.randomizeWeights();
			n.printToConsole();
			networks.Add(n);
			scores.Add(0);
		}

		nn = networks[0];
		weight = networks[0].getWeightMatrix(0).getValue(0, 0);
		player = GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void Update ()
	{
		DateTime now = DateTime.Now;
		TimeSpan t = now - trialStartTime;
		if (t.TotalMilliseconds > 30000)
		{
			score -= 10;
			CollisionOrTimeout();
		}

		GameObject wall_top = GameObject.FindGameObjectWithTag("wall-top");
		GameObject wall_right = GameObject.FindGameObjectWithTag("wall-right");
		GameObject wall_bottom = GameObject.FindGameObjectWithTag("wall-bottom");
		GameObject wall_left = GameObject.FindGameObjectWithTag("wall-left");

        distanceToTop = wall_top.transform.position.y - player.position.y;
        distanceToRight = wall_right.transform.position.x - player.position.x;
        distanceToBottom = wall_bottom.transform.position.y - player.position.y;
        distanceToLeft = wall_left.transform.position.x - player.position.x;

        List<GameObject> foodsList = new List<GameObject>(GameObject.FindGameObjectsWithTag("food"));


        float foodX = foodsList[0].transform.position.x;
        float foodY = foodsList[0].transform.position.y;
        float playerX = player.position.x;
        float playerY = player.position.y;

        double minDistance = Math.Sqrt(Math.Pow(foodX - playerX, 2) + Math.Pow(foodY - playerY, 2));
        int minIndex = 0;
        for (int i = 1; i < foodsList.Count;i++) 
        {
            foodX = foodsList[i].transform.position.x;
            foodY = foodsList[i].transform.position.y;

            double currentDistance = Math.Sqrt(Math.Pow(foodX - playerX, 2) + Math.Pow(foodY - playerY, 2));

            if (currentDistance < minDistance)
            {
                minDistance = currentDistance;
                minIndex = i;
            }
        }

        double closestFoodX = foodsList[minIndex].transform.position.x;
        double closestFoodY = foodsList[minIndex].transform.position.y;

		//Debug.Log("Top: " + distanceToTop + ", " + "Right: " + distanceToRight + ", " + "Bottom: " + distanceToBottom + ", " + "Left: " + distanceToLeft);

		//Feed NN inputs
		List<double> inputs = new List<double>();
		inputs.Add(distanceToTop);
		inputs.Add(distanceToBottom);
		inputs.Add(distanceToRight);
		inputs.Add(distanceToLeft);
        inputs.Add(closestFoodX);
        inputs.Add(closestFoodY);

        //Debug.Log("Closest Food: (" + closestFoodX + ", " + closestFoodY + ")");

		//Set input and Feed Forward
		nn.setCurrentInput(inputs);
		nn.feedForward();

		//Get Inputs
		List<double> outputs = nn.getOutputValues();

		float moveX, moveY;

		if (outputs[0] > outputs[1])
		{
            moveX = (float)Math.Abs(outputs[0]) * speed;
		}
		else
		{
            moveX = -1*(float)Math.Abs(outputs[1]) * speed;
		}

		if (outputs[2] > outputs[3])
		{
            moveY = (float)Math.Abs(outputs[2]) * speed;
		}
		else
		{
            moveY = -1 * (float)Math.Abs(outputs[3]) * speed;
		}

		player.velocity = new Vector2(moveX, moveY);
	}
	
	void OnCollisionEnter2D(Collision2D col)
	{
		if (col.gameObject.tag == "food")
		{
			Destroy(col.gameObject);

			Vector3 position = new Vector3(UnityEngine.Random.Range(minXDistance, maxXDistance), UnityEngine.Random.Range(minYDistance, maxYDistance), -1);
			Instantiate(prefab, position, Quaternion.identity);

			score += 100;
		}

		if (col.gameObject.tag == "wall-top" ||
			col.gameObject.tag == "wall-right" ||
			col.gameObject.tag == "wall-bottom" ||
			col.gameObject.tag == "wall-left")
		{
            score -= 100;
			CollisionOrTimeout();
		}
	}

	void CollisionOrTimeout()
	{
		scores[networkIndex] = score;
		score = 0;
		if (networkIndex == populationSize - 1)
		{
			epoch++;
			enabled = false;
			EvolveNetwork();
			enabled = true;
			networkIndex = 0;
			nn = networks[networkIndex];
			weight = networks[networkIndex].getWeightMatrix(0).getValue(0, 0);
		}
		else
		{
			networkIndex++;
			nn = networks[networkIndex];
			weight = networks[networkIndex].getWeightMatrix(0).getValue(0, 0);
		}
		player.position = new Vector3(0, 0, -1);
		trialStartTime = DateTime.Now;
	}

	void EvolveNetwork()
	{
		List<NeuralNetwork> higherNetworks = new List<NeuralNetwork>();
		//Sort the scores and networks try not to missalign them...
		for (int i = 0;i < populationSize / 2;i++)
		{
			int maxScore = scores[0];
			int maxIndex = 0;
			for (int j = 1;j < scores.Count;j++)
			{
				if (scores[j] > maxScore)
				{
					
					maxScore = scores[j];
					maxIndex = j;
				}
			}

            if (i == 0)
            {
                Debug.Log("Max score for Epoch " + epoch + ": " + maxScore);
            }

            Debug.Log(maxIndex + ": " + maxScore);
			higherNetworks.Add(networks[maxIndex]);

			//Rmove the value
			scores.RemoveAt(maxIndex);
			networks.RemoveAt(maxIndex);
		}
		scores = new List<int>();
		networks = new List<NeuralNetwork>();
		//breed the other half
		for (int i = 0;i < populationSize / 2;i++)
		{
			//Figure out how to breed for now do this.
			NeuralNetwork n = new NeuralNetwork(topology, random);
			n.randomizeWeights();
			higherNetworks.Add(n);
		}

		for (int i = 0;i < populationSize;i++)
		{
			networks.Add(higherNetworks[i]);
			scores.Add(0);
		}
	}
}
