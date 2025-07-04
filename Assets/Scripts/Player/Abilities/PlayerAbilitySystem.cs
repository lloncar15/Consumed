using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerAbilitySystem : MonoBehaviour {
    [Header("Ability Bindings")]
    [SerializeField] private readonly List<AbilityBinding> _abilityBindings = new List<AbilityBinding>()
    {
        new AbilityBinding(AbilitySlot.Ability1),
        new AbilityBinding(AbilitySlot.Ability2),
        new AbilityBinding(AbilitySlot.Ability3),
        new AbilityBinding(AbilitySlot.Ability4)
    };
    
    private EnhancedPlayerController _playerController;
    private readonly Dictionary<AbilitySlot, PlayerAbility> _abilities = new Dictionary<AbilitySlot, PlayerAbility>();
    
    // Events
    public event Action<AbilitySlot, PlayerAbility> OnAbilityBound;
    public event Action<AbilitySlot, PlayerAbility> OnAbilityActivated;
    
    private void Awake() {
        _playerController = GetComponent<EnhancedPlayerController>();
        
        // Initialize ability dictionary
        foreach (AbilityBinding binding in _abilityBindings.Where(binding => binding.Ability != null)) {
            BindAbility(binding.Slot, binding.Ability);
        }
    }
    
    private void OnEnable()
    {
        _playerController.OnAbilityUsed += HandleAbilityInput;
    }
    
    private void OnDisable()
    {
        _playerController.OnAbilityUsed -= HandleAbilityInput;
    }
    
    private void HandleAbilityInput(AbilitySlot slot)
    {
        if (_abilities.TryGetValue(slot, out PlayerAbility ability))
        {
            ability.TryActivate();
            OnAbilityActivated?.Invoke(slot, ability);
        }
    }
    
    public void BindAbility(AbilitySlot slot, PlayerAbility ability)
    {
        if (ability == null) return;
        
        // Create instance to avoid shared state
        PlayerAbility abilityInstance = Instantiate(ability);
        abilityInstance.Initialize(_playerController);
        
        _abilities[slot] = abilityInstance;
        
        // Update binding list
        AbilityBinding binding = _abilityBindings.Find(b => b.Slot == slot);
        if (binding != null)
        {
            binding.Ability = abilityInstance;
        }
        
        OnAbilityBound?.Invoke(slot, abilityInstance);
    }
    
    public void UnbindAbility(AbilitySlot slot) {
        if (!_abilities.Remove(slot)) return;

        AbilityBinding binding = _abilityBindings.Find(b => b.Slot == slot);
        if (binding != null)
        {
            binding.Ability = null;
        }
    }
    
    public PlayerAbility GetAbility(AbilitySlot slot)
    {
        _abilities.TryGetValue(slot, out PlayerAbility ability);
        return ability;
    }
    
    public float GetCooldownPercent(AbilitySlot slot) {
        return _abilities.TryGetValue(slot, out PlayerAbility ability) ? ability.CooldownPercent : 1f;
    }
}