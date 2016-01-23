using UnityEngine;
using System.Collections;
using System;

public class PlayerMovement : MonoBehaviour
{

    #region Public Fields 

    // Player view
    public Transform playerView;
    public float playerViewYOffset = 0.6F;
    public float xMouseSensitivity = 30.0F;
    public float yMouseSensitivity = 30.0F;

    // Frame occuring factors
    public float gravity = 20.0F;
    public float friction = 6F;                     // Ground friction

    // Movement stuff
    public float moveSpeed = 7.0F;                  // Ground move speed
    public float runAcceleration = 14F;             // Ground accel
    public float runDeacceleration = 10F;           // Deacceleration that occurs when running on the ground
    public float airAcceleration = 2.0F;            // Air accel
    public float airDeacceleration = 2.0F;          // Deacceleration experienced when opposite strafing
    public float airControl = 0.3F;                 // How precise air control is
    public float sideStrafeAcceleration = 50F;      // How fast acceleration occurs to get up to sideStrafeSpeed when side strafing
    public float sideStrafeSpeed = 1F;              // What the max speed to generate when side strafing
    public float jumpSpeed = 8.0F;                  // The speed at which the character's up axis gains when hitting jump
    public float moveScale = 1.0F;

    // *print() styles
    public GUIStyle style;

    //Sound stuff
    public AudioClip[] jumpSounds;

    // FPS stuff
    public float fpsDisplayRate = 4.0F;

    // Prefabs
    public GameObject gibEffectPrefab;

    #endregion

    #region Private fields
    private int _frameCount = 0;
    private float _dt = 0.0F;
    private float _fps = 0.0F;

    private CharacterController _controller;

    // Camera rotationals
    private float _rotX = 0.0F;
    private float _rotY = 0.0F;

    private Vector3 _moveDirection = Vector3.zero;
    private Vector3 _moveDirectionNorm = Vector3.zero;
    private Vector3 _playerVelocity = Vector3.zero;
    private float _playerTopVelocity = 0.0F;

    // If true then player is fully on the ground
    //private bool _grounded = true;

    // Q3: players can queue the next jump just before he hits the ground
    private bool _wishJump = false;

    // USed to display real time friction values
    private float _playerFriction = 0.0F;

    private Commands _cmd; // Player commands, stores wish commands that the player skas for (Forward, back, jump, etc)

    // Player status
    private bool _isDead = false;

    private Vector3 playerSpawnPos;
    private Quaternion playerSpawnRot;

    #endregion

    // Use this for initialization
    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        playerView.position = this.transform.position;
        playerView.position = this.transform.position + new Vector3(0, playerViewYOffset, 0);

        _controller = GetComponent<CharacterController>();
        _cmd = new Commands();

        playerSpawnPos = transform.position;
        playerSpawnRot = this.playerView.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        // Do FPS calculation
        _frameCount++;
        _dt += Time.deltaTime;
        if (_dt > 1.0 / fpsDisplayRate)
        {
            _fps = Mathf.Round(_frameCount / _dt);
            _frameCount = 0;
            _dt -= 1.0F / fpsDisplayRate;
        }

        // Ensure that the cursor is locked into the screen
        if (Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        // Camera rotation stuff, mouse controls this shit
        _rotX -= Input.GetAxis("Mouse Y") * xMouseSensitivity * 0.02F;
        _rotY += Input.GetAxis("Mouse X") * xMouseSensitivity * 0.02F;

        // Clamp the X rotation
        if (_rotX < -90)
        {
            _rotX = -90;
        }
        else if (_rotX > 90)
        {
            _rotX = 90;
        }

        this.transform.rotation = Quaternion.Euler(0, _rotY, 0); // Rotates the collider
        playerView.rotation = Quaternion.Euler(_rotX, _rotY, 0); // Rotates the camera

        // Set the camera's position to the transform
        playerView.position = this.transform.position;
        playerView.position = this.transform.position + new Vector3(0, playerViewYOffset, 0);

        // Movement, the important part
        QueueJump();
        if (_controller.isGrounded)
        {
            GroundMove();
        }
        else if (!_controller.isGrounded)
        {
            AirMove();
        }

        if (_controller.isGrounded && _moveDirection.y <= 0)
        {
            _playerVelocity.y -= 0.01F;
        }

        Debug.Log(_controller.isGrounded ? "Grounded" : "Not grounded");
        //Move the controller
        _controller.Move(_playerVelocity * Time.deltaTime);

        // Calculate
        var udp = _playerVelocity;
        udp.y = 0.0F;
        if (_playerVelocity.magnitude > _playerTopVelocity)
        {
            _playerTopVelocity = _playerVelocity.magnitude;
        }

        if (Input.GetKeyUp("x"))
        {
            PlayerExplode();
        }
        if (Input.GetAxis("Fire1") != 0 && _isDead)
        {
            PlayerSpawn();
        }
    }

