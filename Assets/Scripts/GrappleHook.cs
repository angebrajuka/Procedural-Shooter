using UnityEngine;
using static GrappleHook.State;

public class GrappleHook : MonoBehaviour
{
    // hierarchy
    public AudioClip clip_reload;
    public ConfigurableJoint configJoint;
    public Rigidbody m_rigidbody;
    public LineRenderer m_lineRenderer;
    public SphereCollider m_sphereCollider;

    public FixedJoint fixedJoint;
    bool addJoint=false;
    Rigidbody other;
    public ThreeDM threeDM;
    float maxDist;

    void Start()
    {
        maxDist = configJoint.linearLimit.limit-2;
    }

    public enum State
    {
        SHOOTING,
        SWINGING,
        RETRACTING
    }
    public State state = SHOOTING;

    public void LockMotion(Rigidbody other)
    {
        this.other = other;
        addJoint = true;
        if(other == null)
        {
            m_rigidbody.velocity *= 0;
            configJoint.massScale = 0;
        }
    }

    public void Retract()
    {
        if(fixedJoint == null)
        {
            GetComponent<Collider>().enabled = false;
            configJoint.connectedMassScale = 0;
        }

        if(state == RETRACTING) return;

        state = RETRACTING;
        configJoint.SetDistance();
        configJoint.massScale = 1;
        threeDM.source_cableSpinning.Play();
    }

    void FixedUpdate()
    {
        if(state == RETRACTING)
        {
            if(configJoint.linearLimit.limit > threeDM.minDistance || fixedJoint == null)
            {
                var ll = configJoint.linearLimit;
                ll.limit -= (fixedJoint == null ? threeDM.autoRetractSpeedFast : threeDM.autoRetractSpeedSlow)*Time.fixedDeltaTime;
                configJoint.linearLimit = ll;
            }
            else
            {
                threeDM.source_cableSpinning.Stop();
            }
        }

        const int layermask = (Layers.PLAYER | Layers.PLAYER_ARMS);
        RaycastHit hit;
        if(state == SHOOTING && Physics.SphereCast(m_rigidbody.position+m_sphereCollider.center, m_sphereCollider.radius, m_rigidbody.velocity, out hit, m_rigidbody.velocity.magnitude*Time.fixedDeltaTime, layermask))
        {
            m_rigidbody.position += m_rigidbody.velocity.normalized*hit.distance;
            LockMotion(hit.rigidbody);
            configJoint.SetDistance();
            state = SWINGING;
        }
    }

    void Update()
    {
        if(fixedJoint == null && Vector3.Distance(m_rigidbody.position+configJoint.anchor, PlayerMovement.m_rigidbody.position+configJoint.connectedAnchor) > maxDist)
        {
            Retract();
        }

        if(state == RETRACTING && fixedJoint == null && Vector3.Distance(configJoint.GetStart(), configJoint.GetEnd()) < threeDM.destroyDistance)
        {
            AudioManager.PlayClip(clip_reload);
            threeDM.returnTime = Time.time;
            threeDM.source_cableSpinning.Stop();
            Destroy(gameObject);
        }
    }

    void LateUpdate()
    {
        if(addJoint)
        {
            fixedJoint = gameObject.AddComponent<FixedJoint>();
            fixedJoint.connectedBody = other;
            fixedJoint.enableCollision = false;
            fixedJoint.breakForce = threeDM.fixedJointBreakForce;
            fixedJoint.breakTorque = threeDM.fixedJointBreakForce;
            addJoint = false;
        }

        m_lineRenderer.SetPosition(0, m_rigidbody.position+configJoint.anchor);
        m_lineRenderer.SetPosition(m_lineRenderer.positionCount-1, threeDM.transform.position);
    }
}