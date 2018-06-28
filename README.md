# MercDeployments
BattleTech mod (using ModTek), that changes Travel contracts to Deplyoments.

## Requirements
* install [BattleTechModLoader](https://github.com/Mpstark/BattleTechModLoader/releases) using the [instructions here](https://github.com/Mpstark/BattleTechModLoader)
* install [ModTek](https://github.com/Mpstark/ModTek/releases) using the [instructions here](https://github.com/Mpstark/ModTek)

## Not Compatible
* Do not use together with Multi Missions

## Features
- Changes Travel contracts to deployments.
- Deplyoments start with the arrival at the Contract System.
- Deployments go on for multiple months.
- While on a Deployment you will get payed monthly.
- Each day on Deplyoment has a change to spawn a new mission.
- While a mission is available time is frozen.
- Missions wont pay you, but still reward you salvage as usual.
- After completeing the mission time is unfrozen again.
- If you leave the planet before the Deplyoment is over you will lose a huge ammount of Rep with the Faction and the MRB.
- When the Deplyoment time is over you get notified and are free again to take a new Deployment or normal Mission.

## Download

Downloads can be found on [github](https://github.com/Morphyum/MercDeployments/releases).

## Settings
Setting | Type | Default | Description
--- | --- | --- | ---
MissionChancePerDay | float | default 0.1 | Chance 0 = 0% 1 = 100% to spawn a mission each day, while on deployment.
DeploymentSalaryMultiplier | float | default 4 | The multiplier a Deplyoment pays more then a normal mission of same difficulty.
MaxMonth | int | default 6 | The maximum amount of months a Deployment can go on for.
DeploymentBreakRepCost | int | default -30 | The Reputation amount you will lose if you break a deployment.
DeploymentBreakMRBRepCost | int | default -50 | The MRB-Rating amount you will lose if you break a deployment.
    
## Install
- After installing BTML and ModTek, put  everything into \BATTLETECH\Mods\ folder.
- If you want different settings set it in the settings.json.
- Start the game.
