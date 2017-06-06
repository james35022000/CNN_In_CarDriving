using System.Collections;
using UnityEngine;

public class PlaneTrigger : MonoBehaviour {

	void OnTriggerEnter (Collider other) {
		getPlaneNum (other.gameObject);
	}

	void OnTriggerStay (Collider other) {

	}

	void OnTriggerExit (Collider other) {
		
	}

	int getPlaneNum(GameObject plane) {
		string[] token = plane.name.Split ("(".ToCharArray (), plane.name.Length);
		return System.Convert.ToInt32 (token [1].Split (")".ToCharArray (), token [1].Length) [0]);
	}
}