    #region Movement

    // Sets the movement direction based on player input
    private void SetMovementDir()
    {
        _cmd.forwardMove = Input.GetAxis("Vertical");
        _cmd.rightMove = Input.GetAxis("Horizontal");
    }

    //Queues the next jump just like in Q3
    private void QueueJump()
    {
        if(Input.GetKeyDown(KeyCode.Space) && !_wishJump)
        {
            _wishJump = true;
        }
        if(Input.GetKeyUp(KeyCode.Space))
        {
            _wishJump = false;
        }
    }

    // Runs when the player is in the air
    private void AirMove()
    {
        Vector3 wishDir;
        var wishVel = airAcceleration;
        float accel;

        var scale = CmdScale();

        SetMovementDir();

        wishDir = new Vector3(_cmd.rightMove, 0, _cmd.forwardMove);
        wishDir = transform.TransformDirection(wishDir);

        var wishSpeed = wishDir.magnitude;
        wishSpeed *= moveSpeed;

        wishDir.Normalize();
        _moveDirectionNorm = wishDir;
        wishSpeed *= scale;

        // CPM: Aircontrol
        var wishSpeed2 = wishSpeed;
        if(Vector3.Dot(_playerVelocity, wishDir) < 0)
        {
            accel = airDeacceleration;
        }
        else
        {
            accel = airAcceleration;
        }
        //If the player is ONLY strafing left or right
        if(_cmd.forwardMove == 0 && _cmd.rightMove != 0)
        {
            if(wishSpeed > sideStrafeSpeed)
            {
                wishSpeed = sideStrafeSpeed;
                accel = sideStrafeAcceleration;
            }
        }

        Accelerate(wishDir, wishSpeed, accel);

        if(airControl == 0)
        {
            AirControl(wishDir, wishSpeed2);
        }
        //! CPM: Aircontrol

        // Apply gravity
        _playerVelocity.y -= gravity * Time.deltaTime;
    }

    // Air control occurs when the player is in the air, it allows players to move side to side much faster rather than being 'sluggish' when it comes to cornering
    private void AirControl(Vector3 wishDir, float wishSpeed)
    {
        if(_cmd.forwardMove == 0 || wishSpeed == 0)
        {
            return;
        }

        var zSpeed = _playerVelocity.y;
        _playerVelocity.y = 0;
        // Equivalent to idTech's VactorNormalized()
        var speed = _playerVelocity.magnitude;
        _playerVelocity.Normalize();

        var dot = Vector3.Dot(_playerVelocity, wishDir);
        var k = 32F;
        k *= airControl * dot * dot * Time.deltaTime;

        // Change the direction while slowing down
        if(dot > 0)
        {
            _playerVelocity = new Vector3
                (
                    _playerVelocity.x * speed + wishDir.x * k, 
                    _playerVelocity.y * speed + wishDir.y * k, 
                    _playerVelocity.z * speed + wishDir.z * k
                );

            _playerVelocity.Normalize();
            _moveDirectionNorm = _playerVelocity;
        }

        _playerVelocity.x *= speed;
        _playerVelocity.y = zSpeed;
        _playerVelocity.z *= speed;
    }

    // Called every frame when the engine detects that the player is on the ground
    private void GroundMove()
    {
        Vector3 wishDir;
        Vector3 wishVel;

        // DO not apply friction if the player is queueing up the next jump
        if(!_wishJump)
        {
            ApplyFriction(1.0F);
        }
        else
        {
            ApplyFriction(0);
        }

        var scale = CmdScale();

        SetMovementDir();

        wishDir = new Vector3(_cmd.rightMove, 0, _cmd.forwardMove);
        wishDir = transform.TransformDirection(wishDir);
        wishDir.Normalize();
        _moveDirectionNorm = wishDir;

        var wishSpeed = wishDir.magnitude;
        wishSpeed *= moveSpeed;

        Accelerate(wishDir, wishSpeed, runAcceleration);

        // Reset the gravity velocity
        _playerVelocity.y = 0;

        if(_wishJump)
        {
            _playerVelocity.y = jumpSpeed;
            _wishJump = false;
            PlayJumpSound();
        }
    }

