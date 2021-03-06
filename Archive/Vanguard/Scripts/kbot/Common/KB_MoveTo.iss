/* credits:
This script has been written, rewritten, and contributed to by many people.
Most notably by Fippy, scubaski, and don'tdoit.  Not possible without Lax and Amadeus.  <3
Get Innerspace: www.lavishsoft.com
Get ISXVG: www.isxgames.com
Feel free to use and redistribute in a non-commercial manner as long as you keep the above (and add to them when necessary) in place.
If you remember working on this (from wowhunter days or before), let me know and i'll change my credits lines.
*/

/* Version:
v1.0 - Initial Release
v1.1 - Fixed a bug with a ' instead of a ;
*/
;
; This function moves you to within Precision yards
; of the specified X Y loc
;

function moveto(float X, float Y, float Precision, bool checkCollision)
{
	echo MOVETO called
	declare SavX float local ${Me.X}
	declare SavY float local ${Me.Y}
	declare SavZ float local ${Me.Z}
	;set BailOut timer (4 minutes)
	declare BailOut int local ${Math.Calc[${LavishScript.RunningTime}+(1000*240)]}

	;Turn to face the desired loc
	Face ${X} ${Y}

	;Check that we are not already there!
	if ${Math.Distance[${Me.X},${Me.Y},${X},${Y}]} > ${Precision}
	{
		do
		{
			if !${CurrentChunk.Equal[${Me.Chunk}]}	
			{
				return "COLLISION"
			}

			if ${checkCollision} && ${VG.CheckCollision[${X},${Y},${Me.Z}](exists)}
			{
				return "COLLISION"
			}

			;press and hold the forward button
			VG:ExecBinding[moveforward]

			;ensure we are still facing our target loc
			Face ${X} ${Y}
			;wait for half a second to give our pc a chance to move
			wait 5
			;check to make sure we have moved if not then try and avoid the
			;obstacle thats in our path
			if ${Math.Distance[${Me.X},${Me.Y},${Me.Z},${SavX},${SavY},${SavZ}]} < 1
			{
				call Obstacle2
			}
			;store our current location for future checking
			SavX:Set[${Me.X}]
			SavY:Set[${Me.Y}]
			SavZ:Set[${Me.Z}]
		}
		while (${Math.Distance[${Me.X},${Me.Y},${X},${Y}]} > ${Precision}) && !${Me.InCombat} && (${LavishScript.RunningTime} < ${BailOut})
		; need ${Me.IsDead} or something

		;Made it to our target loc
		VG:ExecBinding[moveforward,release]
		VG:ExecBinding[movebackward,release]
	}
}

;
; This function moves you to within MaxDist yards
; of the specified Object and no closer than MinDist
; if specified.
function:string movetoobject(string ObjectID, float MaxDist, float MinDist)
{
	echo ID=${ObjectID}, MaxDist=${MaxDist}, MinDist=${MinDist}

	;; we are moving so save current target ID
	lastTargetID:Set[${ObjectID}]

	;echo ${Pawn[id,${ObjectID}]} Max: ${MaxDist} Min: ${MinDist}
	declare SavX float local ${Me.X}
	declare SavY float local ${Me.Y}
	declare SavZ float local ${Me.Z}
	declare BailOut int local ${Math.Calc[${LavishScript.RunningTime}+(1000*8)]}
	declare StuckCheck bool local FALSE
	declare StuckCheckTime int local

	;Check our arguments are sensible
	if ${MinDist} > ${MaxDist}
	{
		echo Invalid arguments min distance must be less than max
		VG:ExecBinding[moveforward,release]
		VG:ExecBinding[movebackward,release]
		return
	}

	if ${MinDist} < 0 || ${MaxDist} < 0
	{
		echo Invalid value for min or max distance
		VG:ExecBinding[moveforward,release]
		VG:ExecBinding[movebackward,release]
		return
	}

	if !${Pawn[id,${ObjectID}](exists)}
	{
		echo no object specified
		VG:ExecBinding[moveforward,release]
		VG:ExecBinding[movebackward,release]
		return
	}

	do
	{
		SavX:Set[${Me.X}]
		SavY:Set[${Me.Y}]
		SavZ:Set[${Me.Z}]

		; Ensure we are still facing our target loc
		Face ${Pawn[id,${ObjectID}].X} ${Pawn[id,${ObjectID}].Y}

		;If too far away run forward
		if ${Pawn[id,${ObjectID}].Distance} > ${MaxDist}
		{
			echo Forward1 - Pawn=${Pawn[id,${ObjectID}].Distance}, MaxDist=${MaxDist}
			VG:ExecBinding[movebackward,release]
			VG:ExecBinding[moveforward]
		}

		;If too close then run backward
		if ${Pawn[id,${ObjectID}].Distance}<${MinDist}
		{
			echo Backward1
			VG:ExecBinding[moveforward,release]
			VG:ExecBinding[movebackward]
			Face ${Pawn[id,${ObjectID}].X} ${Pawn[id,${ObjectID}].Y}
		}

		;If we are close enough stop running
		;if ${Pawn[id,${ObjectID}].Distance} > ${MinDist} && ${Pawn[id,${ObjectID}].Distance} < ${MaxDist}
		if ${Pawn[id,${ObjectID}].Distance} < ${MaxDist}
		{
			echo Finished moving
			VG:ExecBinding[moveforward,release]
			VG:ExecBinding[movebackward,release]
			StuckCheck:Set[FALSE]
		}

		;wait for half a second to give our pc a chance to move
		wait 2
		
		;	echo ${Pawn[id,${ObjectID}].Name} ${Pawn[id,${ObjectID}].Distance}
		; Check to make sure we have moved if not then try and avoid the
		; obstacle thats in our path
		
		echo Distance Moved=${Math.Distance[${Me.X},${Me.Y},${Me.Z},${SavX},${SavY},${SavZ}]}
		if ${Math.Distance[${Me.X},${Me.Y},${Me.Z},${SavX},${SavY},${SavZ}]} < 10
		{
			;	echo Distdiff ${Math.Distance[${Me.X},${Me.Y},${Me.Z},${SavX},${SavY},${SavZ}]}
			; I think i might be stuck so save off the current time
			if !${StuckCheck}
			{
				;echo I might be stuck
				StuckCheck:Set[TRUE]
				StuckCheckTime:Set[${LavishScript.RunningTime}]
			}
			else
			{
				; If I am still stuck after 8 seconds then try and avoid the obstacle.
				if ${Math.Calc[${LavishScript.RunningTime} - ${StuckCheckTime}]} > 8000
				{
					;echo Yep I am stuck trying to free myself
					;call Obstacle
					StuckCheck:Set[FALSE]
					if ${VG.CheckCollision[${Pawn[id,${ObjectID}].X},${Pawn[id,${ObjectID}].Y},${Pawn[id,${ObjectID}].Z}](exists)}
					{
						call AvoidCollision ${Pawn[id,${ObjectID}].X} ${Pawn[id,${ObjectID}].Y} ${Pawn[id,${ObjectID}].Z}
						if !${Return}
						{
							VG:ExecBinding[moveforward,release]
							VG:ExecBinding[movebackward,release]
							return "COLLISION"
						}
					}
				}
			}
		}

		; If I have moved away from my saved spot reset my stuck toggle
		if ${StuckCheck} && ${Math.Distance[${Me.X},${Me.Y},${Me.Z},${SavX},${SavY},${SavZ}]} > 3
		{
			;echo I am no longer stuck
			StuckCheck:Set[FALSE]
		}

	}
	while (${Pawn[id,${ObjectID}].Distance} > ${MaxDist} || ${Pawn[id,${ObjectID}].Distance} < ${MinDist}) && ${LavishScript.RunningTime} < ${BailOut}

	VG:ExecBinding[moveforward,release]
	VG:ExecBinding[movebackward,release]

	if ${VG.CheckCollision[${Pawn[id,${ObjectID}].X},${Pawn[id,${ObjectID}].Y},${Pawn[id,${ObjectID}].Z}](exists)}
	{
		return "COLLISION"
	}
	
	if !${CurrentChunk.Equal[${Me.Chunk}]}	
	{
		return "COLLISION"
	}
	return "SUCCESS"
}

