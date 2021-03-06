/*
Check4Immunites v1.0
by:  Zandros, 22 APR 2010

Description:
Return TRUE or FALSE if target is immune to a type of ability you are about to use.
Also, scan events if you healed a mob and stop attacking with that ability

parameters:
ABILITY = Any ability (spell) you are about to use

Example Code:
call MobImmune "Union of Blood I"
if ${Return}
"mob is immune"

External Routines that must be in your program:  None
*/

/* Toggle this on or off in your scripts to overide */
variable bool doArcane = TRUE
variable bool doPhysical = TRUE
variable bool doMental = TRUE

variable collection:string Immune2Arcane
variable collection:string Immune2Physical
variable collection:string Immune2Mental

variable string TargetImmunity
variable bool doSetupImmunities = TRUE

;===================================================
;===      Known Mobs with Immunities            ====
;===================================================
function:bool Check4Immunites(string ABILITY="SKIP")
{
	variable string temp = "None"

	if ${doSetupImmunities}
	{
		;; We only want to setup once
		doSetupImmunities:Set[FALSE]
	
		Immune2Arcane:Clear
		Immune2Arcane:Set["Omac","Omac"]
		Immune2Arcane:Set["Salrin","Salrin"]
		Immune2Arcane:Set["Bandori","Bandori"]
		Immune2Arcane:Set["Guardian B27","Guardian B27"]
		Immune2Arcane:Set["Energized Marauder","Energized Marauder"]
		Immune2Arcane:Set["Energized Resonator","Energized Resonator"]
		Immune2Arcane:Set["Energized Elemental","Energized Elemental"]
		Immune2Arcane:Set["Construct of Lightning","Construct of Lightning"]
		Immune2Arcane:Set["Cartheon Wingwraith","Cartheon Wingwraith"]
		Immune2Arcane:Set["Source of Arcane Energy","Source of Arcane Energy"]
		Immune2Arcane:Set["Ancient Infector","Ancient Infector"]
		Immune2Arcane:Set["Cartheon Archivist","Cartheon Archivist"]
		Immune2Arcane:Set["Cartheon Arcanist","Cartheon Arcanist"]
		Immune2Arcane:Set["Cartheon Scholar","Cartheon Scholar"]
		Immune2Arcane:Set["Eyelord Seeker","Eyelord Seeker"]
		Immune2Arcane:Set["Stormsuit","Stormsuit"]
		Immune2Arcane:Set["Electric Elemental","Electric Elemental"]
	
		Immune2Physical:Clear
		Immune2Physical:Set["Enraged Death Hound","Enraged Death Hound"]
		Immune2Physical:Set["Lesser Flarehound","Lesser Flarehound"]
		Immune2Physical:Set["Lixirikin","Lixirikin"]
		Immune2Physical:Set["Nathrix","Nathrix"]
		Immune2Physical:Set["Shonaka","Shonaka"]
		Immune2Physical:Set["Wisil","Wisil"]
		Immune2Physical:Set["Filtha","Filtha"]
		Immune2Physical:Set["SILIUSAURUS","SILIUSAURUS"]
		Immune2Physical:Set["SUMMONER RINIPIN","SUMMONER RINIPIN"]
		Immune2Physical:Set["ARCHON TRAVIX","ARCHON TRAVIX"]
		Immune2Physical:Set["Earthen Marauder","Earthen Marauder"]
		Immune2Physical:Set["Earthen Resonator","Earthen Resonator"]
		Immune2Physical:Set["Cartheon Devourer","Cartheon Devourer"]
		Immune2Physical:Set["Earth Elemental","Earth Elemental"]
		Immune2Physical:Set["Rock Elemental","Rock Elemental"]
		Immune2Physical:Set["Cartheon Soulslasher","Cartheon Soulslasher"]
		Immune2Physical:Set["Cartheon Abomination","Cartheon Abomination"]
		Immune2Physical:Set["Glowing Infineum","Glowing Infineum"]
		Immune2Physical:Set["Living Infineum","Living Infineum"]
		Immune2Physical:Set["Spawn of Infineum","Spawn of Infineum"]
		Immune2Physical:Set["Myconid Fungal Ravager","Myconid Fungal Ravager"]
		Immune2Physical:Set["Xakrin Sage","Xakrin Sage"]
		Immune2Physical:Set["Hound of Rahz","Hound of Rahz"]
		Immune2Physical:Set["Ancient Juggernaut","Ancient Juggernaut"]
		Immune2Physical:Set["Mechanized Pyromaniac","Mechanized Pyromaniac"]
		Immune2Physical:Set["Xakrin Razarclaw","Xakrin Razarclaw"]
		Immune2Physical:Set["Assaulting Death Hound","Assaulting Death Hound"]
		Immune2Physical:Set["Blood-crazed Ettercap","Blood-crazed Ettercap"]
		Immune2Physical:Set["Earth Elemental","Earth Elemental"]

		Immune2Mental:Clear
		Immune2Mental:Set["VAHSREN THE LIBRARIAN","VAHSREN THE LIBRARIAN"]
	}

	;; Return FALSE if no target
	if !${Me.Target(exists)}
	{
		TargetImmunity:Set[No Target]
		return FALSE
	}

	if ${Immune2Arcane.Element["${Me.Target.Name}"](exists)} || ${Me.TargetBuff[Electric Form](exists)}
	{
		temp:Set[ARCANE]
	}
	
	if ${Immune2Physical.Element["${Me.Target.Name}"](exists)} || ${Me.TargetBuff[Earth Form](exists)}
	{
		if ${temp.Equal[None]}
		{
			temp:Set[PHYSICAL]
		}
		else
		{
			temp:Set[${temp}, PHYSICAL]
		}
	}

	if ${Immune2Mental.Element["${Me.Target.Name}"](exists)}
	{
		if ${temp.Equal[None]}
		{
			temp:Set[MENTAL]
		}
		else
		{
			temp:Set[${temp}, MENTAL]
		}
	}

	;; Update out display
	TargetImmunity:Set[${temp}]

	;; Check our passed ability
	if !${ABILITY.Equal[SKIP]}
	{
		if ${Me.Ability[${ABILITY}].School.Find[Arcane]} || ${Me.Ability[${ABILITY}].Description.Find[Arcane]}
		{	
			if ${TargetImmunity.Find[ARCANE]} || !${doArcane} || ${Me.TargetBuff[Electric Form](exists)}
			{
				return TRUE
			}
		}
		if ${Me.Ability[${ABILITY}].School.Find[Physical]} || ${Me.Ability[${ABILITY}].Description.Find[Physical]}
		{
			if ${TargetImmunity.Find[PHYSICAL]} || !${doPhysical} || ${Me.TargetBuff[Earth Form](exists)}
			{
				return TRUE
			}
		}
		if ${Me.Ability[${ABILITY}].School.Find[Mental]} || ${Me.Ability[${ABILITY}].Description.Find[Mental]}
		{
			if !${doMental}
			{
				return TRUE
			}
		}
	}
	return FALSE
}
	

