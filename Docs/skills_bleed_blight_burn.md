# DoT Skills Reference (Bleed, Blight, Burn)

This document provides a complete reference of all Marionette and Enemy skills that apply Damage over Time (DoT) status effects (**Bleed**, **Blight**, and **Burn**), including their amplitude, duration, application chance, and target parameters.

---

## 1. Marionette Skills

> [!NOTE]
> Following the DoT Attack Scaling rework, the `Amplitude` field is evaluated as a percentage of the applier's effective Attack stat (`damagePerTick = max(1, round(effAttack * amplitude / 100))`). Authored asset values below remain pending game designer re-tuning.

| Skill | Character | Status | Amplitude (% of Attack, pending designer re-tune) | Duration | Base Chance | Target Scope | Target Ranks | Asset File |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **Poison Blast** | Alchemist | **Blight** | **5** | **3** turns | 100% | Enemies (1) | 1, 2, 3, 4 | [alchemist_SD_poison_blast.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Marionettes/Alchemist/alchemist_SD_poison_blast.asset) |
| **Toxic Grenade** | Alchemist | **Blight** | **4** | **3** turns | 100% | Enemies (2) | 3, 4 | [alchemist_SD_toxic_grenade.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Marionettes/Alchemist/alchemist_SD_toxic_grenade.asset) |
| **Dagger Throw** | Assassin | **Bleed** | **3** | **3** turns | 100% | Enemies (1) | 2, 3, 4 | [assassin_SD_dagger_throw.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Marionettes/Assassin/assassin_SD_dagger_throw.asset) |
| **Deep Cut** | Assassin | **Bleed** | **3** | **3** turns | 100% | Enemies (1) | 1, 2 | [assassin_SD_deep_cut.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Marionettes/Assassin/assassin_SD_deep_cut.asset) |
| **Lashing Strings** | Cecilia | **Bleed** | **3** | **3** turns | 100% | Enemies (1) | 1, 2, 3 | [Cecilia_SD_lashing_strings.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Marionettes/Cecilia/Cecilia_SD_lashing_strings.asset) |
| **Rusted Tip** | Cecilia | **Blight** | **4** | **3** turns | 100% | Enemies (1) | 1, 2, 3, 4 | [Cecilia_SD_rusted_tip.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Marionettes/Cecilia/Cecilia_SD_rusted_tip.asset) |

---

## 2. Enemy Skills

