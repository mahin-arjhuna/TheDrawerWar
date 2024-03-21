using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DAIMBEnemy : MonoBehaviour
{
    // Property Variables (all enemies have these)
    public ulong health;
    public ulong damage;
    public ulong moneyDrop;
    public float maxSpeed;
    public float acceleration;
    public float attackRadius;
    public float attackCooldown;
    // Internal Variables (for calculations and logic)
    private Vector2 attackDirection;
    private States currentState;
    private States nextState;
    private Vector2 velocity;
    private Vector2 currAcceleration;
    private float timer;
    // Enemy specific variables
    public float attackingSpeed;
    public float preAttackDelay;
    public float postAttackDeceleration;

    private int turnMultiplier;

    private Vector2 angleVelocity;

    // Start is called before the first frame update
    void Start()
    {
        attackDirection = Vector2.zero;
        currentState = States.Idle;
        nextState = States.Idle;
        velocity = Vector2.zero;
        currAcceleration = Vector2.zero;
        timer = 0.0f;

        turnMultiplier = 3;

        angleVelocity = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        // If new state detected, change states
        if (currentState != nextState)
        {
            currentState = nextState;
        }

        // Update current state
        switch (currentState)
        {
            case States.Idle:
                nextState = States.Chase;
                break;
            case States.Chase:
                Chase();
                break;
            case States.Attack:
                ShortRangeAttack();
                break;
            case States.Dead:
                break;
        }
    }

    private void Chase()
    {
        // Follow mouse position
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 position = gameObject.transform.position;
        attackDirection = mousePos - position;

        // Update acceleration (Seek behavior) [more acceleration if target is behind]
        currAcceleration = acceleration * (attackDirection - velocity).normalized;
        if (Vector2.Dot(currAcceleration, velocity) < 0)
            currAcceleration *= turnMultiplier;
        // Update velocity
        velocity += Time.deltaTime * currAcceleration;
        if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
            velocity = velocity.normalized * maxSpeed;
        // Update Position
        position += Time.deltaTime * velocity;
        gameObject.transform.position = position;
        // Update Rotation
        float angle = HelperFunctions.GetAngleTowardsVector(velocity);
        gameObject.transform.rotation = Quaternion.Euler(new(0, 0, angle));

        // Check if player is within attacking distance
        if (attackDirection.sqrMagnitude <= attackRadius * attackRadius)
        {
            timer = 0.0f;
            nextState = States.Attack;
            velocity = Vector2.zero;
        }
    }

    private void ShortRangeAttack()
    {
        // Get position
        Vector2 position = gameObject.transform.position;

        // Delay by attackDelay amount of time before attacking
        if (timer < preAttackDelay)
        {
            timer += Time.deltaTime;

            Vector2 playerPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 attackDirection = playerPosition - position;

            // Update acceleration (for speed of rotation)
            currAcceleration = acceleration * (attackDirection - angleVelocity).normalized;
            if (Vector2.Dot(currAcceleration, angleVelocity) < 0)
                currAcceleration *= turnMultiplier;
            // Update velocity
            angleVelocity += Time.deltaTime * currAcceleration;
            // Update rotation
            float angle = HelperFunctions.GetAngleTowardsVector(angleVelocity);
            gameObject.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            // Once timer is up, set attacking speed
            if (timer >= preAttackDelay)
            {
                // Set attacking speed
                velocity = attackingSpeed * HelperFunctions.GetVectorFromAngle(angle);
            }
        }
        else
        {
            timer += Time.deltaTime;

            // Pos
            position += Time.deltaTime * velocity;
            gameObject.transform.position = position;

            // Decelerate
            Vector2 decelerate = postAttackDeceleration * Time.deltaTime * velocity;
            velocity -= decelerate;

            // Once speed is approximately 0, reset and return to chasing state
            if (velocity.magnitude <= 0.5f)
            {
                velocity = Vector2.zero;
                if (timer >= attackCooldown)
                {
                    // Set velocity to scaled up normalized attackDirection
                    velocity = maxSpeed * angleVelocity.normalized;
                    // Reset timer
                    timer = 0.0f;
                    nextState = States.Chase;
                }
            }
        }
    }
}
