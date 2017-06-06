using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collid : MonoBehaviour {

	private point newbe;

	void OnTriggerEnter(Collider Other)
	{
		newbe = GameObject.Find("Car").GetComponent<point>();

		string s = gameObject.name.Substring(gameObject.name.IndexOf('(') + 1, gameObject.name.IndexOf(')') - gameObject.name.IndexOf('(') - 1);
		int  i = int.Parse(s);

		if(!newbe.plane_check[i]){
			newbe.plane_check [i] = true;
			(newbe.point1) = (newbe.point1)+1;
		}
	}
}
