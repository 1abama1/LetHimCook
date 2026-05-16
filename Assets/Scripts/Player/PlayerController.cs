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
    public LayerMask interactableLayer = ~0;

    private Rigidbody _rb;
    private Animator  _animator;
    private Vector3   _moveInput;
    private bool      _isSprinting;
    private bool      _isSneaking;
    private bool      _isHiding;

    private IngredientData _heldIngredient;
    public  IngredientData HeldIngredient => _heldIngredient;

    public Plate HeldPlate { get; private set; }

    [Header("Hold Settings")]
    public Transform holdAnchor;            // якорь над головой
    public GameObject ingredientVisualPrefab; // префаб визуала ингредиента (SpriteRenderer или 3D)

    private GameObject _currentIngredientVisual;

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

        _rb.constraints = RigidbodyConstraints.FreezeRotation;
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

    void Update()
    {
        Vector2 raw = Vector2.zero;
        
        // Чтение ввода: приоритет у Input System, запасной - WASD на клавиатуре
        if (_moveAction != null) raw = _moveAction.ReadValue<Vector2>();
        else if (Keyboard.current != null)
        {
            raw.x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
            raw.y = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);
        }

        _moveInput   = new Vector3(raw.x, 0f, raw.y);
        
        // Объединенная логика кнопок
        _isSprinting = _sprintAction?.IsPressed() ?? false;
        
        if (_sneakAction != null && _sneakAction.WasPressedThisFrame())
            _isSneaking = !_isSneaking;

        bool interactPressed = (_interactAction != null && _interactAction.WasPressedThisFrame()) || (Keyboard.current?.eKey.wasPressedThisFrame ?? false);
        bool hidePressed     = (_hideAction != null && _hideAction.WasPressedThisFrame())     || (Keyboard.current?.fKey.wasPressedThisFrame ?? false);

        if (interactPressed) TryInteract();
        if (hidePressed)     ToggleHide();

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

        if (_moveInput.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_moveInput.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.fixedDeltaTime);
        }
    }

    void UpdateAnimator()
    {
        if (_animator == null) return;
        _animator.SetFloat("Speed",      _moveInput.magnitude);
        _animator.SetBool("IsSneaking",  _isSneaking);
        _animator.SetBool("IsHiding",    _isHiding);
        _animator.SetBool("IsCarrying",  HeldIngredient != null || HeldPlate != null);
    }

    public void TryInteract()
    {
        int mask = interactableLayer == 0 ? ~0 : interactableLayer.value;

        Collider[]    hits    = Physics.OverlapSphere(transform.position, interactRange, mask);
        float         closest = float.MaxValue;
        IInteractable target  = null;

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            IInteractable ia = hit.GetComponent<IInteractable>();
            if (ia != null && dist < closest) { closest = dist; target = ia; }
        }

        if (target != null)
        {
            Debug.Log($"[Player] Interacted with {((MonoBehaviour)target).gameObject.name}");
            target.Interact(this);
        }
        else
        {
            Debug.Log("[Player] Tried to interact");
        }
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

    public bool IsHiding   => _isHiding;
    public bool IsSneaking  => _isSneaking;
    public bool IsSprinting => _isSprinting;
    public bool IsMoving    => _moveInput.sqrMagnitude > 0.01f;

    // ── Ингредиент ──────────────────────────────────────────────
    public void GiveIngredient(IngredientData ing)
    {
        _heldIngredient = ing;
        UpdateHoldVisual();
    }

    public IngredientData TakeIngredient()
    {
        var ing = _heldIngredient;
        _heldIngredient = null;
        UpdateHoldVisual();
        return ing;
    }

    // ── Тарелка ────────────────────────────────────────────────
    public void GivePlate(Plate plate)
    {
        HeldPlate = plate;
        if (plate != null && holdAnchor != null)
        {
            plate.transform.SetParent(holdAnchor);
            plate.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        UpdateHoldVisual();
    }

    public Plate TakePlate()
    {
        var plate = HeldPlate;
        HeldPlate = null;
        if (plate != null)
        {
            plate.transform.SetParent(null);
        }
        UpdateHoldVisual();
        return plate;
    }

    void UpdateHoldVisual()
    {
        // 1. Уничтожаем старый визуал ингредиента
        if (_currentIngredientVisual != null)
        {
            Destroy(_currentIngredientVisual);
            _currentIngredientVisual = null;
        }

        // 2. Создаём новый визуал, если игрок держит ингредиент
        if (_heldIngredient != null && holdAnchor != null)
        {
            // Если у ингредиента есть своя модель — используем её, иначе общий префаб
            GameObject prefabToSpawn = _heldIngredient.modelPrefab != null ? _heldIngredient.modelPrefab : ingredientVisualPrefab;

            if (prefabToSpawn != null)
            {
                _currentIngredientVisual = Instantiate(
                    prefabToSpawn,
                    holdAnchor.position,
                    Quaternion.identity,
                    holdAnchor);

                // Настраиваем визуал только если используем общий префаб (спрайтовый)
                if (prefabToSpawn == ingredientVisualPrefab)
                {
                    // Поддержка 2D-спрайтов
                    var sr = _currentIngredientVisual.GetComponent<SpriteRenderer>();
                    if (sr != null && _heldIngredient.sprite != null)
                        sr.sprite = _heldIngredient.sprite;

                    // Поддержка UI-Image (Canvas в world-space)
                    var img = _currentIngredientVisual.GetComponentInChildren<UnityEngine.UI.Image>();
                    if (img != null && _heldIngredient.sprite != null)
                        img.sprite = _heldIngredient.sprite;
                }
            }
        }
        // Тарелка уже привязана через SetParent в GivePlate / TakePlate — ничего дополнительного не нужно.
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