;===================================================
;===    Automatically learn a mob resistance    ====
;===================================================
atom VG_OnIncomingCombatText(string aText, int aType)
{

	if ${Me.Target.Name.Find[Unstable]}
		return


	if ${aText.Find[immune]}
		echo (${aType}) -- ${aText}


	if ${aType} == 28 && ${aText.Find[${Me.Target.Name}]} && (${aText.Find[heals]} || ${aText.Find[immune]})
	{
	
		;; We want to update our immunity display
		doCheckImmunities:Set[TRUE]

		;; Add target and type to Learned Immunities list
		if ${Me.Ability[${aText.Token[2,">"].Token[1,"<"]}].School.Find[Arcane]}
		{
			LearnedImmunitiesList:Set["${Me.Target.Name}", "Arcane"]
			call PlaySound ALARM
			echo ${Me.Target.Name} is immune/healed to ${aText.Token[2,">"].Token[1,"<"]} (Arcane)
			vgecho ${aText.Token[2,">"].Token[1,"<"]} (Arcane)
		}
		if ${Me.Ability[${aText.Token[2,">"].Token[1,"<"]}].School.Find[Physical]}
		{
			LearnedImmunitiesList:Set["${Me.Target.Name}", "Physical"]
			call PlaySound ALARM
			echo ${Me.Target.Name} is immune/healed to ${aText.Token[2,">"].Token[1,"<"]} (Physical)
			vgecho ${aText.Token[2,">"].Token[1,"<"]} (Physical)
		}
		if ${Me.Ability[${aText.Token[2,">"].Token[1,"<"]}].School.Find[Spiritual]}
		{
			LearnedImmunitiesList:Set["${Me.Target.Name}", "Spiritual"]
			call PlaySound ALARM
			echo ${Me.Target.Name} is immune/healed to ${aText.Token[2,">"].Token[1,"<"]} (Spiritual)
			vgecho ${aText.Token[2,">"].Token[1,"<"]} (Spiritual)
		}
		if ${Me.Ability[${aText.Token[2,">"].Token[1,"<"]}].School.Find[Fire]}
		{
			LearnedImmunitiesList:Set["${Me.Target.Name}", "Fire"]
			call PlaySound ALARM
			echo ${Me.Target.Name} is immune/healed to ${aText.Token[2,">"].Token[1,"<"]} (Fire)
			vgecho ${aText.Token[2,">"].Token[1,"<"]} (Fire)
		}
		if ${Me.Ability[${aText.Token[2,">"].Token[1,"<"]}].School.Find[Ice]}
		{
			LearnedImmunitiesList:Set["${Me.Target.Name}", "Ice"]
			call PlaySound ALARM
			echo ${Me.Target.Name} is immune/healed to ${aText.Token[2,">"].Token[1,"<"]} (Ice)
			vgecho ${aText.Token[2,">"].Token[1,"<"]} (Ice)
		}
		if ${Me.Ability[${aText.Token[2,">"].Token[1,"<"]}].School.Find[Cold]}
		{
			LearnedImmunitiesList:Set["${Me.Target.Name}", "Cold"]
			call PlaySound ALARM
			echo ${Me.Target.Name} is immune/healed to ${aText.Token[2,">"].Token[1,"<"]} (Cold)
			vgecho ${aText.Token[2,">"].Token[1,"<"]} (Cold)
		}
	}
}

