using UnityEngine;
using System.Collections;

public class ShootBullet : MonoBehaviour {

    [SerializeField]
    private GameObject _bullet;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        Shoot();
	}

    void Shoot()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Instantiate(_bullet, transform.position + transform.forward, Quaternion.identity);
        }
    }
}
