using UnityEngine;
using System.Collections;

public class CreateShield : MonoBehaviour {

    [SerializeField] private float _duration;
    [SerializeField] private float _tickTime;

    private float _tickTimer;
    private GameObject _shield;

	// Use this for initialization
	void Start () {
        _tickTimer = _tickTime;
        _shield = transform.FindChild("MainCamera").FindChild("Shield").gameObject;
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
