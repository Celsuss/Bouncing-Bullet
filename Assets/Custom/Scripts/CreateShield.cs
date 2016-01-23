using UnityEngine;
using System.Collections;

public class CreateShield : MonoBehaviour {

    [SerializeField] private float _duration;
    [SerializeField] private float _tickTime;
    [SerializeField] private GameObject _shield;
    private float _tickTimer;

	// Use this for initialization
	void Start () {
        _tickTimer = _tickTime;
        Vector3 pos = transform.position + transform.forward + -transform.right;
        _shield = (GameObject)Instantiate(_shield, pos, Quaternion.identity);
        _shield.transform.parent = transform;
        _shield.SetActive(false);
	}
	
	// Update is called once per frame
	void Update () {
        Tick();
        ActivateShield();
	}

    void ActivateShield()
    {
        if (Input.GetMouseButton(1) && _duration > 0)
        {
            _shield.SetActive(true);
            _tickTimer -= Time.deltaTime;
        }
        else
        {
            _shield.SetActive(false);
        }
    }

    void Tick()
    {
        if (_tickTimer <= 0)
        {
            _tickTimer = _tickTime;
            _duration -= 1;
        }
    }
}
