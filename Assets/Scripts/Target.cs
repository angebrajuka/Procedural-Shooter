﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public float maxHealth = 100;
    public float health = 100;
    public bool damageable = false;
    public bool dead = false;

    // public static bool DefaultHeal(float f) { return false; }
    // public static bool DefaultDmgK(float f1, float f2) {
    //     Debug.Log("default damage");
    //     return false;
    // }

    public Func<float, float, bool> OnDamage;
    public Func<float, float, bool> OnKill;
    public Func<float, bool> OnHeal;
    
    public bool Damage(float damage, float angle=0)
    {
        if(damageable && !dead)
        {
            health -= damage;
            if(OnDamage != null) OnDamage(damage, angle);
            
            if(health <= 0)
            {
                dead = true;
                if(OnKill != null) OnKill(damage, angle);
            }
            return true;
        }
        if(OnDamage != null) OnDamage(0, angle);
        return false;
    }

    public bool Heal(float heal)
    {
        if(damageable)
        {
            health += heal;
            if(OnHeal != null) OnHeal(heal);
            if(health > maxHealth)
            {
                health = maxHealth;
                return true;
            }
        }
        return false;
    }
}
