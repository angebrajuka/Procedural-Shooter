using UnityEngine;
using static PlayerInput;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement instance;

    // hierarchy
    public Transform t_camera;
    public float walkAccel;
    public float airWalkAccel;
    public float walkMaxSpeed;
    public float friction_normal;
    public float friction_slide;
    public float groundNormal;
    public float jumpForce;
    public float slideForce;
    public float slideStartThreshhold;
    public float slideMaxSpeed;
    public float slideHeightAdjust;
    public float cameraHeightAdjustSpeed;

    // components
    public static Rigidbody m_rigidbody;
    public static CapsuleCollider m_collider;
    public static SphereCollider m_uncrouchCollider;

    Vector3 normal;
    Vector3 cameraTargetPos;
    Vector3 cameraDefaultPos;
    bool grounded;
    bool p_crouching;
    Vector3 input_move;
    Vector2 input_look;
    bool input_crouch;

    public void Init()
    {
        instance = this;

        m_rigidbody = GetComponent<Rigidbody>();
        m_collider = GetComponent<CapsuleCollider>();
        m_uncrouchCollider = GetComponent<SphereCollider>();

        cameraTargetPos = Vector3.up*slideHeightAdjust/2;
        cameraDefaultPos = t_camera.localPosition-cameraTargetPos;

        normal = new Vector3(0, 0, 0);
        grounded = false;
        crouching = false;
        input_move = new Vector3(0, 0);
        input_look = new Vector2(0, 0);
        input_crouch = false;
    }

    bool crouching
    {
        get { return p_crouching; }
        set
        {
            bool wasCrouching = p_crouching;
            p_crouching = value;
            if(crouching == wasCrouching) return;

            int mult = crouching ? -1 : 1;
            m_collider.height += slideHeightAdjust * mult;
            cameraTargetPos *= -1;
            m_collider.center += cameraTargetPos;
            var zvel = m_rigidbody.RelativeVelocity().z;
            if(grounded && crouching && zvel > walkMaxSpeed*slideStartThreshhold && zvel < slideMaxSpeed)
                m_rigidbody.AddRelativeForce(0, 0, slideForce);
        }
    }

    void OnCollisionExit(Collision collision) {
        normal.Set(0, 0, 0);
        grounded = false;
    }

    void OnCollisionStay(Collision collision) {
        for(int i=0; i < collision.contactCount; i++) {
            Vector3 cnormal = collision.contacts[i].normal;
            if(cnormal.y > normal.y) {
                normal = cnormal;
            }
        }

        grounded = normal.y > groundNormal && Mathf.Abs(m_rigidbody.velocity.y) <= 1f;
    }

    void Update()
    {
        input_move.Set(0, 0, 0);
        input_look.Set(0, 0);

        if(GetKey("walk_front"))    input_move.z ++;
        if(GetKey("walk_back"))     input_move.z --;
        if(GetKey("walk_left"))     input_move.x --;
        if(GetKey("walk_right"))    input_move.x ++;
        input_move.Normalize();

        if(GetKeyDown("jump"))
        {
            input_move.y ++;
        }

        input_crouch = GetKey("slide");

        input_look.x = Input.GetAxis("Mouse X") * speed_look.x;
        input_look.y = Input.GetAxis("Mouse Y") * speed_look.y;
    }

    void FixedUpdate()
    {
        // accelerate
        m_rigidbody.AddRelativeForce(
            Mathf.Abs(Vector3.Dot(m_rigidbody.velocity, m_rigidbody.transform.right)) < walkMaxSpeed ? (input_move.x*(grounded && !crouching ? walkAccel : airWalkAccel))*Time.fixedDeltaTime : 0,
            0,
            Mathf.Abs(Vector3.Dot(m_rigidbody.velocity, m_rigidbody.transform.forward)) < walkMaxSpeed ? (input_move.z*(grounded && !crouching ? walkAccel : airWalkAccel))*Time.fixedDeltaTime : 0
        );

        if(grounded)
        {
            // friction
            Vector3 vel = m_rigidbody.velocity;
            vel *= crouching ? friction_slide : friction_normal;
            vel.y = m_rigidbody.velocity.y; // dont affect y for friction
            m_rigidbody.velocity = vel;

            // jump
            m_rigidbody.AddForce(0, input_move.y*jumpForce, 0);

            // crouch & slide
            crouching = input_crouch || crouching;
        }
        if((!input_crouch || !grounded) && Physics.OverlapSphere(m_uncrouchCollider.center+m_rigidbody.position, m_uncrouchCollider.radius).Length <= 1) // 1 because it collides with m_uncrouchTrigger
        {
            crouching = false;
        }
    }

    void LateUpdate()
    {
        t_camera.localPosition = Vector3.Lerp(t_camera.localPosition, cameraDefaultPos+cameraTargetPos, cameraHeightAdjustSpeed*Time.deltaTime);

        Vector3 rotation;
        if(input_look.x != 0)
        {
            rotation = m_rigidbody.rotation.eulerAngles;
            rotation.y += input_look.x;
            m_rigidbody.rotation = Quaternion.Euler(rotation);
        }
        if(input_look.y != 0)
        {
            rotation = t_camera.localEulerAngles;
            rotation.x -= input_look.y;
            if(rotation.x > 90 && rotation.x <= 180)        rotation.x = 90;
            else if(rotation.x < 270 && rotation.x >= 180)  rotation.x = 270;
            t_camera.localEulerAngles = rotation;
        }
    }
}