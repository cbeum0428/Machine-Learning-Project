using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DotManager : MonoBehaviour {

	public int popultationSize = 50;
	public GameObject prefab;

	List<GameObject> food;
	// Use this for initialization
	void Start () {
		food = new List<GameObject>();
		for (int i = 0;i < popultationSize;i++)
		{
			food.Add(prefab);

			Instantiate(prefab, transform.position, Quaternion.identity);
		}
	}
	
	// Update is called once per frame
	void Update () {
		//handle timers for forcing evolution
	}
}
