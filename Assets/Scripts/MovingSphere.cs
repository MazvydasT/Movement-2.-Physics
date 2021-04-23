using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;

    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;

    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;

    Rigidbody rigidBody;

    Vector3 velocity, desiredVelocity, contactNormal;
    
    bool desiredJump;

    int groundContactCount;

    int jumpPhase = 0;

    float minGroundDotProduct;

    bool OnGround => groundContactCount > 0;

    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        
        OnValidate();
    }

    void Update()
    {
        var playerInput = Vector2.ClampMagnitude(
            new Vector2(
                Input.GetAxis("Horizontal"),
                Input.GetAxis("Vertical")
            ),
            1f
        );

        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        desiredJump |= Input.GetButtonDown("Jump");

        GetComponent<Renderer>().material.SetColor("_Color", Color.white * (groundContactCount * 0.25f));
    }

    void FixedUpdate()
    {
        UpdateState();
        AdjustVelocity();

        if(desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        rigidBody.velocity = velocity;

        ClearState();
    }

    void ClearState()
    {
        groundContactCount = 0;
        contactNormal = Vector3.zero;
    }

    void Jump()
    {
        if (OnGround || jumpPhase < maxAirJumps)
        {
            ++jumpPhase;

            var jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            var alignedSpeed = Vector3.Dot(velocity, contactNormal);

            if(alignedSpeed > 0)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }

            velocity += contactNormal * jumpSpeed;
        }
    }

    void UpdateState()
    {
        velocity = rigidBody.velocity;

        if(OnGround)
        {
            jumpPhase = 0;

            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }

        else
        {
            contactNormal = Vector3.up;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        EvaluateColision(collision);
    }

    void OnCollisionEnter(Collision collision)
    {
        EvaluateColision(collision);
    }

    void EvaluateColision(Collision collision)
    {
        for(var i = 0; i < collision.contactCount; ++i)
        {
            var normal = collision.GetContact(i).normal;

            if(normal.y >= minGroundDotProduct)
            {
                ++groundContactCount;

                contactNormal += normal;
            }
        }
    }

    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    void AdjustVelocity()
    {
        var xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        var zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        var currentX = Vector3.Dot(velocity, xAxis);
        var currentZ = Vector3.Dot(velocity, zAxis);

        var acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        var maxSpeedChange = acceleration * Time.deltaTime;

        var newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        var newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }
}