| Skill | Enemy | Status | Amplitude (% of Attack, pending designer re-tune) | Duration | Base Chance | Target Scope | Target Ranks | Asset File |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **Rip and Tear** | Guardian Hounds | **Bleed** | **5** | **3** turns | 130% | Enemies (All) | 1, 2, 3, 4 | [guardian_hound_blue_SD_rip_and_tear.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Guardian%20Hounds/guardian_hound_blue_SD_rip_and_tear.asset) |
| **Explosion** | Overheating Golem | **Bleed** | **3** | **3** turns | 100% | Enemies (All) | 1, 2, 3, 4 | [overheating_golem_SBD_explosion.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Overheating%20Golem/overheating_golem_SBD_explosion.asset) |
| **Hunt Down** | Corgian Hunter | **Bleed** | **2** | **3** turns | 110% | Enemies (1) | 1, 2, 3 | [corgian_hunter_SD_hunt_down.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Corgian%20Hunter/corgian_hunter_SD_hunt_down.asset) |
| **Shred Armor** | Shredder Bot | **Bleed** | **2** | **3** turns | 100% | Enemies (1) | 1, 2 | [shredder_bot_SBD_shred_armor.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Shredder%20Bot/shredder_bot_SBD_shred_armor.asset) |
| **Shred Weapon** | Shredder Bot | **Bleed** | **2** | **3** turns | 100% | Enemies (1) | 1, 2 | [shredder_bot_SBD_shred_weapon.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Shredder%20Bot/shredder_bot_SBD_shred_weapon.asset) |
| **Feral Ravage** | Scratcher | **Bleed** | **1** | **3** turns | 90% | Enemies (2) | 1, 2 | [scratcher_SD_feral_ravage.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Scratcher/scratcher_SD_feral_ravage.asset) |
| **Slashing Claw** | Scratcher | **Bleed** | **1** | **3** turns | 100% | Enemies (1) | 1, 2, 3 | [scratcher_SD_slashing_claw.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Scratcher/scratcher_SD_slashing_claw.asset) |
| **Sharpen Claws** *(On Attack)* | Apex Scratcher | **Bleed** (Buff) | **2** | **3** turns | 100% | Self | Self | [apex_scratcher_SBD_sharpen_claws.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Apex%20Scratcher/apex_scratcher_SBD_sharpen_claws.asset) |
| **Coffee Catapult** | Catapult Gremlin | **Blight** | **2** | **3** turns | 110% | Enemies (2) | 1, 2, 3, 4 | [catapult_gremlin_SD_coffee_catapult.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Catapult%20Gremlin/catapult_gremlin_SD_coffee_catapult.asset) |
| **Venom Bite** | Infester | **Blight** | **2** | **3** turns | 100% | Enemies (1) | 1, 2, 3 | [infester_SD_venom_bite.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Infester/infester_SD_venom_bite.asset) |
| **Pointblank Puke** | Bile Puker | **Blight** | **2** | **3** turns | 120% | Enemies (1) | 1, 2 | [bile_puker_SBD_pointblank_puke.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Pile%20Buker/bile_puker_SBD_pointblank_puke.asset) |
| **Puke** | Bile Puker | **Blight** | **2** | **3** turns | 120% | Enemies (2) | 3, 4 | [bile_puker_SBD_puke.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Pile%20Buker/bile_puker_SBD_puke.asset) |
| **Blazing Advance** | Rose Knight | **Burn** | **3** | **3** turns | 100% | Enemies (All) | 1, 2, 3, 4 | [rose_knight_SD_blazing_advance.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Rose%20Knight/rose_knight_SD_blazing_advance.asset) |
| **Ignition Great Slash** | Rose Knight | **Burn** | **3** | **3** turns | 100% | Enemies (All) | 1, 2, 3, 4 | [rose_knight_SD_erupt.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Rose%20Knight/rose_knight_SD_erupt.asset) |
| **Spread The Flame** | Rose Knight Bodyguard | **Burn** | **2** | **3** turns | 100% | Enemies (1) | 1, 2, 3 | [corgian_bodyguard_SD_spread_the_flame.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Rose%20Knight/corgian_bodyguard_SD_spread_the_flame.asset) |
| **Eternal Blaze** | Rose Knight | **Burn** (Allies) | **1** | **3** turns | 300% | Allies (All) | 1, 2, 3, 4 | [rose_knight_SD_burning_heart.asset](file:///d:/Nevergreen/Project_Nevergreen/Assets/Data/Skills/Enemies/Rose%20Knight/rose_knight_SD_burning_heart.asset) |

---

## 3. Breakdown by Status Effect Type

### Bleed (`StatusType: 0`)
* **Marionette Skills:**
  * **Dagger Throw** (Assassin) - Amplitude: **3**, Duration: **3**
  * **Deep Cut** (Assassin) - Amplitude: **3**, Duration: **3**
  * **Lashing Strings** (Cecilia) - Amplitude: **3**, Duration: **3**
* **Enemy Skills:**
  * **Rip and Tear** (Guardian Hounds) - Amplitude: **5**, Duration: **3**
  * **Explosion** (Overheating Golem) - Amplitude: **3**, Duration: **3**
  * **Hunt Down** (Corgian Hunter) - Amplitude: **2**, Duration: **3**
  * **Shred Armor** (Shredder Bot) - Amplitude: **2**, Duration: **3**
  * **Shred Weapon** (Shredder Bot) - Amplitude: **2**, Duration: **3**
  * **Feral Ravage** (Scratcher) - Amplitude: **1**, Duration: **3**
  * **Slashing Claw** (Scratcher) - Amplitude: **1**, Duration: **3**
  * **Sharpen Claws** (Apex Scratcher) - Grants self a 3-turn buff ([BleedOnAttackStatusEffect](file:///d:/Nevergreen/Project_Nevergreen/Assets/Scripts/Combat/Effects/BleedOnAttackStatusEffect.cs)) giving all attacks a 100% chance to inflict Bleed with Amplitude: **2**, Duration: **3**.

### Blight (`StatusType: 1`)
* **Marionette Skills:**
  * **Poison Blast** (Alchemist) - Amplitude: **5**, Duration: **3**
  * **Toxic Grenade** (Alchemist) - Amplitude: **4**, Duration: **3**
  * **Rusted Tip** (Cecilia) - Amplitude: **4**, Duration: **3**
* **Enemy Skills:**
  * **Coffee Catapult** (Catapult Gremlin) - Amplitude: **2**, Duration: **3**
  * **Venom Bite** (Infester) - Amplitude: **2**, Duration: **3**
  * **Pointblank Puke** (Bile Puker) - Amplitude: **2**, Duration: **3**
  * **Puke** (Bile Puker) - Amplitude: **2**, Duration: **3**

### Burn (`StatusType: 14`)
* **Marionette Skills:**
  * *None currently configured.*
* **Enemy Skills:**
  * **Blazing Advance** (Rose Knight) - Amplitude: **3**, Duration: **3**
  * **Ignition Great Slash** (Rose Knight) - Amplitude: **3**, Duration: **3**
  * **Spread The Flame** (Rose Knight Bodyguard) - Amplitude: **2**, Duration: **3**
  * **Eternal Blaze** (Rose Knight) - Amplitude: **1**, Duration: **3** *(targets all allies except self alongside attack buff)*

---

## 4. Mechanics Reference

* **Mechanic Spec:** [MECHANIC_DAMAGE_OVER_TIME.md](file:///d:/Nevergreen/Project_Nevergreen/Docs/specs/mechanics/MECHANIC_DAMAGE_OVER_TIME.md)
* **Skill Data Definition:** [SkillData.cs](file:///d:/Nevergreen/Project_Nevergreen/Assets/Scripts/Data/SkillData.cs)
* **Status Effect Logic:** [StatusEffect.cs](file:///d:/Nevergreen/Project_Nevergreen/Assets/Scripts/Combat/Effects/StatusEffect.cs)
* **Status Enum:** [StatusType](file:///d:/Nevergreen/Project_Nevergreen/Assets/Scripts/Data/SkillData.cs#L105-L126)
