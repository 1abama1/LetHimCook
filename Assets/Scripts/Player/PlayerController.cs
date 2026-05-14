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
    public float      interactRange    = 1.5f;
    public LayerMask  interactableLayer;
    public Transform  holdPoint;

    private Rigidbody  _rb;
    private Animator   _animator;
    private Vector3    _moveInput;
    private bool       _isSprinting;
    private bool       _isSneaking;
    private bool       _isHiding;

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
        _rb          = GetComponent<Rigidbody>();
        _animator    = GetComponent<Animator>();
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

    void OnEnable()
    {
        _interactAction?.Enable();
        _hideAction?.Enable();
        if (_interactAction != null) _interactAction.performed += _ => TryInteract();
        if (_hideAction     != null) _hideAction.performed     += _ => ToggleHide();
    }

    void OnDisable()
    {
        if (_interactAction != null) _interactAction.performed -= _ => TryInteract();
        if (_hideAction     != null) _hideAction.performed     -= _ => ToggleHide();
    }

    void Update()
    {
        Vector2 raw = _moveAction != null
            ? _moveAction.ReadValue<Vector2>()
            : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        _moveInput   = new Vector3(raw.x, 0f, raw.y);
        _isSprinting = _sprintAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftShift);
        _isSneaking  = _sneakAction?.IsPressed()  ?? Input.GetKey(KeyCode.LeftControl);

        if (_interactAction == null && Input.GetKeyDown(KeyCode.E)) TryInteract();
        if (_hideAction     == null && Input.GetKeyDown(KeyCode.F)) ToggleHide();

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (_isHiding) { _rb.linearVelocity = Vector3.zero; return; }

        float speed = moveSpeed;
        if (_isSprinting) speed *= sprintMultiplier;
        if (_isSneaking)  speed *= sneakMultiplier;

        Vector3 velocity = _moveInput.normalized * speed;
        velocity.y       = _rb.linearVelocity.y;
        _rb.linearVelocity = velocity;

        if (_moveInput.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(transform.forward, _moveInput.normalized, 0.2f);
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
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactableLayer);
        float       closest = float.MaxValue;
        IInteractable target  = null;

        foreach (var hit in hits)
        {
            float dist         = Vector3.Distance(transform.position, hit.transform.position);
            IInteractable ia   = hit.GetComponent<IInteractable>();
            if (ia != null && dist < closest) { closest = dist; target = ia; }
        }

        target?.Interact(this);
    }

    public void ToggleHide()
    {
        _isHiding = !_isHiding;

        // Hide spoiled ingredient on cooking station when inspector turns
        if (_isHiding)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactableLayer);
            foreach (var hit in hits)
                hit.GetComponent<CookingStation>()?.HideIngredient();
        }
    }

    public bool IsHiding => _isHiding;
    public bool IsSneaking => _isSneaking;

    public void GiveIngredient(IngredientData ingredient)
    {
        _heldIngredient = ingredient;
    }

    public IngredientData TakeIngredient()
    {
        var tmp         = _heldIngredient;
        _heldIngredient = null;
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
