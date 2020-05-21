using UnityEngine;
using System.Collections;

public class Manager : MonoBehaviour {

	public GameObject model;

	void Start () {
		ObjectLoader loader = model.AddComponent <ObjectLoader> ();
		loader.Load (@"/model/path/", "model.obj");
	}
}
