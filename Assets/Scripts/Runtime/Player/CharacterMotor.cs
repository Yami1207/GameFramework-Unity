using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    private enum MovementTransferOnJump
    {
        None, // The jump is not affected by velocity of floor at all.
        InitTransfer, // Jump gets its initial velocity from the floor, then gradualy comes to a stop.
        PermaTransfer, // Jump gets its initial velocity from the floor, and keeps that velocity until landing.
        PermaLocked // Jump is relative to the movement of the last touched floor and will move together with that floor.
    }

    public class CharacterMotorMovement
    {
        // The maximum horizontal speed when moving
        public float maxForwardSpeed = 10.0f;
        public float maxSidewaysSpeed = 10.0f;
        public float maxBackwardsSpeed = 10.0f;

        // Curve for multiplying speed based on slope (negative = downwards)
        public AnimationCurve slopeSpeedMultiplier = new AnimationCurve(new Keyframe(-90, 1), new Keyframe(0, 1), new Keyframe(90, 0));

        // How fast does the character change speeds?  Higher is faster.
        public float maxGroundAcceleration = 30.0f;
        public float maxAirAcceleration = 20.0f;

        // The gravity for the character
        public float gravity = 10.0f;
        public float maxFallSpeed = 20.0f;

        // The last collision flags returned from controller.Move
        [NonSerialized]
        public CollisionFlags collisionFlags;

        // We will keep track of the character's current velocity,
        [NonSerialized]
        public Vector3 velocity;

        // This keeps track of our current velocity while we're not grounded
        [NonSerialized]
        public Vector3 frameVelocity = Vector3.zero;

        [NonSerialized]
        public Vector3 hitPoint = Vector3.zero;

        [NonSerialized]
        public Vector3 lastHitPoint = new Vector3(Mathf.Infinity, 0, 0);
    }

    public class CharacterMotorJumping
    {
        // Can the character jump?
        public bool enabled = true;

        // How high do we jump when pressing jump and letting go immediately
        public float baseHeight = 1.6f;

        // We add extraHeight units (meters) on top when holding the button down longer while jumping
        public float extraHeight = 1.6f;

        // How much does the character jump out perpendicular to the surface on walkable surfaces?
        // 0 means a fully vertical jump and 1 means fully perpendicular.
        public float perpAmount = 2.0f;

        // How much does the character jump out perpendicular to the surface on too steep surfaces?
        // 0 means a fully vertical jump and 1 means fully perpendicular.
        public float steepPerpAmount = 1.5f;

        // For the next variables, @System.NonSerialized tells Unity to not serialize the variable or show it in the inspector view.
        // Very handy for organization!

        // Are we jumping? (Initiated with jump button and not grounded yet)
        // To see if we are just in the air (initiated by jumping OR falling) see the grounded variable.
        public bool jumping = false;

        public bool holdingJumpButton = false;

        // the time we jumped at (Used to determine for how long to apply extra jump power after jumping.)
        public float lastStartTime = 0.0f;

        public float lastButtonDownTime = -100f;

        public Vector3 jumpDir = Vector3.up;
    }

    private class CharacterMotorMovingPlatform
    {
        public bool enabled = true;

        public MovementTransferOnJump movementTransfer = MovementTransferOnJump.PermaTransfer;

        public Transform hitPlatform;

        public Transform activePlatform;

        public Vector3 activeLocalPoint;

        public Vector3 activeGlobalPoint;

        public Quaternion activeLocalRotation;

        public Quaternion activeGlobalRotation;

        public Matrix4x4 lastMatrix;

        public Vector3 platformVelocity;

        public bool newPlatform;
    }

    private class CharacterMotorSliding
    {
        // Does the character slide on too steep surfaces?
        public bool enabled = true;

        // How fast does the character slide on steep surfaces?
        public float slidingSpeed = 15f;

        // How much can the player control the sliding direction?
        // If the value is 0.5 the player can slide sideways with half the speed of the downwards sliding speed.
        public float sidewaysControl = 1.0f;

        // How much can the player influence the sliding speed?
        // If the value is 0.5 the player can speed the sliding up to 150% or slow it down to 50%.
        public float speedControl = 0.4f;
    }

    /// <summary>
    /// 移动方向
    /// </summary>
    private Vector3 m_MoveDirection = Vector3.zero;
    public Vector3 moveDirection { set { m_MoveDirection = value; } get { return m_MoveDirection; } }

    private readonly CharacterMotorMovement m_Movement = new CharacterMotorMovement();
    public CharacterMotorMovement movement { get { return m_Movement; } }

    /// <summary>
    /// 跳跃
    /// </summary>
    private readonly CharacterMotorJumping m_Jumping = new CharacterMotorJumping();

    private readonly CharacterMotorMovingPlatform m_MovingPlatform = new CharacterMotorMovingPlatform();

    private readonly CharacterMotorSliding m_Sliding = new CharacterMotorSliding();

    private bool m_CanControl = true;
    private bool m_IsGrounded = true;

    private bool m_InputJump = false;

    private Vector3 m_GroundNormal = Vector3.zero;
    private Vector3 m_LastGroundNormal = Vector3.zero;

    private CharacterController m_Controller;

    public Action onFall;
    public Action onLand;
    public Action onJump;

    private void Awake()
    {
        m_Controller = gameObject.GetComponent<CharacterController>();
    }

    private void Update()
    {
        DoUpdate();
    }

    private void DoUpdate()
    {
        // We copy the actual velocity into a temporary variable that we can manipulate.
        var velocity = movement.velocity;

        // Update velocity based on input
        velocity = ApplyInputVelocityChange(velocity);

        // Apply gravity and jumping force
        velocity = ApplyGravityAndJumping(velocity);

        // Moving platform support
        var moveDistance = Vector3.zero;
        if (MoveWithPlatform())
        {
            var newGlobalPoint = m_MovingPlatform.activePlatform.TransformPoint(m_MovingPlatform.activeLocalPoint);
            moveDistance = (newGlobalPoint - m_MovingPlatform.activeGlobalPoint);
            if (moveDistance != Vector3.zero)
                m_Controller.Move(moveDistance);

            // Support moving platform rotation as well:
            var newGlobalRotation = m_MovingPlatform.activePlatform.rotation * m_MovingPlatform.activeLocalRotation;
            var rotationDiff = newGlobalRotation * Quaternion.Inverse(m_MovingPlatform.activeGlobalRotation);

            var yRotation = rotationDiff.eulerAngles.y;
            if (yRotation != 0)
            {
                // Prevent rotation of the local up vector
                transform.Rotate(0, yRotation, 0);
            }
        }

        // Save lastPosition for velocity calculation.
        var lastPosition = transform.position;

        // We always want the movement to be framerate independent.  Multiplying by Time.deltaTime does this.
        var currentMovementOffset = velocity * Time.deltaTime;

        // Find out how much we need to push towards the ground to avoid loosing grouning
        // when walking down a step or over a sharp change in slope.
        var pushDownOffset = Mathf.Max(m_Controller.stepOffset, new Vector3(currentMovementOffset.x, 0, currentMovementOffset.z).magnitude);
        if (m_IsGrounded)
            currentMovementOffset -= pushDownOffset * Vector3.up;

        // Reset variables that will be set by collision function
        m_MovingPlatform.hitPlatform = null;
        m_GroundNormal = Vector3.zero;

        // Move our character!
        movement.collisionFlags = m_Controller.Move(currentMovementOffset);

        movement.lastHitPoint = movement.hitPoint;
        m_LastGroundNormal = m_GroundNormal;

        if (m_MovingPlatform.enabled && m_MovingPlatform.activePlatform != m_MovingPlatform.hitPlatform)
        {
            if (m_MovingPlatform.hitPlatform != null)
            {
                m_MovingPlatform.activePlatform = m_MovingPlatform.hitPlatform;
                m_MovingPlatform.lastMatrix = m_MovingPlatform.hitPlatform.localToWorldMatrix;
                m_MovingPlatform.newPlatform = true;
            }
        }

        // Calculate the velocity based on the current and previous position.  
        // This means our velocity will only be the amount the character actually moved as a result of collisions.
        var oldHVelocity = new Vector3(velocity.x, 0, velocity.z);
        movement.velocity = (transform.position - lastPosition) / Time.deltaTime;
        var newHVelocity = new Vector3(movement.velocity.x, 0, movement.velocity.z);

        // The CharacterController can be moved in unwanted directions when colliding with things.
        // We want to prevent this from influencing the recorded velocity.
        if (oldHVelocity == Vector3.zero)
        {
            movement.velocity = new Vector3(0, movement.velocity.y, 0);
        }
        else
        {
            var projectedNewVelocity = Vector3.Dot(newHVelocity, oldHVelocity) / oldHVelocity.sqrMagnitude;
            movement.velocity = oldHVelocity * Mathf.Clamp01(projectedNewVelocity) + movement.velocity.y * Vector3.up;
        }

        if (movement.velocity.y < velocity.y - 0.001)
        {
            if (movement.velocity.y < 0)
            {
                // Something is forcing the CharacterController down faster than it should.
                // Ignore this
                movement.velocity.y = velocity.y;
            }
            else
            {
                // The upwards movement of the CharacterController has been blocked.
                // This is treated like a ceiling collision - stop further jumping here.
                m_Jumping.holdingJumpButton = false;
            }
        }

        if (movement.velocity.y > velocity.y + 0.001)
        {
            if (movement.velocity.y > 0)
            {
                movement.velocity.y = velocity.y;
            }
        }

        // We were grounded but just loosed grounding
        if (m_IsGrounded && !IsGroundedTest())
        {
            m_IsGrounded = false;

            // Apply inertia from platform
            if (m_MovingPlatform.enabled &&
                (m_MovingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer ||
                m_MovingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)
            )
            {
                movement.frameVelocity = m_MovingPlatform.platformVelocity;
                movement.velocity += m_MovingPlatform.platformVelocity;
            }

            //SendMessage("OnFall", SendMessageOptions.DontRequireReceiver);
            if (onFall != null) onFall();
            // We pushed the character down to ensure it would stay on the ground if there was any.
            // But there wasn't so now we cancel the downwards offset to make the fall smoother.
            transform.position += pushDownOffset * Vector3.up;
        }
        // We were not grounded but just landed on something
        else if (!m_IsGrounded && IsGroundedTest())
        {
            m_IsGrounded = true;
            m_Jumping.jumping = false;
            SubtractNewPlatformVelocity();

            //SendMessage("OnLand", SendMessageOptions.DontRequireReceiver);
            if (onLand != null)
                onLand();
        }

        // Moving platforms support
        if (MoveWithPlatform())
        {
            // Use the center of the lower half sphere of the capsule as reference point.
            // This works best when the character is standing on moving tilting platforms. 
            m_MovingPlatform.activeGlobalPoint = transform.position + Vector3.up * (m_Controller.center.y - (m_Controller.height * 0.5f) + m_Controller.radius);
            m_MovingPlatform.activeLocalPoint = m_MovingPlatform.activePlatform.InverseTransformPoint(m_MovingPlatform.activeGlobalPoint);

            // Support moving platform rotation as well:
            m_MovingPlatform.activeGlobalRotation = transform.rotation;
            m_MovingPlatform.activeLocalRotation = Quaternion.Inverse(m_MovingPlatform.activePlatform.rotation) * m_MovingPlatform.activeGlobalRotation;
        }
    }

    private Vector3 ApplyInputVelocityChange(Vector3 velocity)
    {
        if (!m_CanControl)
            m_MoveDirection = Vector3.zero;

        // Find desired velocity
        Vector3 desiredVelocity;
        if (m_IsGrounded && TooSteep())
        {
            // The direction we're sliding in
            desiredVelocity = new Vector3(m_GroundNormal.x, 0, m_GroundNormal.z).normalized;
            // Find the input movement direction projected onto the sliding direction
            var projectedMoveDir = Vector3.Project(m_MoveDirection, desiredVelocity);
            // Add the sliding direction, the spped control, and the sideways control vectors
            desiredVelocity = desiredVelocity + projectedMoveDir * m_Sliding.speedControl + (m_MoveDirection - projectedMoveDir) * m_Sliding.sidewaysControl;
            // Multiply with the sliding speed
            desiredVelocity *= m_Sliding.slidingSpeed;
        }
        else
            desiredVelocity = GetDesiredHorizontalVelocity();

        if (m_MovingPlatform.enabled && m_MovingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)
        {
            desiredVelocity += movement.frameVelocity;
            desiredVelocity.y = 0;
        }

        if (m_IsGrounded)
            desiredVelocity = AdjustGroundVelocityToNormal(desiredVelocity, m_GroundNormal);
        else
            velocity.y = 0;

        // Enforce max velocity change
        var maxVelocityChange = GetMaxAcceleration(m_IsGrounded) * Time.deltaTime;
        var velocityChangeVector = (desiredVelocity - velocity);
        if (velocityChangeVector.sqrMagnitude > maxVelocityChange * maxVelocityChange)
        {
            velocityChangeVector = velocityChangeVector.normalized * maxVelocityChange;
        }
        // If we're in the air and don't have control, don't apply any velocity change at all.
        // If we're on the ground and don't have control we do apply it - it will correspond to friction.
        if (m_IsGrounded || m_CanControl)
            velocity += velocityChangeVector;

        if (m_IsGrounded)
        {
            // When going uphill, the CharacterController will automatically move up by the needed amount.
            // Not moving it upwards manually prevent risk of lifting off from the ground.
            // When going downhill, DO move down manually, as gravity is not enough on steep hills.
            velocity.y = Mathf.Min(velocity.y, 0);
        }

        return velocity;
    }

    /// <summary>
    /// 处理重力与跳跃
    /// </summary>
    /// <param name="velocity"></param>
    /// <returns></returns>
    private Vector3 ApplyGravityAndJumping(Vector3 velocity)
    {
        if (!m_InputJump || !m_CanControl)
        {
            m_Jumping.holdingJumpButton = false;
            m_Jumping.lastButtonDownTime = -100;
        }

        if (m_InputJump && m_Jumping.lastButtonDownTime < 0 && m_CanControl)
            m_Jumping.lastButtonDownTime = Time.time;

        if (m_IsGrounded)
        {
            velocity.y = Mathf.Min(0, velocity.y) - movement.gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = movement.velocity.y - movement.gravity * Time.deltaTime * 2;

            // When jumping up we don't apply gravity for some time when the user is holding the jump button.
            // This gives more control over jump height by pressing the button longer.
            if (m_Jumping.jumping && m_Jumping.holdingJumpButton)
            {
                // Calculate the duration that the extra jump force should have effect.
                // If we're still less than that duration after the jumping time, apply the force.
                if (Time.time < m_Jumping.lastStartTime + m_Jumping.extraHeight / CalculateJumpVerticalSpeed(m_Jumping.baseHeight))
                {
                    // Negate the gravity we just applied, except we push in jumpDir rather than jump upwards.
                    velocity += m_Jumping.jumpDir * movement.gravity * Time.deltaTime;
                }
            }

            // Make sure we don't fall any faster than maxFallSpeed. This gives our character a terminal velocity.
            velocity.y = Mathf.Max(velocity.y, -movement.maxFallSpeed);
        }

        if (m_IsGrounded)
        {
            // Jump only if the jump button was pressed down in the last 0.2 seconds.
            // We use this check instead of checking if it's pressed down right now
            // because players will often try to jump in the exact moment when hitting the ground after a jump
            // and if they hit the button a fraction of a second too soon and no new jump happens as a consequence,
            // it's confusing and it feels like the game is buggy.
            if (m_Jumping.enabled && m_CanControl && (Time.time - m_Jumping.lastButtonDownTime < 0.2))
            {
                m_IsGrounded = false;
                m_Jumping.jumping = true;
                m_Jumping.lastStartTime = Time.time;
                m_Jumping.lastButtonDownTime = -100;
                m_Jumping.holdingJumpButton = true;

                // Calculate the jumping direction
                if (TooSteep())
                    m_Jumping.jumpDir = Vector3.Slerp(Vector3.up, m_GroundNormal, m_Jumping.steepPerpAmount);
                else
                    m_Jumping.jumpDir = Vector3.Slerp(Vector3.up, m_GroundNormal, m_Jumping.perpAmount);

                // Apply the jumping force to the velocity. Cancel any vertical velocity first.
                velocity.y = 0;
                velocity += m_Jumping.jumpDir * CalculateJumpVerticalSpeed(m_Jumping.baseHeight);

                // Apply inertia from platform
                if (m_MovingPlatform.enabled &&
                    (m_MovingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer ||
                    m_MovingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)
                )
                {
                    movement.frameVelocity = m_MovingPlatform.platformVelocity;
                    velocity += m_MovingPlatform.platformVelocity;
                }

                //SendMessage("OnJump", SendMessageOptions.DontRequireReceiver);
                if (onJump != null)
                    onJump();
            }
            else
            {
                m_Jumping.holdingJumpButton = false;
            }
        }

        return velocity;
    }

    private IEnumerable SubtractNewPlatformVelocity()
    {
        // When landing, subtract the velocity of the new ground from the character's velocity
        // since movement in ground is relative to the movement of the ground.
        if (m_MovingPlatform.enabled &&
            (m_MovingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer ||
            m_MovingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)
        )
        {
            // If we landed on a new platform, we have to wait for two FixedUpdates
            // before we know the velocity of the platform under the character
            if (m_MovingPlatform.newPlatform)
            {
                var platform = m_MovingPlatform.activePlatform;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                if (m_IsGrounded && platform == m_MovingPlatform.activePlatform)
                    yield return 1;
            }
            movement.velocity -= m_MovingPlatform.platformVelocity;
        }
    }

    private bool TooSteep()
    {
        return (m_GroundNormal.y <= Mathf.Cos(m_Controller.slopeLimit * Mathf.Deg2Rad));
    }

    private bool IsGroundedTest()
    {
        return m_GroundNormal.y > 0.01;
    }

    private bool MoveWithPlatform()
    {
        return m_MovingPlatform.enabled
            && (m_IsGrounded || m_MovingPlatform.movementTransfer == MovementTransferOnJump.PermaLocked)
            && m_MovingPlatform.activePlatform != null;
    }

    private Vector3 GetDesiredHorizontalVelocity()
    {
        // Find desired velocity
        var desiredLocalDirection = transform.InverseTransformDirection(m_MoveDirection);
        var maxSpeed = MaxSpeedInDirection(desiredLocalDirection);
        if (m_IsGrounded)
        {
            // Modify max speed on slopes based on slope speed multiplier curve
            var movementSlopeAngle = Mathf.Asin(movement.velocity.normalized.y) * Mathf.Rad2Deg;
            maxSpeed *= movement.slopeSpeedMultiplier.Evaluate(movementSlopeAngle);
        }
        return transform.TransformDirection(desiredLocalDirection * maxSpeed);
    }

    /// <summary>
    /// 最大速率
    /// </summary>
    /// <param name="desiredMovementDirection"></param>
    /// <returns></returns>
    private float MaxSpeedInDirection(Vector3 desiredMovementDirection)
    {
        if (desiredMovementDirection == Vector3.zero)
            return 0;

        var zAxisEllipseMultiplier = (desiredMovementDirection.z > 0 ? movement.maxForwardSpeed : movement.maxBackwardsSpeed) / movement.maxSidewaysSpeed;
        var temp = new Vector3(desiredMovementDirection.x, 0, desiredMovementDirection.z / zAxisEllipseMultiplier).normalized;
        var length = new Vector3(temp.x, 0, temp.z * zAxisEllipseMultiplier).magnitude * movement.maxSidewaysSpeed;
        return length;
    }

    /// <summary>
    /// 最大加速度
    /// </summary>
    /// <param name="grounded"></param>
    /// <returns></returns>
    private float GetMaxAcceleration(bool grounded)
    {
        return grounded ? movement.maxGroundAcceleration : movement.maxAirAcceleration;
    }

    private float CalculateJumpVerticalSpeed(float targetJumpHeight)
    {
        // From the jump height and gravity we deduce the upwards speed 
        // for the character to reach at the apex.
        return Mathf.Sqrt(2 * targetJumpHeight * movement.gravity);
    }

    private Vector3 AdjustGroundVelocityToNormal(Vector3 hVelocity, Vector3 groundNormal)
    {
        var sideways = Vector3.Cross(Vector3.up, hVelocity);
        return Vector3.Cross(sideways, groundNormal).normalized * hVelocity.magnitude;
    }
}
