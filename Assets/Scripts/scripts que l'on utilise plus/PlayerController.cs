using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float movingSpeed = 5f;

    private Rigidbody playerRb;
    public InputActionReference move;
    public InputActionReference slash;


    // doit match l'animation, car on ne veut pas qu'il recommence l'action avant de finir l'animation
    private float slashCoolDown = 2.75f;
    private float slashTimeCounter = 0f;

    public Vector2 moveDirection {  get; private set; }
    public bool SlashPressedThisFrame { get; private set; }
    // valeur de 0-1 pour l'animation
    public float Speed { get; private set; }
    public bool IsSlashing => slashTimeCounter > 0f;


    private void Awake()
    {
        playerRb = GetComponent<Rigidbody>();
        Debug.Assert(movingSpeed >  0f);
    }
    
    //activer 
    private void OnEnable()
    {
        // Ensure actions are enabled
        move?.action?.Enable();
        slash?.action?.Enable();
    }

    // désactiver si on a pas besoin
    private void OnDisable()
    {
        move?.action?.Disable();
        slash?.action?.Disable();
    }
    private void Update()
    {
        // le Cooldown
       if ( slashTimeCounter > 0f ) slashTimeCounter -= Time.deltaTime;

        moveDirection = move != null ? move.action.ReadValue<Vector2>() : Vector2.zero;

        // pour le garder entre 0 et 1 pour l'animation
        Speed = Mathf.Clamp01(moveDirection.magnitude);

        // ca évite des lags de le set a false par défaut
        SlashPressedThisFrame = false;

        bool slashIsPressed = slash != null && slash.action.triggered;

        if ( slashIsPressed )
        {
            // le lockout du slash est démarré
            SlashPressedThisFrame = true;
            slashTimeCounter = slashCoolDown;
        }

    }
    private void FixedUpdate()
    {
        // Optionnel pour ne pas qu'il bouge pendant qu'il slash ( a voir si on doit implementer
        // Vector2 dir = IsSlashing ? Vector2.zero : moveDirection.normalized;

        //                                                             le y axe dépend du terrain
        playerRb.linearVelocity = new Vector3(moveDirection.normalized.x * movingSpeed, playerRb.linearVelocity.y, moveDirection.normalized.y * movingSpeed);
        
    }

    
}
