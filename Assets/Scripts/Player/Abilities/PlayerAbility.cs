using UnityEngine;

public abstract class PlayerAbility : ScriptableObject {
    [Header("Ability Info")]
    public string abilityName;
    public Sprite icon;
    public float cooldown = 1f;
    public bool requiresGrounded = false;
    
    [Header("Input Binding")]
    public KeyCode defaultKey;
    
    protected EnhancedPlayerController Player;
    protected float LastUsedTime = -999f;
    
    public bool IsReady => Time.time >= LastUsedTime + cooldown;
    public float CooldownRemaining => Mathf.Max(0, (LastUsedTime + cooldown) - Time.time);
    public float CooldownPercent => IsReady ? 1f : 1f - (CooldownRemaining / cooldown);
    
    public virtual void Initialize(EnhancedPlayerController playerController)
    {
        Player = playerController;
    }
    
    public virtual bool CanActivate()
    {
        return IsReady;
    }
    
    public void TryActivate() {
        if (!CanActivate()) return;
        
        LastUsedTime = Time.time;
        OnActivate();
    }
    
    protected abstract void OnActivate();
}

public class AbilityBinding {
    public AbilitySlot Slot;
    public PlayerAbility Ability;
    public KeyCode CustomKey;
    
    public AbilityBinding(AbilitySlot slot)
    {
        this.Slot = slot;
    }
}