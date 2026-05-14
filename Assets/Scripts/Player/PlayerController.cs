using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed        = 5f;
    public float sprintMultiplier = 1.6f;
    public float sneakMultiplier  = 0.5f;

    [Header("Interaction")]
    public float     interactRange     = 1.5f;
    public LayerMask interactableLayer = ~0;    // All layers by default

    private Rigidbody _rb;
    private Animator  _animator;
    private Vector3   _moveInput;
    private bool      _isSprinting;
    private bool      _isSneaking;
    private bool      _isHiding;

    private IngredientData _heldIngredient;
    public  IngredientData HeldIngredient => _heldIngredient;

    private PlayerInput  _playerInput;
    private InputAction  _moveAction;
    private InputAction  _sprintAction;
    private InputAction  _sneakAction;
    private InputAction  _interactAction;
    private InputAction  _hideAction;

    void Awake()
    {
        _rb       = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();

        // Prevent physics from tipping the character over
        _rb.constraints = RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput != null)
        {
            _moveAction     = _playerInput.actions["Move"];
            _sprintAction   = _playerInput.actions["Sprint"];
            _sneakAction    = _playerInput.actions["Sneak"];
            _interactAction = _playerInput.actions["Interact"];
            _hideAction     = _playerInput.actions["Hide"];
        }
    }

    // Store method references so OnDisable can unsubscribe the exact same delegate
    void OnEnable()
    {
        if (_interactAction != null) _interactAction.performed += OnInteract;
        if (_hideAction     != null) _hideAction.performed     += OnHide;
    }

    void OnDisable()
    {
        if (_interactAction != null) _interactAction.performed -= OnInteract;
        if (_hideAction     != null) _hideAction.performed     -= OnHide;
    }

    void OnInteract(InputAction.CallbackContext ctx) => TryInteract();
    void OnHide(InputAction.CallbackContext ctx)     => ToggleHide();

    void Update()
    {
        Vector2 raw = _moveAction != null
            ? _moveAction.ReadValue<Vector2>()
            : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        _moveInput   = new Vector3(raw.x, 0f, raw.y);
        _isSprinting = _sprintAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftShift);
        _isSneaking  = _sneakAction?.IsPressed()  ?? Input.GetKey(KeyCode.LeftControl);

        // Legacy fallback when Input System not configured
        if (_interactAction == null && Input.GetKeyDown(KeyCode.E)) TryInteract();
        if (_hideAction     == null && Input.GetKeyDown(KeyCode.F)) ToggleHide();

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (_isHiding)
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            return;
        }

        float speed = moveSpeed;
        if (_isSprinting) speed *= sprintMultiplier;
        if (_isSneaking)  speed *= sneakMultiplier;

        Vector3 velocity = _moveInput.normalized * speed;
        velocity.y = _rb.linearVelocity.y;          // Preserve gravity
        _rb.linearVelocity = velocity;

        // Rotate to face movement direction — only Y axis
        if (_moveInput.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_moveInput.normalized, Vector3.up);
            _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, 0.2f);
        }
    }

    void UpdateAnimator()
    {
        if (_animator == null) return;
        _animator.SetFloat("Speed",     _moveInput.magnitude);
        _animator.SetBool("IsSneaking", _isSneaking);
        _animator.SetBool("IsHiding",   _isHiding);
    }

    public void TryInteract()
    {
        // Use all layers if none assigned
        int mask = interactableLayer == 0 ? ~0 : interactableLayer.value;

        Collider[]    hits    = Physics.OverlapSphere(transform.position, interactRange, mask);
        float         closest = float.MaxValue;
        IInteractable target  = null;

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;   // Skip self
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            IInteractable ia = hit.GetComponent<IInteractable>();
            if (ia != null && dist < closest) { closest = dist; target = ia; }
        }

        target?.Interact(this);
    }

    public void ToggleHide()
    {
        _isHiding = !_isHiding;
        if (_isHiding)
        {
            int mask = interactableLayer == 0 ? ~0 : interactableLayer.value;
            foreach (var hit in Physics.OverlapSphere(transform.position, interactRange, mask))
                hit.GetComponent<CookingStation>()?.HideIngredient();
        }
    }

    public bool IsHiding  => _isHiding;
    public bool IsSneaking => _isSneaking;

    public void GiveIngredient(IngredientData ingredient) => _heldIngredient = ingredient;

    public IngredientData TakeIngredient()
    {
        IngredientData tmp = _heldIngredient;
        _heldIngredient    = null;
        return tmp;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}

public interface IInteractable
{
    void Interact(PlayerController player);
}
