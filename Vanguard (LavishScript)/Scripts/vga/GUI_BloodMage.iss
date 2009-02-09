function PopulateBMLists()
{
	variable int i 
	variable int rCount

	;;; Populate Combo Boxes ;;;
	for (i:Set[1] ; ${i}<=${Me.Ability} ; i:Inc)
	{
		if (${Me.Ability[${i}].Name.Find[Mental Transmutation]})
			UIElement[cmbHealthToEnergySpell@bloodmagefrm@ClassFrm@Class@ABot@vga_gui]:AddItem[${Me.Ability[${i}].Name}]
	}



	;;; Select Proper Item in Combo Boxes ;;;
	rCount:Set[0]
	while ${rCount:Inc} <= ${UIElement[cmbHealthToEnergySpell@bloodmagefrm@ClassFrm@Class@ABot@vga_gui].Items}
	{
		if ${UIElement[cmbHealthToEnergySpell@bloodmagefrm@ClassFrm@Class@ABot@vga_gui].Item[${rCount}].Text.Equal[${BMHealthToEnergySpell}]}
			UIElement[cmbHealthToEnergySpell@bloodmagefrm@ClassFrm@Class@ABot@vga_gui]:SelectItem[${rCount}]
	}
	
}