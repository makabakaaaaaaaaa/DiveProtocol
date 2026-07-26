# Kevin Iglesias Zombie Animations Import Check

## Conclusion

Situation A: The package imported usable zombie AnimationClips and includes the basic Idle / Walk or Run / Attack / Death set.

## Target Retarget Check

- Target=Assets/PolygonZombies/Prefabs/Zombie_BioHazardSuit_Male_01.prefab; Controller=None; Avatar=ZombiesAvatar; AvatarIsHuman=True; AvatarIsValid=True

## Summary

| Metric | Value |
|---|---:|
| Package Exists | True |
| AnimationClip Count | 8 |
| AnimatorController Count | 5 |
| Broken / Missing Motion Controllers | 0 |
| FBX / Model Count | 7 |
| Models With Clips | 6 |
| Avatar Count | 1 |
| Prefab Count | 1 |
| Has Idle | True |
| Has Walk Or Run | True |
| Has Attack | True |
| Has Death | True |

## Idle Clips

- Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Idle01.fbx :: Zombie@Idle01
- Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Idle01_Action01.fbx :: Zombie@Idle01_Action01

## Walk / Run Clips

- Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Walk01.fbx :: Zombie@Walk01
- Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Walk01.fbx :: Zombie@Walk01 [RM]

## Attack Clips

- Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Attack01.fbx :: Zombie@Attack01

## Death Clips

- Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Death01_A.fbx :: Zombie@Death01_A
- Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Death01_A.fbx :: Zombie@Death01_B


## Inventory

