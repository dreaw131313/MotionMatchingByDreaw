# MotionMatchingByDreaw
Animation system for Unity engine based on motion matching

## Dictionary:

## New Assets (by class names):
### Editor assets:
**Data Creator** or **DataCreator_New** - stores data and animations to calculate "Motion Matching Data" asset <br/> 
**Data Creator Trajectory Settings** - stores past and future times of trajectory points needed to create trajectory<br/> 
**Motion Matching Bone Profile** - stores information about bones witch will be used to match animations<br/> 
**Motion Matching Data** - assets which stores calculated data (trajectory and pose) for specific animations, this assets are created by editors which use **Data Creator** or **DataCreator_New**. This asset can be editoed in **Motion matching data editor**

### Runtime assets:
**Motion Matching Animator_SO** - asset which represent animation state machine controller (like mecanim animation controller)<br/>
**Native Motion Group** - asset which represent one or more animations. This asset need to be placed in states of **Motion Matching Animator_SO**.




## Editors:

### Motion matching state machine graph editor

![image](https://user-images.githubusercontent.com/49455788/192016139-0c37036f-d4b0-4097-a1c3-a2a192c49062.png)

Editor for creating animation state machine.


### Motion matching data editor:

![image](https://user-images.githubusercontent.com/49455788/192018706-133718c5-b642-42ea-8fa6-9ec539b83ecb.png)

In this editor we can add a lot of data to **Motion Matching Data** asset, such as:
- Sections - time intervals in animation, wchich can be used as:
  - time intervals in witch conditions of transition should be checked
  - time intervals in which next animation should be finded
- Conatct points - points which are used in contact state of **Motion Matching Animator_SO** to move character betweem points specified in game world (they can be used to implement parkour)
- Curves - works like curves in normal animation, but theirs values are evaluated in LateUpdate.
- Animation Events - worls like events in normal animations but are fired in LateUpdate
- Bone Tracks - experimental
- Animation Speed Curve - it is curve which represent animation speed multiplier