function:bool AvoidCollision(float inX, float inY, float inZ)
{
	variable int BailOut

	VG:ExecBinding[moveforward,release]
	VG:ExecBinding[movebackward,release]

	BailOut:Set[${Math.Calc[${LavishScript.RunningTime}+(2000)]}]

	; First, straf to the right
	do
	{
		VG:ExecBinding[StrafeRight]
		wait 5
	}
	while ${VG.CheckCollision[${inX},${inY},${inZ}](exists)} && ${LavishScript.RunningTime} < ${BailOut} && !${Me.InCombat}

	VG:ExecBinding[StrafeRight,release]


	BailOut:Set[${Math.Calc[${LavishScript.RunningTime}+(4000)]}]

	; Ok, then try the left
	do
	{
		VG:ExecBinding[StrafeLeft]
		wait 5
	}
	while ${VG.CheckCollision[${inX},${inY},${inZ}](exists)} && ${LavishScript.RunningTime} < ${BailOut} && !${Me.InCombat}

	VG:ExecBinding[StrafeLeft,release]

	if ${VG.CheckCollision[${inX},${inY},${inZ}](exists)}
	{
		return FALSE
	}

	return TRUE
}

;
; Use strafing to get around an obstacle
;
function Obstacle()
{
	;echo Stuck, backing up

	;backup a little
	VG:ExecBinding[moveforward,release]
	VG:ExecBinding[movebackward]
	wait 10
	VG:ExecBinding[movebackward,release]

	;randomly pick a direction
	if ${Math.Rand[10]} > 5
	{
		;echo Strafing Left
		VG:ExecBinding[StrafeLeft]
		wait 5
		VG:ExecBinding[strafeleft,release]
		wait 30
	}
	else
	{
		;echo Strafing Right
		VG:ExecBinding[straferight]
		wait 5
		VG:ExecBinding[straferight,release]
		wait 30
	}
	;	echo Advancing
	;Start moving forward again
	VG:ExecBinding[moveforward]
}

;
; Turn to get around an obstacle
;
function Obstacle2()
{
	;echo Stuck, backing up

	;backup a little
	VG:ExecBinding[moveforward,release]
	VG:ExecBinding[movebackward]
	wait 20
	VG:ExecBinding[movebackward,release]

	;randomly pick a direction
	if ${Math.Rand[10]} > 5
	{
		;echo Running Left
		;turn left a bit
		;echo ${Me.Heading} ${Math.Calc[((${Me.Heading}+-15)+360)%360]}
		face ${Math.Calc[((${Me.Heading}+-45)+360)%360]}
		VG:ExecBinding[moveforward]
		wait 30
	}
	else
	{
		;echo Running Right
		;turn right a bit
		;echo ${Me.Heading} ${Math.Calc[((${Me.Heading}+15)+360)%360]}
		face ${Math.Calc[((${Me.Heading}+45)+360)%360]}
		VG:ExecBinding[moveforward]
		wait 30
	}
	;echo Advancing
	;Start moving forward again
	VG:ExecBinding[moveforward]
}

function atexit()
{
	;Script has been ended, release the movement keys
	VG:ExecBinding[moveforward,release]
	VG:ExecBinding[movebackward,release]
}