| Asset Name | Asset Type | Unity Path | Source Folder | Is Animation Clip | Likely Motion Type | Clip Count | Clip Names | Has Avatar | Import Animation | Animation Type | Used By Controller | Controller Reference Status | Notes |
|---|---|---|---|---|---|---:|---|---|---|---|---|---|---|
| Zombie@Attacks | AnimatorController | Assets/Kevin Iglesias/Zombie Animations/AnimatorControllers/Zombie@Attacks.controller | Kevin Iglesias/Zombie Animations | No | Attack | 1 | Zombie@Attack01 | No | N/A | N/A | N/A | OK | Layers=1; States=1; StatesWithMotion=1 |
| Zombie@Damages | AnimatorController | Assets/Kevin Iglesias/Zombie Animations/AnimatorControllers/Zombie@Damages.controller | Kevin Iglesias/Zombie Animations | No | Hit | 1 | Zombie@Damage01 | No | N/A | N/A | N/A | OK | Layers=1; States=1; StatesWithMotion=1 |
| Zombie@Death | AnimatorController | Assets/Kevin Iglesias/Zombie Animations/AnimatorControllers/Zombie@Death.controller | Kevin Iglesias/Zombie Animations | No | Death | 1 | Zombie@Death01_A | No | N/A | N/A | N/A | OK | Layers=1; States=1; StatesWithMotion=1 |
| Zombie@Idles | AnimatorController | Assets/Kevin Iglesias/Zombie Animations/AnimatorControllers/Zombie@Idles.controller | Kevin Iglesias/Zombie Animations | No | Idle | 2 | Zombie@Idle01; Zombie@Idle01_Action01 | No | N/A | N/A | N/A | OK | Layers=1; States=2; StatesWithMotion=2 |
| Zombie@Walk | AnimatorController | Assets/Kevin Iglesias/Zombie Animations/AnimatorControllers/Zombie@Walk.controller | Kevin Iglesias/Zombie Animations | No | Walk | 1 | Zombie@Walk01 | No | N/A | N/A | N/A | OK | Layers=1; States=1; StatesWithMotion=1 |
| ZombieModel | Avatar | Assets/Kevin Iglesias/Zombie Animations/Model/ZombieModel.fbx | Kevin Iglesias/Zombie Animations | No | Not Animation | 0 |  | Yes | True | Human | N/A | N/A | ZombieModelAvatar IsHuman=True IsValid=True |
| Zombie@Attack01 | FBX / ModelImporter | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Attack01.fbx | Kevin Iglesias/Zombie Animations | No | Attack | 1 | Zombie@Attack01 | No | True | Human | N/A | N/A | AvatarSetup=CopyFromOther |
| Zombie@Damage01 | FBX / ModelImporter | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Damage01.fbx | Kevin Iglesias/Zombie Animations | No | Hit | 1 | Zombie@Damage01 | No | True | Human | N/A | N/A | AvatarSetup=CopyFromOther |
| Zombie@Death01_A | FBX / ModelImporter | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Death01_A.fbx | Kevin Iglesias/Zombie Animations | No | Death | 2 | Zombie@Death01_A; Zombie@Death01_B | No | True | Human | N/A | N/A | AvatarSetup=CopyFromOther |
| Zombie@Idle01 | FBX / ModelImporter | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Idle01.fbx | Kevin Iglesias/Zombie Animations | No | Idle | 1 | Zombie@Idle01 | No | True | Human | N/A | N/A | AvatarSetup=CopyFromOther |
| Zombie@Idle01_Action01 | FBX / ModelImporter | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Idle01_Action01.fbx | Kevin Iglesias/Zombie Animations | No | Idle | 1 | Zombie@Idle01_Action01 | No | True | Human | N/A | N/A | AvatarSetup=CopyFromOther |
| Zombie@Walk01 | FBX / ModelImporter | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Walk01.fbx | Kevin Iglesias/Zombie Animations | No | Walk | 2 | Zombie@Walk01; Zombie@Walk01 [RM] | No | True | Human | N/A | N/A | AvatarSetup=CopyFromOther |
| ZombieModel | FBX / ModelImporter | Assets/Kevin Iglesias/Zombie Animations/Model/ZombieModel.fbx | Kevin Iglesias/Zombie Animations | No | Unknown | 0 |  | Yes | True | Human | N/A | N/A | AvatarSetup=CreateFromThisModel |
| Zombie@Attack01 | FBX Embedded AnimationClip | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Attack01.fbx | Kevin Iglesias/Zombie Animations | Yes | Attack | 1 | Zombie@Attack01 | Unknown | True | Human | Assets/Kevin Iglesias/Zombie Animations/AnimatorControllers/Zombie@Attacks.controller :: Base Layer/Attack01 | Clip asset loaded | Embedded in model |
| Zombie@Damage01 | FBX Embedded AnimationClip | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Damage01.fbx | Kevin Iglesias/Zombie Animations | Yes | Hit | 1 | Zombie@Damage01 | Unknown | True | Human | Assets/Kevin Iglesias/Zombie Animations/AnimatorControllers/Zombie@Damages.controller :: Base Layer/Damage01 | Clip asset loaded | Embedded in model |
| Zombie@Death01_A | FBX Embedded AnimationClip | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Death01_A.fbx | Kevin Iglesias/Zombie Animations | Yes | Death | 2 | Zombie@Death01_A; Zombie@Death01_B | Unknown | True | Human | Assets/Kevin Iglesias/Zombie Animations/AnimatorControllers/Zombie@Death.controller :: Base Layer/Zombie@Death01_A | Clip asset loaded | Embedded in model |
| Zombie@Idle01 | FBX Embedded AnimationClip | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Idle01.fbx | Kevin Iglesias/Zombie Animations | Yes | Idle | 1 | Zombie@Idle01 | Unknown | True | Human | Assets/Kevin Iglesias/Zombie Animations/AnimatorControllers/Zombie@Idles.controller :: Base Layer/Idle01 | Clip asset loaded | Embedded in model |
| Zombie@Idle01_Action01 | FBX Embedded AnimationClip | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Idle01_Action01.fbx | Kevin Iglesias/Zombie Animations | Yes | Idle | 1 | Zombie@Idle01_Action01 | Unknown | True | Human | Assets/Kevin Iglesias/Zombie Animations/AnimatorControllers/Zombie@Idles.controller :: Base Layer/Idle01 Action | Clip asset loaded | Embedded in model |
| Zombie@Walk01 | FBX Embedded AnimationClip | Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Walk01.fbx | Kevin Iglesias/Zombie Animations | Yes | Walk | 2 | Zombie@Walk01; Zombie@Walk01 [RM] | Unknown | True | Human | Assets/Kevin Iglesias/Zombie Animations/AnimatorControllers/Zombie@Walk.controller :: Base Layer/Zombie@Walk01 | Clip asset loaded | Embedded in model |
| Zombie | Prefab | Assets/Kevin Iglesias/Zombie Animations/Prefabs/Zombie.prefab | Kevin Iglesias/Zombie Animations | No | Not Animation | 0 |  | Yes | N/A | N/A | None | No controller on Animator | Animator Count=1; Avatars=ZombieModelAvatar |
