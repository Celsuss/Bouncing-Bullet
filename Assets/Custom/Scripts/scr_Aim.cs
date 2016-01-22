using UnityEngine;
using System.Collections;

public class scr_Aim : MonoBehaviour {

    public Transform gunObj;


	// Use this for initialization
	void Start () {
	
	}

    void Update()
    {
        if (Input.GetMouseButton(0)) {
            
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            
            if (Physics.Raycast(ray, out hit)) {
                Vector3 incomingVec = hit.point - gunObj.position;
                Vector3 reflectVec = Vector3.Reflect(incomingVec, hit.normal);
                
                Debug.DrawLine(gunObj.position, hit.point, Color.red);
                Debug.DrawRay(hit.point, reflectVec, Color.green);
            }
        }
    }
}
