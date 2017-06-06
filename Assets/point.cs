using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class point : MonoBehaviour {

	//const
	public int num_plane = 500;

	//public
	public int point1;
	public bool[] plane_check;
	public int point2;

	// Use this for initialization
	void Start () {
		plane_check = new bool[num_plane + 1];
		Clear ();
	}
	
	// Update is called once per frame
	void Update () {
		if (point1 != point2) {
			point2 = point1;
			GameObject.Find ("Canvas/Text").GetComponent<Text> ().text = "Score : " + point2.ToString ();
		}
	}

	void Clear() {
		point1 = 0;
		point2 = point1;
		for (int i = 1; i < num_plane; i++) plane_check[i] = false;
	}
}