    // Applies friction to the player, called in both the air and on the ground
    private void ApplyFriction(float t)
    {
        var vec = _playerVelocity;
        float vel;
        float speed;
        float newSpeed;
        float control;
        float drop;

        vec.y = 0.0F;
        speed = vec.magnitude;
        drop = 0.0F;

        // Only apply friction if the player is on the ground
        if(_controller.isGrounded)
        {
            control = speed < runDeacceleration ? runDeacceleration : speed;
            drop = control * friction * Time.deltaTime * t;
        }

        newSpeed = speed - drop;
        _playerFriction = newSpeed;
        if (newSpeed < 0)
        {
            newSpeed = 0;
        }
        if (speed > 0)
        {
            newSpeed /= speed;
        }

        _playerVelocity.x *= newSpeed;
        _playerVelocity.z *= newSpeed;
    }

    private void Accelerate(Vector3 wishDir, float wishSpeed, float accel)
    {
        var currentSpeed = Vector3.Dot(_playerVelocity, wishDir);

        var addSpeed = wishSpeed - currentSpeed;
        if(addSpeed <= 0)
        {
            return;
        }

        var accelSpeed = accel * Time.deltaTime * wishSpeed;
        if(accelSpeed > addSpeed)
        {
            accelSpeed = addSpeed;
        }

        _playerVelocity.x += accelSpeed * wishDir.x;
        _playerVelocity.z += accelSpeed * wishDir.z;
    }

    #endregion

    void LateUpdate()
    {

    }

    void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 400, 100), "FPS: " + _fps, style);
        var ups = _controller.velocity;
        ups.y = 0;
        GUI.Label(new Rect(0, 15, 400, 100), "Speed: " + Mathf.Round(ups.magnitude * 100) / 100 + "ups", style);
        GUI.Label(new Rect(0, 30, 400, 100), "Top Speed: " + Mathf.Round(_playerTopVelocity * 100) / 100 + "ups", style);
    }

    /// <summary>
    /// 
    /// CmdScale
    /// 
    /// Returns the scalar factor to apply to cmd movements.
    /// This allows the clients to use axial -127 to 127 values for all directions,
    /// without getting a sqrt(2) distortion in speed.
    /// 
    /// </summary>
    private float CmdScale()
    {
        var max = Mathf.Abs(_cmd.forwardMove);
        if(Mathf.Abs(_cmd.rightMove) > max)
        {
            max = Mathf.Abs(_cmd.rightMove);
        }
        if(max == 0)
        {
            return max;
        }

        var total = Mathf.Sqrt(_cmd.forwardMove * _cmd.forwardMove + _cmd.rightMove * _cmd.rightMove);
        var scale = moveSpeed * max / (moveScale * total);

        return scale;
    }

    // Plays a random jump sound
    private void PlayJumpSound()
    {
        //// Don't play a new sound while the last hasn't finished
        //if(GetComponent<AudioSource>().isPlaying)
        //{
        //    return;
        //}
        //GetComponent<AudioSource>().clip = jumpSounds[UnityEngine.Random.Range(0, jumpSounds.Length)];
        //GetComponent<AudioSource>().Play();
    }

    private void PlayerExplode()
    {
        //var velocity = _controller.velocity;
        //velocity.Normalize();
        //var gibEffect = Instantiate(gibEffectPrefab, transform.position, Quaternion.identity);
        //gibEffect.GetComponent(GibFX).Explode(transform.position, velocity, _Controller.velocity.magnitude);

        _isDead = true;
    }

    private void PlayerSpawn()
    {
        this.transform.position = playerSpawnPos;
        this.playerView.rotation = playerSpawnRot;
        _rotX = 0.0F;
        _rotY = 0.0F;
        _playerVelocity = Vector3.zero;
        _isDead = false;
    }
}
