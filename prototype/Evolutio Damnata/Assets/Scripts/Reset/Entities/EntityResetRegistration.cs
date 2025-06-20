using UnityEngine;

/// <summary>
/// Component that automatically registers/unregisters its EntityManager with the EntityCombatResetHandler
/// based on the entity's placement and death status
/// </summary>
[RequireComponent(typeof(EntityManager))]
public class EntityResetRegistration : MonoBehaviour
{
    private EntityManager _entityManager;
    private EntityCombatResetHandler _resetHandler;
    private bool _isRegistered = false;
    
    private void Awake()
    {
        _entityManager = GetComponent<EntityManager>();
        
        // Find the reset handler
        _resetHandler = FindObjectOfType<EntityCombatResetHandler>();
        
        if (_resetHandler == null)
        {
            Debug.LogWarning($"[EntityResetRegistration] No EntityCombatResetHandler found in scene for entity: {name}");
        }
    }
    
    private void Start()
    {
        // Initial check - if entity is already placed at start, register it
        CheckAndUpdateRegistration();
    }
    
    private void Update()
    {
        CheckAndUpdateRegistration();
    }
    
    private void CheckAndUpdateRegistration()
    {
        if (_entityManager == null || _resetHandler == null)
            return;
        
        // Register when entity is placed
        if (_entityManager.placed && !_isRegistered && !_entityManager.dead)
        {
            RegisterEntity();
        }
        
        // Unregister when entity dies
        if (_entityManager.dead && _isRegistered)
        {
            UnregisterEntity();
        }
    }
    
    private void RegisterEntity()
    {
        if (_resetHandler != null && _entityManager != null && !_isRegistered)
        {
            _resetHandler.RegisterEntity(_entityManager);
            _isRegistered = true;
            Debug.Log($"[EntityResetRegistration] Entity '{_entityManager.name}' registered");
        }
    }
    
    private void UnregisterEntity()
    {
        if (_resetHandler != null && _entityManager != null && _isRegistered)
        {
            _resetHandler.UnregisterEntity(_entityManager);
            _isRegistered = false;
            Debug.Log($"[EntityResetRegistration] Entity '{_entityManager.name}' unregistered");
        }
    }
    
    // As a safety measure, still unregister on destroy
    private void OnDestroy()
    {
        if (_isRegistered)
        {
            UnregisterEntity();
        }
    }
}