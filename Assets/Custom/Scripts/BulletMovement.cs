using UnityEngine;
using System.Collections;

public class BulletMovement : MonoBehaviour {

    [SerializeField]
    private float _movementSpeed;
    private Vector3 _direction;

	// Use this for initialization
	void Start () {
        _direction = Camera.main.transform.forward;
	}
	
	// Update is called once per frame
	void Update () {
        transform.Translate(_direction * _movementSpeed * Time.deltaTime, transform);
	}

    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 reflectVec = Vector3.Reflect(_direction, contact.normal);
            _direction = reflectVec;
        }
    }
}